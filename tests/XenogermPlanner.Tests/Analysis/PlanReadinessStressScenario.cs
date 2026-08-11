using System;
using System.Collections.Generic;
using System.Linq;
using Verse;
using XenogermPlanner.Analysis;
using XenogermPlanner.Plans;

namespace XenogermPlanner.Tests.Analysis
{
    internal sealed class PlanReadinessStressScenario
    {
        private const int PlannedGeneCount = 512;
        private const int ExtraGeneCount = 512;
        private const int MaximumGenesPerPack = 4;
        internal const int PhysicalPackCount = 2048;

        internal XenogermPlan Plan { get; }
        internal PlanReadinessTestData.PackFixture[] Packs { get; }
        internal PlanReadinessStatus ExpectedStatus { get; }
        internal int ExpectedCoveredGeneCount { get; }
        internal int ExpectedMissingGeneCount { get; }
        internal int ExpectedExactPayloadConflictGeneCount { get; }
        internal bool ExpectedExactPayloadConflict { get; }

        private PlanReadinessStressScenario(
            XenogermPlan plan,
            IEnumerable<PlanReadinessTestData.PackFixture> packs,
            PlanReadinessStatus expectedStatus,
            int expectedCoveredGeneCount,
            int expectedMissingGeneCount,
            int expectedExactPayloadConflictGeneCount,
            bool expectedExactPayloadConflict)
        {
            Plan = plan ?? throw new ArgumentNullException(nameof(plan));

            if (packs == null)
                throw new ArgumentNullException(nameof(packs));

            Packs = packs.ToArray();
            ExpectedStatus = expectedStatus;
            ExpectedCoveredGeneCount = expectedCoveredGeneCount;
            ExpectedMissingGeneCount = expectedMissingGeneCount;
            ExpectedExactPayloadConflictGeneCount = expectedExactPayloadConflictGeneCount;
            ExpectedExactPayloadConflict = expectedExactPayloadConflict;

            Validate();
        }

        internal static PlanReadinessStressScenario CreateCoverageReady()
        {
            GeneDef[] targetGenes = PlanReadinessTestData.CreateGenes("TargetGene", PlannedGeneCount);

            GeneDef[] extraGenes = PlanReadinessTestData.CreateGenes("ExtraGene", ExtraGeneCount);

            var baseCompositions = new List<GeneDef[]>(PlannedGeneCount);

            for (var index = 0; index < PlannedGeneCount; index++)
            {
                switch (index % 4)
                {
                    case 0:
                        baseCompositions.Add(
                            new[]
                            {
                                targetGenes[index]
                            });
                        break;

                    case 1:
                        baseCompositions.Add(
                            new[]
                            {
                                targetGenes[index],
                                extraGenes[index]
                            });
                        break;

                    case 2:
                        baseCompositions.Add(
                            new[]
                            {
                                targetGenes[index],
                                targetGenes[(index + 1) % PlannedGeneCount],
                                extraGenes[index]
                            });
                        break;

                    default:
                        baseCompositions.Add(
                            new[]
                            {
                                targetGenes[index],
                                extraGenes[index],
                                extraGenes[(index + 1) % ExtraGeneCount],
                                extraGenes[(index + 2) % ExtraGeneCount]
                            });
                        break;
                }
            }

            return new PlanReadinessStressScenario(
                PlanReadinessTestData.CreatePlan(PlanReadinessMode.Coverage, targetGenes),
                RepeatCompositions(baseCompositions, 4),
                PlanReadinessStatus.Ready,
                PlannedGeneCount,
                0,
                0,
                false);
        }

