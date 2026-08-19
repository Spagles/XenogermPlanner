using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using RimWorld;
using Verse;
using XenogermPlanner.Genes;

namespace XenogermPlanner.Trade
{
    internal sealed class PlanTraderAdvisorySourceSnapshot
    {
        private readonly ReadOnlyCollection<PlanTraderAdvisoryOfferSnapshot> _offers;

        internal PlanTraderAdvisorySourceSnapshot(
            ITrader source,
            PlanTraderAdvisorySourceKind kind,
            Pawn navigationPawn,
            IEnumerable<PlanTraderAdvisoryOfferSnapshot> offers)
        {
            Source = source ?? throw new ArgumentNullException(nameof(source));
            ValidateKind(kind);

            if (kind == PlanTraderAdvisorySourceKind.Orbital && navigationPawn != null)
            {
                throw new ArgumentException(
                    "Orbital trader advisory source cannot have a caravan navigation pawn.",
                    nameof(navigationPawn));
            }

            if (kind == PlanTraderAdvisorySourceKind.Caravan && navigationPawn == null)
            {
                throw new ArgumentNullException(
                    nameof(navigationPawn),
                    "Caravan trader advisory source requires its exact trader pawn.");
            }

            if (offers == null)
                throw new ArgumentNullException(nameof(offers));

            var copiedOffers = new List<PlanTraderAdvisoryOfferSnapshot>();
            var physicalGenepacks = new HashSet<Genepack>(ReferenceEqualityComparer<Genepack>.Instance);

            foreach (PlanTraderAdvisoryOfferSnapshot offer in offers)
            {
                if (offer == null)
                {
                    throw new ArgumentException(
                        "Trader advisory offer collection cannot contain null values.",
                        nameof(offers));
                }

                if (physicalGenepacks.Add(offer.Genepack))
                    copiedOffers.Add(offer);
            }

            Kind = kind;
            NavigationPawn = navigationPawn;
            _offers = copiedOffers.AsReadOnly();
        }

        internal ITrader Source { get; }
        internal PlanTraderAdvisorySourceKind Kind { get; }
        internal Pawn NavigationPawn { get; }
        internal IReadOnlyList<PlanTraderAdvisoryOfferSnapshot> Offers => _offers;

        private static void ValidateKind(PlanTraderAdvisorySourceKind kind)
        {
            if (kind != PlanTraderAdvisorySourceKind.Orbital && kind != PlanTraderAdvisorySourceKind.Caravan)
            {
                throw new ArgumentOutOfRangeException(nameof(kind), kind, "Unknown trader advisory source kind.");
            }
        }
    }
}