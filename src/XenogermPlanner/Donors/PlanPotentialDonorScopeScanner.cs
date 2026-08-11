using System;
using System.Collections.Generic;
using Verse;

namespace XenogermPlanner.Donors
{
    internal static class PlanPotentialDonorScopeScanner
    {
        internal static PlanPotentialDonorScopeSnapshot Scan(Map map)
        {
            if (map == null)
                return PlanPotentialDonorScopeSnapshot.Unavailable;

            return Scan(map, DiscoverPawns, IsSpawnedOnMap, HasGeneTracker);
        }

        internal static PlanPotentialDonorScopeSnapshot Scan(
            Map map,
            Func<Map, IEnumerable<Pawn>> discoverPawns,
            Func<Map, Pawn, bool> isSpawnedOnMap,
            Func<Pawn, bool> hasGeneTracker)
        {
            if (map == null)
                return PlanPotentialDonorScopeSnapshot.Unavailable;

            if (discoverPawns == null)
                throw new ArgumentNullException(nameof(discoverPawns));

            if (isSpawnedOnMap == null)
                throw new ArgumentNullException(nameof(isSpawnedOnMap));

            if (hasGeneTracker == null)
                throw new ArgumentNullException(nameof(hasGeneTracker));

            var includedPawns = new List<Pawn>();
            IEnumerable<Pawn> discoveredPawns = discoverPawns(map);

            if (discoveredPawns == null)
                return PlanPotentialDonorScopeSnapshot.CreateAvailable(includedPawns);

            foreach (Pawn pawn in discoveredPawns)
            {
                if (pawn == null || !isSpawnedOnMap(map, pawn) || !hasGeneTracker(pawn))
                    continue;

                includedPawns.Add(pawn);
            }

            return PlanPotentialDonorScopeSnapshot.CreateAvailable(includedPawns);
        }

        private static IEnumerable<Pawn> DiscoverPawns(Map map)
        {
            return map.mapPawns.AllPawnsSpawned;
        }

        private static bool IsSpawnedOnMap(Map map, Pawn pawn)
        {
            return pawn.Spawned && ReferenceEquals(pawn.Map, map);
        }

        private static bool HasGeneTracker(Pawn pawn)
        {
            return pawn.genes != null;
        }
    }
}