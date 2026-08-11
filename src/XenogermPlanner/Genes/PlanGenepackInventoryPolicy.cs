using System;
using System.Collections.Generic;
using RimWorld;
using Verse;

namespace XenogermPlanner.Genes
{
    internal static class PlanGenepackInventoryPolicy
    {
        internal static bool ShouldTraverseHolder(IThingHolder holder)
        {
            if (holder == null || holder is PassingShip)
                return false;

            Faction holderFaction = GetHolderFaction(holder);

            return holderFaction == null || ReferenceEquals(holderFaction, Faction.OfPlayer);
        }

        internal static bool ShouldTraverseHolder(
            IThingHolder holder,
            Func<IThingHolder, Faction> getHolderFaction,
            Faction playerFaction)
        {
            if (getHolderFaction == null)
                throw new ArgumentNullException(nameof(getHolderFaction));

            if (holder == null || holder is PassingShip)
                return false;

            Faction holderFaction = getHolderFaction(holder);

            return holderFaction == null || ReferenceEquals(holderFaction, playerFaction);
        }

        internal static bool ShouldInclude(Map map, Genepack genepack)
        {
            return ShouldInclude(
                map,
                genepack,
                currentGenepack => currentGenepack.MapHeld,
                currentGenepack => currentGenepack.SpawnedOrAnyParentSpawned,
                HasForeignFactionOwner,
                GetGenes);
        }

        internal static bool ShouldInclude(
            Map map,
            Genepack genepack,
            Func<Genepack, Map> getMapHeld,
            Func<Genepack, bool> isPhysicallyMapRooted,
            Func<Genepack, bool> hasForeignFactionOwner,
            Func<Genepack, IEnumerable<GeneDef>> getGenes)
        {
            if (map == null)
                throw new ArgumentNullException(nameof(map));

            if (genepack == null)
                return false;

            if (getMapHeld == null)
                throw new ArgumentNullException(nameof(getMapHeld));

            if (isPhysicallyMapRooted == null)
                throw new ArgumentNullException(nameof(isPhysicallyMapRooted));

            if (hasForeignFactionOwner == null)
                throw new ArgumentNullException(nameof(hasForeignFactionOwner));

            if (getGenes == null)
                throw new ArgumentNullException(nameof(getGenes));

            if (!ReferenceEquals(getMapHeld(genepack), map) || !isPhysicallyMapRooted(genepack))
                return false;

            if (hasForeignFactionOwner(genepack))
                return false;

            IEnumerable<GeneDef> genes = getGenes(genepack);

            if (genes == null)
                return false;

            foreach (GeneDef gene in genes)
            {
                if (gene != null)
                    return true;
            }

            return false;
        }

        private static bool HasForeignFactionOwner(Genepack genepack)
        {
            Faction playerFaction = Faction.OfPlayer;

            if (IsForeignFaction(genepack.Faction, playerFaction))
                return true;

            IThingHolder holder = genepack.ParentHolder;

            while (holder != null)
            {
                if (holder is PassingShip || IsForeignFaction(GetHolderFaction(holder), playerFaction))
                    return true;

                holder = holder.ParentHolder;
            }

            return false;
        }

        private static Faction GetHolderFaction(IThingHolder holder)
        {
            if (holder is Thing thing)
                return thing.Faction;

            if (holder is ThingComp thingComp)
                return thingComp.parent?.Faction;

            return null;
        }

        private static bool IsForeignFaction(Faction faction, Faction playerFaction)
        {
            return faction != null && !ReferenceEquals(faction, playerFaction);
        }

        private static IEnumerable<GeneDef> GetGenes(Genepack genepack)
        {
            GeneSet geneSet = genepack.GeneSet;

            return geneSet?.GenesListForReading;
        }
    }
}