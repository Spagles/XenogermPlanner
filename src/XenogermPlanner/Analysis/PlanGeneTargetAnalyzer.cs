using System;
using System.Collections.Generic;
using RimWorld;
using Verse;

namespace XenogermPlanner.Analysis
{
    public static class PlanGeneTargetAnalyzer
    {
        public static PlanGeneTargetAnalysisResult Analyze(IEnumerable<GeneDef> genes)
        {
            return Analyze(
                genes,
                (left, right) => left.ConflictsWith(right),
                (left, right) => left.Overrides(right, isXenogene: true, otherIsXenogene: true),
                gene => gene.RandomChosen,
                gene => gene.prerequisite);
        }

        internal static PlanGeneTargetAnalysisResult Analyze(
            IEnumerable<GeneDef> genes,
            Func<GeneDef, GeneDef, bool> conflictsWith,
            Func<GeneDef, GeneDef, bool> overrides,
            Func<GeneDef, bool> isRandomChosen,
            Func<GeneDef, GeneDef> getPrerequisite)
        {
            if (conflictsWith == null)
                throw new ArgumentNullException(nameof(conflictsWith));

            if (overrides == null)
                throw new ArgumentNullException(nameof(overrides));

            if (isRandomChosen == null)
                throw new ArgumentNullException(nameof(isRandomChosen));

            if (getPrerequisite == null)
                throw new ArgumentNullException(nameof(getPrerequisite));

            List<GeneDef> distinctGenes = CopyDistinctGenes(genes, nameof(genes));
            distinctGenes.Sort(CompareGenes);

            var conflicts = new List<PlanGeneConflictDiagnostic>();
            var randomConflictGraph = new Dictionary<GeneDef, HashSet<GeneDef>>();

            for (var leftIndex = 0; leftIndex < distinctGenes.Count; leftIndex++)
            {
                GeneDef left = distinctGenes[leftIndex];

                for (int rightIndex = leftIndex + 1; rightIndex < distinctGenes.Count; rightIndex++)
                {
                    GeneDef right = distinctGenes[rightIndex];

                    if (!conflictsWith(left, right) && !conflictsWith(right, left))
                        continue;

                    bool leftRandom = isRandomChosen(left);
                    bool rightRandom = isRandomChosen(right);

                    if (leftRandom && rightRandom)
                    {
                        AddRandomConflictEdge(randomConflictGraph, left, right);
                        continue;
                    }

                    PlanGeneConflictKind kind = leftRandom || rightRandom
                        ? PlanGeneConflictKind.Mixed
                        : PlanGeneConflictKind.Ordinary;
                    GeneDef overridingGene = null;
                    GeneDef overriddenGene = null;

                    if (kind == PlanGeneConflictKind.Ordinary)
                    {
                        bool leftOverridesRight = overrides(left, right);
                        bool rightOverridesLeft = overrides(right, left);

                        if (leftOverridesRight != rightOverridesLeft)
                        {
                            overridingGene = leftOverridesRight ? left : right;
                            overriddenGene = leftOverridesRight ? right : left;
                        }
                    }

                    conflicts.Add(new PlanGeneConflictDiagnostic(left, right, kind, overridingGene, overriddenGene));
                }
            }

            List<PlanGeneRandomChoiceGroupDiagnostic> randomChoiceGroups =
                CreateRandomChoiceGroups(distinctGenes, randomConflictGraph);

            List<PlanGenePrerequisiteDiagnostic> missingPrerequisites = FindMissingPrerequisites(
                distinctGenes,
                getPrerequisite);

            return new PlanGeneTargetAnalysisResult(conflicts, randomChoiceGroups, missingPrerequisites);
        }

        internal static List<PlanGenePrerequisiteDiagnostic> FindMissingPrerequisites(IEnumerable<GeneDef> genes)
        {
            return FindMissingPrerequisites(genes, gene => gene.prerequisite);
        }