        internal static PlanReadinessStressScenario CreateExactReady()
        {
            GeneDef[] targetGenes = PlanReadinessTestData.CreateGenes("TargetGene", PlannedGeneCount);

            GeneDef[] extraGenes = PlanReadinessTestData.CreateGenes("ExtraGene", ExtraGeneCount);

            var packs = new List<PlanReadinessTestData.PackFixture>(PhysicalPackCount);

            for (var index = 0; index < PlannedGeneCount; index++)
            {
                packs.Add(PlanReadinessTestData.CreatePack(targetGenes[index]));

                packs.Add(PlanReadinessTestData.CreatePack(targetGenes[index]));
            }

            for (var groupIndex = 0; groupIndex < PlannedGeneCount / 4; groupIndex++)
            {
                int geneIndex = groupIndex * 4;
                GeneDef[] composition =
                {
                    targetGenes[geneIndex],
                    targetGenes[geneIndex + 1],
                    targetGenes[geneIndex + 2],
                    targetGenes[geneIndex + 3]
                };

                AddCompositionCopies(packs, composition, 4);
            }

            for (var groupIndex = 0; groupIndex < PlannedGeneCount / 4; groupIndex++)
            {
                int geneIndex = groupIndex * 4;
                GeneDef[] composition =
                {
                    targetGenes[geneIndex],
                    targetGenes[geneIndex + 1],
                    extraGenes[groupIndex]
                };

                AddCompositionCopies(packs, composition, 4);
            }

            return new PlanReadinessStressScenario(
                PlanReadinessTestData.CreatePlan(PlanReadinessMode.ExactPayload, targetGenes),
                packs,
                PlanReadinessStatus.Ready,
                PlannedGeneCount,
                0,
                0,
                false);
        }

        internal static PlanReadinessStressScenario CreateExactConflict()
        {
            GeneDef[] targetGenes = PlanReadinessTestData.CreateGenes("TargetGene", PlannedGeneCount);

            GeneDef[] extraGenes = PlanReadinessTestData.CreateGenes("ExtraGene", ExtraGeneCount);

            var baseCompositions = new List<GeneDef[]>(PlannedGeneCount);

            for (var index = 0; index < PlannedGeneCount; index++)
            {
                if (index < 384)
                {
                    baseCompositions.Add(
                        new[]
                        {
                            targetGenes[index]
                        });
                }
                else
                {
                    baseCompositions.Add(
                        new[]
                        {
                            targetGenes[index],
                            extraGenes[index - 384]
                        });
                }
            }

            return new PlanReadinessStressScenario(
                PlanReadinessTestData.CreatePlan(PlanReadinessMode.ExactPayload, targetGenes),
                RepeatCompositions(baseCompositions, 4),
                PlanReadinessStatus.NotReady,
                PlannedGeneCount,
                0,
                128,
                true);
        }

        private static List<PlanReadinessTestData.PackFixture> RepeatCompositions(
            IReadOnlyList<GeneDef[]> compositions,
            int copiesPerComposition)
        {
            var packs = new List<PlanReadinessTestData.PackFixture>(compositions.Count * copiesPerComposition);

            foreach (GeneDef[] composition in compositions)
            {
                AddCompositionCopies(packs, composition, copiesPerComposition);
            }

            return packs;
        }

        private static void AddCompositionCopies(
            ICollection<PlanReadinessTestData.PackFixture> packs,
            GeneDef[] composition,
            int copyCount)
        {
            for (var copyIndex = 0; copyIndex < copyCount; copyIndex++)
            {
                packs.Add(PlanReadinessTestData.CreatePack(composition));
            }
        }

        private void Validate()
        {
            if (Plan.DesiredGenes.Count != PlannedGeneCount)
            {
                throw new InvalidOperationException(
                    $"Stress scenario must contain exactly {PlannedGeneCount} planned genes.");
            }

            if (Packs.Length != PhysicalPackCount)
            {
                throw new InvalidOperationException(
                    $"Stress scenario must contain exactly {PhysicalPackCount} physical genepacks.");
            }

            foreach (PlanReadinessTestData.PackFixture pack in Packs)
            {
                if (pack == null)
                {
                    throw new InvalidOperationException("Stress scenario cannot contain a null pack fixture.");
                }

                if (pack.Genes.Count < 1 || pack.Genes.Count > MaximumGenesPerPack)
                {
                    throw new InvalidOperationException(
                        "Every stress-scenario genepack must contain between one and four genes.");
                }

                foreach (GeneDef gene in pack.Genes)
                {
                    if (gene == null)
                    {
                        throw new InvalidOperationException("Stress-scenario genepacks cannot contain null genes.");
                    }
                }
            }
        }
    }
}