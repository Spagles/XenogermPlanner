using System;
using System.Collections.Generic;
using Verse;
using XenogermPlanner.Genes;
using XenogermPlanner.Plans;

namespace XenogermPlanner.Analysis
{
    internal sealed class PlanGenepackRelevanceAnalyzer
    {
        private readonly IReadOnlyList<PlanCandidate> _candidates;

        internal PlanGenepackRelevanceAnalyzer(
            IReadOnlyList<XenogermPlan> plans,
            PlanGenepackInventorySnapshot inventory) : this(plans, inventory, PlanReadinessAnalyzer.Analyze)
        {
        }

        internal PlanGenepackRelevanceAnalyzer(
            IReadOnlyList<XenogermPlan> plans,
            PlanGenepackInventorySnapshot inventory,
            Func<XenogermPlan, PlanGenepackInventorySnapshot, PlanReadinessResult> analyzeReadiness)
        {
            if (plans == null)
                throw new ArgumentNullException(nameof(plans));

            if (inventory == null)
                throw new ArgumentNullException(nameof(inventory));

            if (analyzeReadiness == null)
                throw new ArgumentNullException(nameof(analyzeReadiness));

            _candidates = BuildCandidates(plans, inventory, analyzeReadiness).AsReadOnly();
        }

        internal IReadOnlyList<XenogermPlan> Evaluate(IReadOnlyCollection<GeneDef> offeredGenes)
        {
            HashSet<GeneDef> offeredGeneSet = CreateOfferedGeneSet(offeredGenes);
            var matchingCandidates = new List<PlanCandidate>();

            foreach (PlanCandidate candidate in _candidates)
            {
                if (candidate.IsRelevant(offeredGeneSet))
                    matchingCandidates.Add(candidate);
            }

            matchingCandidates.Sort(CompareCandidates);

            var matchingPlans = new List<XenogermPlan>(matchingCandidates.Count);

            foreach (PlanCandidate candidate in matchingCandidates)
                matchingPlans.Add(candidate.Plan);

            return matchingPlans.AsReadOnly();
        }

        private static HashSet<GeneDef> CreateOfferedGeneSet(IReadOnlyCollection<GeneDef> offeredGenes)
        {
            if (offeredGenes == null)
                throw new ArgumentNullException(nameof(offeredGenes));

            var offeredGeneSet = new HashSet<GeneDef>();

            foreach (GeneDef offeredGene in offeredGenes)
            {
                if (offeredGene == null)
                {
                    throw new ArgumentException(
                        "Offered gene collection cannot contain null values.",
                        nameof(offeredGenes));
                }

                offeredGeneSet.Add(offeredGene);
            }

            if (offeredGeneSet.Count == 0)
                throw new ArgumentException("Offered gene collection cannot be empty.", nameof(offeredGenes));

            return offeredGeneSet;
        }

        private static List<PlanCandidate> BuildCandidates(
            IReadOnlyList<XenogermPlan> plans,
            PlanGenepackInventorySnapshot inventory,
            Func<XenogermPlan, PlanGenepackInventorySnapshot, PlanReadinessResult> analyzeReadiness)
        {
            var candidates = new List<PlanCandidate>();

            foreach (XenogermPlan plan in plans)
            {
                if (plan == null)
                    throw new ArgumentException("Plan collection cannot contain null values.", nameof(plans));

                PlanReadinessResult readiness = analyzeReadiness(plan, inventory) ??
                                                throw new InvalidOperationException(
                                                    "Plan readiness analyzer returned a null result.");

                if (readiness.Status != PlanReadinessStatus.NotReady)
                    continue;

                var targetGenes = new HashSet<GeneDef>(plan.DesiredGenes);
                var neededGenes = new HashSet<GeneDef>();

                foreach (PlanGeneCoverageDiagnostic diagnostic in readiness.GeneCoverageDiagnostics)
                {
                    if (IsRelevantNeed(plan.ReadinessMode, diagnostic.State))
                        neededGenes.Add(diagnostic.Gene);
                }

                if (neededGenes.Count == 0)
                    continue;

                candidates.Add(new PlanCandidate(plan, targetGenes, neededGenes));
            }

            return candidates;
        }

        private static bool IsRelevantNeed(PlanReadinessMode readinessMode, PlanGeneCoverageState state)
        {
            switch (readinessMode)
            {
                case PlanReadinessMode.Coverage:
                    return state == PlanGeneCoverageState.Missing;

                case PlanReadinessMode.ExactPayload:
                    return state == PlanGeneCoverageState.Missing ||
                           state == PlanGeneCoverageState.ExactPayloadConflict;

                default:
                    throw new ArgumentOutOfRangeException(
                        nameof(readinessMode),
                        readinessMode,
                        "Unknown plan readiness mode.");
            }
        }

        private static int CompareCandidates(PlanCandidate left, PlanCandidate right)
        {
            int nameComparison = StringComparer.OrdinalIgnoreCase.Compare(left.DisplayName, right.DisplayName);

            return nameComparison != 0 ? nameComparison : StringComparer.Ordinal.Compare(left.PlanId, right.PlanId);
        }

        private sealed class PlanCandidate
        {
            private readonly HashSet<GeneDef> _targetGenes;
            private readonly HashSet<GeneDef> _neededGenes;

            internal PlanCandidate(XenogermPlan plan, HashSet<GeneDef> targetGenes, HashSet<GeneDef> neededGenes)
            {
                Plan = plan ?? throw new ArgumentNullException(nameof(plan));
                PlanId = plan.Id;
                DisplayName = plan.Name;
                ReadinessMode = plan.ReadinessMode;
                _targetGenes = targetGenes ?? throw new ArgumentNullException(nameof(targetGenes));
                _neededGenes = neededGenes ?? throw new ArgumentNullException(nameof(neededGenes));
            }

            internal XenogermPlan Plan { get; }
            internal string PlanId { get; }
            internal string DisplayName { get; }
            internal PlanReadinessMode ReadinessMode { get; }

            internal bool IsRelevant(IReadOnlyCollection<GeneDef> offeredGenes)
            {
                if (ReadinessMode == PlanReadinessMode.ExactPayload)
                {
                    foreach (GeneDef offeredGene in offeredGenes)
                    {
                        if (!_targetGenes.Contains(offeredGene))
                            return false;
                    }
                }

                foreach (GeneDef offeredGene in offeredGenes)
                {
                    if (_neededGenes.Contains(offeredGene))
                        return true;
                }

                return false;
            }
        }
    }
}