        internal static List<PlanGenePrerequisiteDiagnostic> FindMissingPrerequisites(
            IEnumerable<GeneDef> genes,
            Func<GeneDef, GeneDef> getPrerequisite)
        {
            if (getPrerequisite == null)
                throw new ArgumentNullException(nameof(getPrerequisite));

            List<GeneDef> distinctGenes = CopyDistinctGenes(genes, nameof(genes));
            var selectedGenes = new HashSet<GeneDef>(distinctGenes);
            var missing = new List<PlanGenePrerequisiteDiagnostic>();

            distinctGenes.Sort(CompareGenes);

            foreach (GeneDef gene in distinctGenes)
            {
                GeneDef prerequisite = getPrerequisite(gene);

                if (prerequisite != null && !selectedGenes.Contains(prerequisite))
                {
                    missing.Add(new PlanGenePrerequisiteDiagnostic(gene, prerequisite));
                }
            }

            missing.Sort(ComparePrerequisites);

            return missing;
        }

        private static void AddRandomConflictEdge(
            IDictionary<GeneDef, HashSet<GeneDef>> graph,
            GeneDef first,
            GeneDef second)
        {
            if (!graph.TryGetValue(first, out HashSet<GeneDef> firstNeighbors))
            {
                firstNeighbors = new HashSet<GeneDef>();
                graph.Add(first, firstNeighbors);
            }

            if (!graph.TryGetValue(second, out HashSet<GeneDef> secondNeighbors))
            {
                secondNeighbors = new HashSet<GeneDef>();
                graph.Add(second, secondNeighbors);
            }

            firstNeighbors.Add(second);
            secondNeighbors.Add(first);
        }

        private static List<PlanGeneRandomChoiceGroupDiagnostic> CreateRandomChoiceGroups(
            IEnumerable<GeneDef> orderedGenes,
            IReadOnlyDictionary<GeneDef, HashSet<GeneDef>> graph)
        {
            var groups = new List<PlanGeneRandomChoiceGroupDiagnostic>();
            var visited = new HashSet<GeneDef>();

            foreach (GeneDef root in orderedGenes)
            {
                if (!graph.ContainsKey(root) || !visited.Add(root))
                    continue;

                var component = new List<GeneDef>();
                var pending = new Stack<GeneDef>();
                pending.Push(root);

                while (pending.Count > 0)
                {
                    GeneDef current = pending.Pop();
                    component.Add(current);

                    var orderedNeighbors = new List<GeneDef>(graph[current]);
                    orderedNeighbors.Sort(CompareGenes);

                    for (int index = orderedNeighbors.Count - 1; index >= 0; index--)
                    {
                        GeneDef neighbor = orderedNeighbors[index];

                        if (visited.Add(neighbor))
                            pending.Push(neighbor);
                    }
                }

                if (component.Count >= 2)
                    groups.Add(new PlanGeneRandomChoiceGroupDiagnostic(component));
            }

            groups.Sort(CompareRandomChoiceGroups);
            return groups;
        }

        private static List<GeneDef> CopyDistinctGenes(IEnumerable<GeneDef> genes, string parameterName)
        {
            if (genes == null)
                throw new ArgumentNullException(parameterName);

            var copied = new List<GeneDef>();
            var distinct = new HashSet<GeneDef>();

            foreach (GeneDef gene in genes)
            {
                if (gene == null)
                {
                    throw new ArgumentException("Gene collection cannot contain null values.", parameterName);
                }

                if (distinct.Add(gene))
                    copied.Add(gene);
            }

            return copied;
        }

        private static int CompareGenes(GeneDef left, GeneDef right)
        {
            return StringComparer.Ordinal.Compare(left.defName ?? string.Empty, right.defName ?? string.Empty);
        }

        private static int CompareRandomChoiceGroups(
            PlanGeneRandomChoiceGroupDiagnostic left,
            PlanGeneRandomChoiceGroupDiagnostic right)
        {
            int sharedCount = Math.Min(left.Genes.Count, right.Genes.Count);

            for (var index = 0; index < sharedCount; index++)
            {
                int comparison = CompareGenes(left.Genes[index], right.Genes[index]);

                if (comparison != 0)
                    return comparison;
            }

            return left.Genes.Count.CompareTo(right.Genes.Count);
        }

        private static int ComparePrerequisites(
            PlanGenePrerequisiteDiagnostic left,
            PlanGenePrerequisiteDiagnostic right)
        {
            int dependentComparison = CompareGenes(left.DependentGene, right.DependentGene);

            return dependentComparison != 0
                ? dependentComparison
                : CompareGenes(left.PrerequisiteGene, right.PrerequisiteGene);
        }
    }
}