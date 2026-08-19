using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using XenogermPlanner.Plans;

namespace XenogermPlanner.Trade
{
    internal sealed class PlanTraderAdvisoryOfferAnalysis
    {
        private readonly ReadOnlyCollection<XenogermPlan> _matchingPlans;

        internal PlanTraderAdvisoryOfferAnalysis(
            PlanTraderAdvisoryOfferSnapshot offer,
            IEnumerable<XenogermPlan> matchingPlans)
        {
            Offer = offer ?? throw new ArgumentNullException(nameof(offer));

            if (matchingPlans == null)
                throw new ArgumentNullException(nameof(matchingPlans));

            var copiedMatches = new List<XenogermPlan>();

            foreach (XenogermPlan plan in matchingPlans)
            {
                if (plan == null)
                {
                    throw new ArgumentException(
                        "Trader advisory matching plan collection cannot contain null values.",
                        nameof(matchingPlans));
                }

                copiedMatches.Add(plan);
            }

            _matchingPlans = copiedMatches.AsReadOnly();
        }

        internal PlanTraderAdvisoryOfferSnapshot Offer { get; }
        internal IReadOnlyList<XenogermPlan> MatchingPlans => _matchingPlans;
        internal bool IsRelevant => _matchingPlans.Count > 0;
    }
}