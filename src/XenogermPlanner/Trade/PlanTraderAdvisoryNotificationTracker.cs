using System;
using System.Collections.Generic;
using RimWorld;
using XenogermPlanner.Plans;

namespace XenogermPlanner.Trade
{
    internal static class PlanTraderAdvisoryNotificationTracker
    {
        internal static void EstablishBaseline(PlanTraderAdvisorySourceState sourceState)
        {
            if (sourceState == null)
                throw new ArgumentNullException(nameof(sourceState));

            sourceState.ClearPending();

            foreach (PlanTraderAdvisoryOfferAnalysis analysis in sourceState.OfferAnalyses)
            {
                foreach (XenogermPlan plan in analysis.MatchingPlans)
                    sourceState.MarkAcknowledged(analysis.Offer.Genepack, plan.Id);
            }
        }

        internal static PlanTraderAdvisoryNotification Reconcile(
            PlanTraderAdvisorySourceState sourceState,
            bool canTradeNow)
        {
            if (sourceState == null)
                throw new ArgumentNullException(nameof(sourceState));

            sourceState.ClearPending();
            var notificationOffers = new List<PlanTraderAdvisoryNotificationOffer>();

            foreach (PlanTraderAdvisoryOfferAnalysis analysis in sourceState.OfferAnalyses)
            {
                Genepack genepack = analysis.Offer.Genepack;
                var unacknowledgedPlans = new List<XenogermPlan>();
                var includedPlanIds = new HashSet<string>(StringComparer.Ordinal);

                foreach (XenogermPlan plan in analysis.MatchingPlans)
                {
                    if (!includedPlanIds.Add(plan.Id) || sourceState.IsAcknowledged(genepack, plan.Id))
                        continue;

                    sourceState.MarkPending(genepack, plan.Id);
                    unacknowledgedPlans.Add(plan);
                }

                if (canTradeNow && unacknowledgedPlans.Count > 0)
                {
                    notificationOffers.Add(
                        new PlanTraderAdvisoryNotificationOffer(analysis.Offer, unacknowledgedPlans));
                }
            }

            return canTradeNow && notificationOffers.Count > 0
                ? new PlanTraderAdvisoryNotification(sourceState.Snapshot, notificationOffers)
                : null;
        }

        internal static void MarkDelivered(
            PlanTraderAdvisorySourceState sourceState,
            PlanTraderAdvisoryNotification notification)
        {
            if (sourceState == null)
                throw new ArgumentNullException(nameof(sourceState));

            if (notification == null)
                throw new ArgumentNullException(nameof(notification));

            if (!ReferenceEquals(sourceState.Snapshot.Source, notification.Source.Source))
            {
                throw new ArgumentException(
                    "Trader advisory notification must belong to the same source lifetime.",
                    nameof(notification));
            }

            foreach (PlanTraderAdvisoryNotificationOffer offer in notification.Offers)
            {
                foreach (XenogermPlan plan in offer.MatchingPlans)
                    sourceState.MarkAcknowledged(offer.Offer.Genepack, plan.Id);
            }
        }
    }
}