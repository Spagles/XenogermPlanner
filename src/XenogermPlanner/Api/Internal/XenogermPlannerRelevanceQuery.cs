using System;
using System.Collections.Generic;
using Verse;
using XenogermPlanner.Genes;
using XenogermPlanner.Plans;

namespace XenogermPlanner.Api.Internal
{
    internal static class XenogermPlannerRelevanceQuery
    {
        internal static GenepackRelevanceBatchResult Query(IReadOnlyList<GenepackRelevanceRequest> requests)
        {
            return Query(
                requests,
                () => Current.Game != null,
                () => Find.CurrentMap != null,
                CreateEvaluator,
                ResolveGeneDef);
        }

        internal static GenepackRelevanceBatchResult Query(
            IReadOnlyList<GenepackRelevanceRequest> requests,
            Func<bool> hasGame,
            Func<bool> hasActiveMap,
            Func<Func<IReadOnlyCollection<GeneDef>, GenepackRelevanceItemResult>> createEvaluator,
            Func<string, GeneDef> resolveGeneDef)
        {
            if (requests == null)
                return GenepackRelevanceBatchResult.CreateInvalidRequest();

            if (requests.Count == 0)
            {
                return GenepackRelevanceBatchResult.CreateSuccess(Array.Empty<GenepackRelevanceItemResult>());
            }

            if (hasGame == null)
                throw new ArgumentNullException(nameof(hasGame));

            if (hasActiveMap == null)
                throw new ArgumentNullException(nameof(hasActiveMap));

            if (createEvaluator == null)
                throw new ArgumentNullException(nameof(createEvaluator));

            if (resolveGeneDef == null)
                throw new ArgumentNullException(nameof(resolveGeneDef));

            Func<IReadOnlyCollection<GeneDef>, GenepackRelevanceItemResult> evaluateComposition;

            try
            {
                if (!hasGame())
                {
                    return GenepackRelevanceBatchResult.CreateUnavailable(GenepackRelevanceUnavailableReason.NoGame);
                }

                if (!hasActiveMap())
                {
                    return GenepackRelevanceBatchResult.CreateUnavailable(
                        GenepackRelevanceUnavailableReason.NoActiveMap);
                }

                evaluateComposition = createEvaluator();

                if (evaluateComposition == null)
                {
                    return GenepackRelevanceBatchResult.CreateUnavailable(
                        GenepackRelevanceUnavailableReason.PlannerStateUnavailable);
                }
            }
            catch
            {
                return GenepackRelevanceBatchResult.CreateFailed();
            }

            var results = new List<GenepackRelevanceItemResult>(requests.Count);

            foreach (GenepackRelevanceRequest request in requests)
            {
                if (!TryCreateEffectiveComposition(request, out IReadOnlyList<string> effectiveComposition))
                {
                    results.Add(GenepackRelevanceItemResult.CreateInvalidInput());
                    continue;
                }

                try
                {
                    if (!TryResolveComposition(
                            effectiveComposition,
                            resolveGeneDef,
                            out IReadOnlyCollection<GeneDef> resolvedComposition))
                    {
                        results.Add(GenepackRelevanceItemResult.CreateUnknownGeneDef());
                        continue;
                    }

                    GenepackRelevanceItemResult result = evaluateComposition(resolvedComposition);
                    results.Add(result ?? GenepackRelevanceItemResult.CreateFailed());
                }
                catch
                {
                    results.Add(GenepackRelevanceItemResult.CreateFailed());
                }
            }

            return GenepackRelevanceBatchResult.CreateSuccess(results);
        }

        private static Func<IReadOnlyCollection<GeneDef>, GenepackRelevanceItemResult> CreateEvaluator()
        {
            Game game = Current.Game;

            XenogermPlanGameComponent planComponent = game?.GetComponent<XenogermPlanGameComponent>();
            PlanGenepackInventoryGameComponent inventoryComponent =
                game?.GetComponent<PlanGenepackInventoryGameComponent>();

            if (planComponent == null || inventoryComponent == null)
                return null;

            PlanGenepackInventorySnapshot inventory = inventoryComponent.Snapshot;

            if (inventory == null || !inventory.IsAvailable)
                return null;

            var evaluator = new PlannerGenepackRelevanceEvaluator(planComponent.Plans, inventory);
            return evaluator.Evaluate;
        }

        private static GeneDef ResolveGeneDef(string geneDefName)
        {
            return DefDatabase<GeneDef>.GetNamedSilentFail(geneDefName);
        }

        private static bool TryCreateEffectiveComposition(
            GenepackRelevanceRequest request,
            out IReadOnlyList<string> effectiveComposition)
        {
            if (request == null || request.GeneDefNames.Count == 0)
            {
                effectiveComposition = null;
                return false;
            }

            var distinctGeneDefNames = new HashSet<string>(StringComparer.Ordinal);
            var normalizedGeneDefNames = new List<string>(request.GeneDefNames.Count);

            foreach (string geneDefName in request.GeneDefNames)
            {
                if (string.IsNullOrWhiteSpace(geneDefName))
                {
                    effectiveComposition = null;
                    return false;
                }

                if (distinctGeneDefNames.Add(geneDefName))
                    normalizedGeneDefNames.Add(geneDefName);
            }

            effectiveComposition = normalizedGeneDefNames.AsReadOnly();
            return true;
        }

        private static bool TryResolveComposition(
            IReadOnlyList<string> effectiveComposition,
            Func<string, GeneDef> resolveGeneDef,
            out IReadOnlyCollection<GeneDef> resolvedComposition)
        {
            var distinctGenes = new HashSet<GeneDef>();
            var resolvedGenes = new List<GeneDef>(effectiveComposition.Count);

            foreach (string geneDefName in effectiveComposition)
            {
                GeneDef gene = resolveGeneDef(geneDefName);

                if (gene == null)
                {
                    resolvedComposition = null;
                    return false;
                }

                if (distinctGenes.Add(gene))
                    resolvedGenes.Add(gene);
            }

            resolvedComposition = resolvedGenes.AsReadOnly();
            return true;
        }
    }
}