using System;
using System.Collections.Generic;
using RimWorld;
using Verse;

namespace XenogermPlanner.Donors
{
    public static class PlanPotentialDonorAnalyzer
    {
        private const int MaximumExtractedGeneCount = 4;

        public static PlanPotentialDonorAnalysisResult Analyze(
            IEnumerable<GeneDef> genes,
            PlanPotentialDonorScopeSnapshot scope)
        {
            return Analyze(genes, scope, GetPawnGenes);
        }

        internal static PlanPotentialDonorAnalysisResult Analyze(
            IEnumerable<GeneDef> genes,
            PlanPotentialDonorScopeSnapshot scope,
            Func<Pawn, IEnumerable<GeneDef>> getPawnGenes)
        {
            if (scope == null)
                throw new ArgumentNullException(nameof(scope));

            if (getPawnGenes == null)
                throw new ArgumentNullException(nameof(getPawnGenes));

            List<GeneDef> requestedGenes = CopyDistinctRequestedGenes(genes);
            requestedGenes.Sort(CompareGenes);

            if (!scope.IsAvailable)
                return PlanPotentialDonorAnalysisResult.Unavailable;

            var donorsByGene = new Dictionary<GeneDef, List<Pawn>>();

            foreach (GeneDef gene in requestedGenes)
                donorsByGene.Add(gene, new List<Pawn>());

            foreach (Pawn pawn in scope.Pawns)
            {
                List<GeneDef> pawnGenes = CopyDistinctPawnGenes(getPawnGenes(pawn));

                if (pawnGenes.Count == 0)
                    continue;

                var pawnGeneSet = new HashSet<GeneDef>(pawnGenes);

                foreach (GeneDef requestedGene in requestedGenes)
                {
                    if (!pawnGeneSet.Contains(requestedGene))
                        continue;

                    if (CanParticipateInExtractionFromDistinctGenes(requestedGene, pawnGenes))
                        donorsByGene[requestedGene].Add(pawn);
                }
            }

            var diagnostics = new List<PlanPotentialDonorGeneDiagnostic>(requestedGenes.Count);

            foreach (GeneDef gene in requestedGenes)
                diagnostics.Add(new PlanPotentialDonorGeneDiagnostic(gene, donorsByGene[gene]));

            return PlanPotentialDonorAnalysisResult.CreateAvailable(diagnostics);
        }

        internal static bool CanParticipateInExtraction(GeneDef targetGene, IEnumerable<GeneDef> pawnGenes)
        {
            if (targetGene == null)
                throw new ArgumentNullException(nameof(targetGene));

            List<GeneDef> distinctPawnGenes = CopyDistinctPawnGenes(pawnGenes);

            return CanParticipateInExtractionFromDistinctGenes(targetGene, distinctPawnGenes);
        }

        private static bool CanParticipateInExtractionFromDistinctGenes(
            GeneDef targetGene,
            IReadOnlyList<GeneDef> distinctPawnGenes)
        {
            if (!ContainsGene(distinctPawnGenes, targetGene) || !CanBeSelected(targetGene))
                return false;

            var helperGenes = new List<GeneDef>();

            foreach (GeneDef gene in distinctPawnGenes)
            {
                if (!ReferenceEquals(gene, targetGene) && CanBeSelected(gene))
                    helperGenes.Add(gene);
            }

            helperGenes.Sort(CompareGenes);

            return CanReachTarget(
                targetGene,
                helperGenes,
                new bool[helperGenes.Count],
                currentMetabolism: 0,
                selectedHelperCount: 0);
        }

        private static bool CanReachTarget(
            GeneDef targetGene,
            IReadOnlyList<GeneDef> helperGenes,
            bool[] selectedHelpers,
            int currentMetabolism,
            int selectedHelperCount)
        {
            int targetMetabolism = currentMetabolism + targetGene.biostatMet;

            if (IsMetabolismInRange(targetMetabolism))
                return true;

            if (selectedHelperCount >= MaximumExtractedGeneCount - 1)
                return false;

            for (var index = 0; index < helperGenes.Count; index++)
            {
                if (selectedHelpers[index])
                    continue;

                GeneDef helperGene = helperGenes[index];
                int nextMetabolism = currentMetabolism + helperGene.biostatMet;

                if (!IsMetabolismInRange(nextMetabolism))
                    continue;

                selectedHelpers[index] = true;

                if (CanReachTarget(targetGene, helperGenes, selectedHelpers, nextMetabolism, selectedHelperCount + 1))
                {
                    selectedHelpers[index] = false;
                    return true;
                }

                selectedHelpers[index] = false;
            }

            return false;
        }

        private static bool IsMetabolismInRange(int metabolism)
        {
            IntRange range = GeneTuning.BiostatRange;

            return metabolism >= range.min && metabolism <= range.max;
        }

        private static bool CanBeSelected(GeneDef gene)
        {
            return gene.biostatArc == 0 && gene.endogeneCategory != EndogeneCategory.Melanin;
        }

        private static IEnumerable<GeneDef> GetPawnGenes(Pawn pawn)
        {
            if (pawn == null)
                throw new ArgumentNullException(nameof(pawn));

            Pawn_GeneTracker geneTracker = pawn.genes;

            if (geneTracker == null)
                return Array.Empty<GeneDef>();

            List<Gene> genes = geneTracker.GenesListForReading;

            if (genes == null || genes.Count == 0)
                return Array.Empty<GeneDef>();

            var geneDefs = new List<GeneDef>(genes.Count);

            foreach (Gene gene in genes)
            {
                if (gene?.def != null)
                    geneDefs.Add(gene.def);
            }

            return geneDefs;
        }

        private static List<GeneDef> CopyDistinctRequestedGenes(IEnumerable<GeneDef> genes)
        {
            if (genes == null)
                throw new ArgumentNullException(nameof(genes));

            var copied = new List<GeneDef>();
            var distinct = new HashSet<GeneDef>();

            foreach (GeneDef gene in genes)
            {
                if (gene == null)
                {
                    throw new ArgumentException("Requested gene collection cannot contain null values.", nameof(genes));
                }

                if (distinct.Add(gene))
                    copied.Add(gene);
            }

            return copied;
        }

        private static List<GeneDef> CopyDistinctPawnGenes(IEnumerable<GeneDef> genes)
        {
            var copied = new List<GeneDef>();

            if (genes == null)
                return copied;

            var distinct = new HashSet<GeneDef>();

            foreach (GeneDef gene in genes)
            {
                if (gene != null && distinct.Add(gene))
                    copied.Add(gene);
            }

            return copied;
        }

        private static bool ContainsGene(IEnumerable<GeneDef> genes, GeneDef expectedGene)
        {
            foreach (GeneDef gene in genes)
            {
                if (ReferenceEquals(gene, expectedGene))
                    return true;
            }

            return false;
        }

        private static int CompareGenes(GeneDef left, GeneDef right)
        {
            return StringComparer.Ordinal.Compare(left.defName ?? string.Empty, right.defName ?? string.Empty);
        }
    }
}