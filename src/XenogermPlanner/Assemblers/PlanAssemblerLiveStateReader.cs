using System;
using System.Collections.Generic;
using RimWorld;
using Verse;

namespace XenogermPlanner.Assemblers
{
    internal static class PlanAssemblerLiveStateReader
    {
        internal static PlanAssemblerLiveState Read(Building_GeneAssembler assembler)
        {
            return Read(
                assembler,
                PlanAssemblerScopeScanner.Scan,
                IsAssemblerPowered,
                value => value.MaxComplexity(),
                HasArchogeneticsResearch,
                CountAvailableArchiteCapsules);
        }

        private static PlanAssemblerLiveState Read(
            Building_GeneAssembler assembler,
            Func<Building_GeneAssembler, PlanAssemblerScopeSnapshot> scanScope,
            Func<Building_GeneAssembler, bool> isAssemblerPowered,
            Func<Building_GeneAssembler, int> getMaxComplexity,
            Func<Building_GeneAssembler, bool> isArchogeneticsFinished,
            Func<Building_GeneAssembler, int> getAvailableArchiteCapsules)
        {
            if (assembler == null)
                throw new ArgumentNullException(nameof(assembler));

            if (scanScope == null)
                throw new ArgumentNullException(nameof(scanScope));

            if (isAssemblerPowered == null)
            {
                throw new ArgumentNullException(nameof(isAssemblerPowered));
            }

            if (getMaxComplexity == null)
                throw new ArgumentNullException(nameof(getMaxComplexity));

            if (isArchogeneticsFinished == null)
            {
                throw new ArgumentNullException(nameof(isArchogeneticsFinished));
            }

            if (getAvailableArchiteCapsules == null)
            {
                throw new ArgumentNullException(nameof(getAvailableArchiteCapsules));
            }

            PlanAssemblerScopeSnapshot scope = scanScope(assembler) ??
                                               throw new InvalidOperationException(
                                                   "Assembler scope scan returned a null snapshot.");

            return new PlanAssemblerLiveState(
                scope,
                isAssemblerPowered(assembler),
                getMaxComplexity(assembler),
                getAvailableArchiteCapsules(assembler),
                isArchogeneticsFinished(assembler));
        }

        private static bool IsAssemblerPowered(Building_GeneAssembler assembler)
        {
            CompPowerTrader power = assembler.GetComp<CompPowerTrader>();

            return power == null || power.PowerOn;
        }

        private static bool HasArchogeneticsResearch(Building_GeneAssembler assembler)
        {
            return ResearchProjectDefOf.Archogenetics.IsFinished;
        }

        private static int CountAvailableArchiteCapsules(Building_GeneAssembler assembler)
        {
            Map map = assembler.Map;

            if (map == null)
                return 0;

            List<Thing> capsules = map.listerThings.ThingsOfDef(ThingDefOf.ArchiteCapsule);

            var availableCount = 0;

            foreach (Thing capsule in capsules)
            {
                if (capsule == null || capsule.Position.Fogged(map))
                {
                    continue;
                }

                availableCount += capsule.stackCount;
            }

            return availableCount;
        }
    }
}