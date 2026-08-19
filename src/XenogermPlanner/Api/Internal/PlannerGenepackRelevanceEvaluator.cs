using System;
using System.Collections.Generic;
using Verse;
using XenogermPlanner.Analysis;
using XenogermPlanner.Genes;
using XenogermPlanner.Plans;

namespace XenogermPlanner.Api.Internal
{
    internal sealed class PlannerGenepackRelevanceEvaluator
    {
        private readonly PlanGenepackRelevanceAnalyzer _analyzer;

        internal PlannerGenepackRelevanceEvaluator(
            IReadOnlyList<XenogermPlan> plans,
            PlanGenepackInventorySnapshot inventory) : this(plans, inventory, PlanReadinessAnalyzer.Analyze)
        {
        }

        internal PlannerGenepackRelevanceEvaluator(
            IReadOnlyList<XenogermPlan> plans,
            PlanGenepackInventorySnapshot inventory,
            Func<XenogermPlan, PlanGenepackInventorySnapshot, PlanReadinessResult> analyzeReadiness)
        {
            _analyzer = new PlanGenepackRelevanceAnalyzer(plans, inventory, analyzeReadiness);
        }

        internal GenepackRelevanceItemResult Evaluate(IReadOnlyCollection<GeneDef> offeredGenes)
        {
            IReadOnlyList<XenogermPlan> matchingPlans = _analyzer.Evaluate(offeredGenes);
            var matches = new List<GenepackRelevancePlanMatch>(matchingPlans.Count);

            foreach (XenogermPlan plan in matchingPlans)
                matches.Add(new GenepackRelevancePlanMatch(plan.Id, plan.Name));

            return GenepackRelevanceItemResult.CreateSuccess(matches);
        }
    }
}