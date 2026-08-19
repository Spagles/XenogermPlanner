using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using XenogermPlanner.Plans;

namespace XenogermPlanner.Trade
{
    internal sealed class PlanTraderAdvisoryNotificationOffer
    {
        private readonly ReadOnlyCollection<XenogermPlan> _matchingPlans;

        internal PlanTraderAdvisoryNotificationOffer(
            PlanTraderAdvisoryOfferSnapshot offer,
            IEnumerable<XenogermPlan> matchingPlans)
        {
            Offer = offer ?? throw new ArgumentNullException(nameof(offer));

            if (matchingPlans == null)
                throw new ArgumentNullException(nameof(matchingPlans));

            var copiedMatches = new List<XenogermPlan>();
            var includedPlanIds = new HashSet<string>(StringComparer.Ordinal);

            foreach (XenogermPlan plan in matchingPlans)
            {
                if (plan == null)
                {
                    throw new ArgumentException(
                        "Trader advisory notification plan collection cannot contain null values.",
                        nameof(matchingPlans));
                }

                if (includedPlanIds.Add(plan.Id))
                    copiedMatches.Add(plan);
            }

            if (copiedMatches.Count == 0)
            {
                throw new ArgumentException(
                    "Trader advisory notification offer requires at least one matching plan.",
                    nameof(matchingPlans));
            }

            _matchingPlans = copiedMatches.AsReadOnly();
        }

        internal PlanTraderAdvisoryOfferSnapshot Offer { get; }
        internal IReadOnlyList<XenogermPlan> MatchingPlans => _matchingPlans;
    }
}