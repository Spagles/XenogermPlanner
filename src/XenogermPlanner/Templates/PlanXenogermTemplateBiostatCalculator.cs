using System;
using System.Collections.Generic;
using Verse;
using XenogermPlanner.Genes;

namespace XenogermPlanner.Templates
{
    internal static class PlanXenogermTemplateBiostatCalculator
    {
        internal static PlanXenogermTemplateBiostats CalculateComposition(PlanXenogermTemplateComposition composition)
        {
            if (composition == null)
                throw new ArgumentNullException(nameof(composition));

            return Convert(PlanGeneBiostatCalculator.CalculateRaw(composition.Genes));
        }

        internal static PlanXenogermTemplateBiostats CalculateCandidate(PlanXenogermTemplateCandidate candidate)
        {
            List<GeneDef> flattenedGenes = FlattenCandidateGenes(candidate);

            return Convert(PlanGeneBiostatCalculator.CalculateEffective(flattenedGenes));
        }

        internal static PlanXenogermTemplateBiostats CalculateCandidate(
            PlanXenogermTemplateCandidate candidate,
            Func<IEnumerable<GeneDef>, IEnumerable<GeneDef>> getNonOverriddenGenes)
        {
            if (getNonOverriddenGenes == null)
                throw new ArgumentNullException(nameof(getNonOverriddenGenes));

            List<GeneDef> flattenedGenes = FlattenCandidateGenes(candidate);

            return Convert(PlanGeneBiostatCalculator.CalculateEffective(flattenedGenes, getNonOverriddenGenes));
        }

        private static List<GeneDef> FlattenCandidateGenes(PlanXenogermTemplateCandidate candidate)
        {
            if (candidate == null)
                throw new ArgumentNullException(nameof(candidate));

            var flattenedGenes = new List<GeneDef>();

            foreach (PlanXenogermTemplateComposition composition in candidate.Compositions)
                flattenedGenes.AddRange(composition.Genes);

            return flattenedGenes;
        }

        private static PlanXenogermTemplateBiostats Convert(PlanGeneBiostats biostats)
        {
            if (biostats == null)
                throw new ArgumentNullException(nameof(biostats));

            return new PlanXenogermTemplateBiostats(biostats.Complexity, biostats.Metabolism, biostats.ArchiteCapsules);
        }
    }
}