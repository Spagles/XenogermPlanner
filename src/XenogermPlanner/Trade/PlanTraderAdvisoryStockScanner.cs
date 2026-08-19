using System;
using System.Collections.Generic;
using RimWorld;
using Verse;
using Verse.AI.Group;
using XenogermPlanner.Genes;

namespace XenogermPlanner.Trade
{
    internal static class PlanTraderAdvisoryStockScanner
    {
        internal static PlanTraderAdvisoryStockSnapshot Scan(Map map)
        {
            if (map == null)
                return PlanTraderAdvisoryStockSnapshot.Unavailable;

            return Scan(
                map,
                DiscoverOrbitalTraders,
                DiscoverCaravanTraders,
                GetTraderGoods,
                GenepackCompositionUtility.GetGenes,
                ReportError);
        }

        internal static PlanTraderAdvisoryStockSnapshot Scan(
            Map map,
            Func<Map, IEnumerable<TradeShip>> discoverOrbitalTraders,
            Func<Map, IEnumerable<Pawn>> discoverCaravanTraders,
            Func<ITrader, IEnumerable<Thing>> getTraderGoods,
            Func<Genepack, IReadOnlyCollection<GeneDef>> getGenepackGenes,
            Action<string> reportError)
        {
            if (map == null)
                return PlanTraderAdvisoryStockSnapshot.Unavailable;

            if (discoverOrbitalTraders == null)
                throw new ArgumentNullException(nameof(discoverOrbitalTraders));

            if (discoverCaravanTraders == null)
                throw new ArgumentNullException(nameof(discoverCaravanTraders));

            if (getTraderGoods == null)
                throw new ArgumentNullException(nameof(getTraderGoods));

            if (getGenepackGenes == null)
                throw new ArgumentNullException(nameof(getGenepackGenes));

            if (reportError == null)
                throw new ArgumentNullException(nameof(reportError));

            var sources = new List<PlanTraderAdvisorySourceSnapshot>();
            var includedTraders = new HashSet<ITrader>(ReferenceEqualityComparer<ITrader>.Instance);

            AddOrbitalSources(
                map,
                discoverOrbitalTraders,
                getTraderGoods,
                getGenepackGenes,
                reportError,
                includedTraders,
                sources);

            AddCaravanSources(
                map,
                discoverCaravanTraders,
                getTraderGoods,
                getGenepackGenes,
                reportError,
                includedTraders,
                sources);

            return PlanTraderAdvisoryStockSnapshot.CreateAvailable(sources);
        }

        private static void AddOrbitalSources(
            Map map,
            Func<Map, IEnumerable<TradeShip>> discoverOrbitalTraders,
            Func<ITrader, IEnumerable<Thing>> getTraderGoods,
            Func<Genepack, IReadOnlyCollection<GeneDef>> getGenepackGenes,
            Action<string> reportError,
            HashSet<ITrader> includedTraders,
            List<PlanTraderAdvisorySourceSnapshot> sources)
        {
            IEnumerable<TradeShip> traders;

            try
            {
                traders = discoverOrbitalTraders(map);
            }
            catch (Exception exception)
            {
                reportError($"Failed to discover current orbital traders: {exception}");
                return;
            }

            if (traders == null)
                return;

            try
            {
                foreach (TradeShip trader in traders)
                {
                    if (trader == null || !includedTraders.Add(trader))
                        continue;

                    TryAddSource(
                        trader,
                        PlanTraderAdvisorySourceKind.Orbital,
                        null,
                        getTraderGoods,
                        getGenepackGenes,
                        reportError,
                        sources);
                }
            }
            catch (Exception exception)
            {
                reportError($"Failed while enumerating current orbital traders: {exception}");
            }
        }

        private static void AddCaravanSources(
            Map map,
            Func<Map, IEnumerable<Pawn>> discoverCaravanTraders,
            Func<ITrader, IEnumerable<Thing>> getTraderGoods,
            Func<Genepack, IReadOnlyCollection<GeneDef>> getGenepackGenes,
            Action<string> reportError,
            HashSet<ITrader> includedTraders,
            List<PlanTraderAdvisorySourceSnapshot> sources)
        {
            IEnumerable<Pawn> traders;

            try
            {
                traders = discoverCaravanTraders(map);
            }
            catch (Exception exception)
            {
                reportError($"Failed to discover current visiting caravan traders: {exception}");
                return;
            }

            if (traders == null)
                return;

            try
            {
                foreach (Pawn traderPawn in traders)
                {
                    if (traderPawn == null)
                        continue;

                    ITrader trader = traderPawn;

                    if (!includedTraders.Add(trader))
                        continue;

                    TryAddSource(
                        trader,
                        PlanTraderAdvisorySourceKind.Caravan,
                        traderPawn,
                        getTraderGoods,
                        getGenepackGenes,
                        reportError,
                        sources);
                }
            }
            catch (Exception exception)
            {
                reportError($"Failed while enumerating current visiting caravan traders: {exception}");
            }
        }

        private static void TryAddSource(
            ITrader trader,
            PlanTraderAdvisorySourceKind kind,
            Pawn navigationPawn,
            Func<ITrader, IEnumerable<Thing>> getTraderGoods,
            Func<Genepack, IReadOnlyCollection<GeneDef>> getGenepackGenes,
            Action<string> reportError,
            List<PlanTraderAdvisorySourceSnapshot> sources)
        {
            try
            {
                IEnumerable<Thing> goods = getTraderGoods(trader) ??
                                           throw new InvalidOperationException(
                                               "Trader goods collection is unavailable.");

                var offers = new List<PlanTraderAdvisoryOfferSnapshot>();
                var includedGenepacks = new HashSet<Genepack>(ReferenceEqualityComparer<Genepack>.Instance);

                foreach (Thing thing in goods)
                {
                    if (!(thing is Genepack genepack) || !includedGenepacks.Add(genepack))
                        continue;

                    try
                    {
                        IReadOnlyCollection<GeneDef> genes = getGenepackGenes(genepack) ??
                                                             throw new InvalidOperationException(
                                                                 "Trader genepack gene collection is unavailable.");

                        offers.Add(new PlanTraderAdvisoryOfferSnapshot(genepack, genes));
                    }
                    catch (Exception exception)
                    {
                        reportError($"Failed to read one trader genepack offer: {exception}");
                    }
                }

                sources.Add(new PlanTraderAdvisorySourceSnapshot(trader, kind, navigationPawn, offers));
            }
            catch (Exception exception)
            {
                reportError($"Failed to read one current trader source: {exception}");
            }
        }

        private static IEnumerable<TradeShip> DiscoverOrbitalTraders(Map map)
        {
            if (map.passingShipManager?.passingShips == null)
                yield break;

            foreach (PassingShip passingShip in map.passingShipManager.passingShips)
            {
                if (passingShip is TradeShip tradeShip)
                    yield return tradeShip;
            }
        }

        private static IEnumerable<Pawn> DiscoverCaravanTraders(Map map)
        {
            if (map.lordManager?.lords == null)
                yield break;

            foreach (Lord lord in map.lordManager.lords)
            {
                if (lord == null || !(lord.LordJob is LordJob_TradeWithColony))
                    continue;

                Pawn trader = TraderCaravanUtility.FindTrader(lord);
                if (trader != null)
                    yield return trader;
            }
        }

        private static IEnumerable<Thing> GetTraderGoods(ITrader trader)
        {
            return trader.Goods;
        }

        private static void ReportError(string message)
        {
            Log.Error($"{XenogermPlannerMod.LogPrefix} {message}");
        }
    }
}