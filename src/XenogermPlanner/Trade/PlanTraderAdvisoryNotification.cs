using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using RimWorld;
using XenogermPlanner.Genes;

namespace XenogermPlanner.Trade
{
    internal sealed class PlanTraderAdvisoryNotification
    {
        private readonly ReadOnlyCollection<PlanTraderAdvisoryNotificationOffer> _offers;

        internal PlanTraderAdvisoryNotification(
            PlanTraderAdvisorySourceSnapshot source,
            IEnumerable<PlanTraderAdvisoryNotificationOffer> offers)
        {
            Source = source ?? throw new ArgumentNullException(nameof(source));

            if (offers == null)
                throw new ArgumentNullException(nameof(offers));

            var copiedOffers = new List<PlanTraderAdvisoryNotificationOffer>();
            var includedGenepacks = new HashSet<Genepack>(ReferenceEqualityComparer<Genepack>.Instance);
            var currentGenepacks = new HashSet<Genepack>(ReferenceEqualityComparer<Genepack>.Instance);

            foreach (PlanTraderAdvisoryOfferSnapshot sourceOffer in source.Offers)
                currentGenepacks.Add(sourceOffer.Genepack);

            foreach (PlanTraderAdvisoryNotificationOffer offer in offers)
            {
                if (offer == null)
                {
                    throw new ArgumentException(
                        "Trader advisory notification offer collection cannot contain null values.",
                        nameof(offers));
                }

                if (!currentGenepacks.Contains(offer.Offer.Genepack))
                {
                    throw new ArgumentException(
                        "Trader advisory notification must refer to a current source offer.",
                        nameof(offers));
                }

                if (includedGenepacks.Add(offer.Offer.Genepack))
                    copiedOffers.Add(offer);
            }

            if (copiedOffers.Count == 0)
            {
                throw new ArgumentException(
                    "Trader advisory notification requires at least one physical offer.",
                    nameof(offers));
            }

            _offers = copiedOffers.AsReadOnly();
        }

        internal PlanTraderAdvisorySourceSnapshot Source { get; }
        internal IReadOnlyList<PlanTraderAdvisoryNotificationOffer> Offers => _offers;
        internal int OfferCount => _offers.Count;
    }
}