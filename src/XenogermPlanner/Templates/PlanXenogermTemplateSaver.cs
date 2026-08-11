using System;
using System.Collections.Generic;
using RimWorld;
using Verse;
using XenogermPlanner.Genes;
using XenogermPlanner.Plans;

namespace XenogermPlanner.Templates
{
    internal static class PlanXenogermTemplateSaver
    {
        internal static PlanXenogermTemplateSaveResult Save(
            XenogermPlan plan,
            PlanXenogermTemplateCandidate candidate,
            string templateName,
            XenotypeIconDef iconDef,
            PlanGenepackInventorySnapshot inventorySnapshot)
        {
            if (inventorySnapshot == null)
                throw new ArgumentNullException(nameof(inventorySnapshot));

            return Save(
                plan,
                candidate,
                templateName,
                iconDef,
                inventorySnapshot.IsAvailable,
                inventorySnapshot.Genepacks,
                GetGenepackGenesOrNull,
                GenepackCompositionUtility.GetStablePhysicalKey,
                CustomXenogermUtility.SaveXenogermTemplate);
        }

        internal static PlanXenogermTemplateSaveResult Save(
            XenogermPlan plan,
            PlanXenogermTemplateCandidate candidate,
            string templateName,
            XenotypeIconDef iconDef,
            bool inventoryAvailable,
            IReadOnlyList<Genepack> genepacks,
            Func<Genepack, IEnumerable<GeneDef>> getGenepackGenes,
            Func<Genepack, string> getPhysicalKey,
            Func<string, XenotypeIconDef, List<Genepack>, AcceptanceReport> saveTemplate)
        {
            if (plan == null)
                throw new ArgumentNullException(nameof(plan));

            if (candidate == null)
                throw new ArgumentNullException(nameof(candidate));

            if (genepacks == null)
                throw new ArgumentNullException(nameof(genepacks));

            if (getGenepackGenes == null)
                throw new ArgumentNullException(nameof(getGenepackGenes));

            if (getPhysicalKey == null)
                throw new ArgumentNullException(nameof(getPhysicalKey));

            if (saveTemplate == null)
                throw new ArgumentNullException(nameof(saveTemplate));

            if (!inventoryAvailable)
                return PlanXenogermTemplateSaveResult.Failed(PlanXenogermTemplateSaveFailure.InventoryUnavailable);

            if (!IsCandidateValidForPlan(plan, candidate))
            {
                return PlanXenogermTemplateSaveResult.Failed(PlanXenogermTemplateSaveFailure.CandidateInvalidForPlan);
            }

            var resolvedGenepacks = new List<Genepack>(candidate.Compositions.Count);

            foreach (PlanXenogermTemplateComposition composition in candidate.Compositions)
            {
                Genepack representative = FindRepresentative(composition, genepacks, getGenepackGenes, getPhysicalKey);

                if (representative == null)
                {
                    return PlanXenogermTemplateSaveResult.Failed(
                        PlanXenogermTemplateSaveFailure.CompositionUnavailable);
                }

                resolvedGenepacks.Add(representative);
            }

            AcceptanceReport report = saveTemplate(templateName, iconDef, resolvedGenepacks);

            if (!report.Accepted)
            {
                return PlanXenogermTemplateSaveResult.VanillaRejected(
                    report.Reason.NullOrEmpty() ? string.Empty : report.Reason);
            }

            return PlanXenogermTemplateSaveResult.Success();
        }

        private static bool IsCandidateValidForPlan(XenogermPlan plan, PlanXenogermTemplateCandidate candidate)
        {
            if (plan.IsDegraded || plan.DesiredGenes.Count == 0 || candidate.Compositions.Count == 0)
                return false;

            var targetGenes = new HashSet<GeneDef>(plan.DesiredGenes);
            var candidateGenes = new HashSet<GeneDef>(candidate.UnionGenes);

            switch (plan.ReadinessMode)
            {
                case PlanReadinessMode.Coverage:
                    return targetGenes.IsSubsetOf(candidateGenes);

                case PlanReadinessMode.ExactPayload:
                    return targetGenes.SetEquals(candidateGenes);

                default:
                    return false;
            }
        }

        private static Genepack FindRepresentative(
            PlanXenogermTemplateComposition composition,
            IEnumerable<Genepack> genepacks,
            Func<Genepack, IEnumerable<GeneDef>> getGenepackGenes,
            Func<Genepack, string> getPhysicalKey)
        {
            Genepack representative = null;
            string representativeKey = null;

            foreach (Genepack genepack in genepacks)
            {
                if (genepack == null)
                    continue;

                IEnumerable<GeneDef> genes = getGenepackGenes(genepack);

                if (genes == null)
                    continue;

                if (!GenepackCompositionUtility.TryCompositionsMatch(composition.Genes, genes))
                    continue;

                string physicalKey = getPhysicalKey(genepack) ?? string.Empty;

                if (representative == null || StringComparer.Ordinal.Compare(physicalKey, representativeKey) < 0)
                {
                    representative = genepack;
                    representativeKey = physicalKey;
                }
            }

            return representative;
        }

        private static IEnumerable<GeneDef> GetGenepackGenesOrNull(Genepack genepack)
        {
            return genepack?.GeneSet?.GenesListForReading;
        }
    }
}