using System;
using System.Collections.Generic;
using NUnit.Framework;
using RimWorld;
using Verse;
using XenogermPlanner.Analysis;
using XenogermPlanner.Genes;
using XenogermPlanner.Plans;

namespace XenogermPlanner.Tests.Trade
{
    [TestFixture]
    public sealed class PlanTraderAdvisoryRegressionTests
    {
        [Test]
        public void RelevantTraderOfferDoesNotEnterProductInventoryOrChangeProductReadiness()
        {
            GeneDef gene = CreateGene("GeneA");
            XenogermPlan plan = CreatePlan("plan-id", "Plan", PlanReadinessMode.Coverage, gene);
            PlanGenepackInventorySnapshot inventory = EmptyInventory();
            PlanReadinessResult readinessBefore = PlanReadinessAnalyzer.Analyze(plan, inventory);
            var analyzer = new PlanGenepackRelevanceAnalyzer(new[] { plan }, inventory);

            IReadOnlyList<XenogermPlan> matches = analyzer.Evaluate(new[] { gene });
            PlanReadinessResult readinessAfter = PlanReadinessAnalyzer.Analyze(plan, inventory);

            Assert.That(matches, Has.Count.EqualTo(1));
            Assert.That(matches[0], Is.SameAs(plan));
            Assert.That(inventory.Genepacks, Is.Empty);
            Assert.That(readinessBefore.Status, Is.EqualTo(PlanReadinessStatus.NotReady));
            Assert.That(readinessAfter.Status, Is.EqualTo(PlanReadinessStatus.NotReady));
            Assert.That(readinessBefore.MissingGenes, Is.EquivalentTo(new[] { gene }));
            Assert.That(readinessAfter.MissingGenes, Is.EquivalentTo(new[] { gene }));
        }

        [Test]
        public void TraderAdvisoryAnalysisDoesNotChangeReadinessNotificationCursor()
        {
            GeneDef gene = CreateGene("GeneA");
            var plan = new XenogermPlan(
                "plan-id",
                "Plan",
                new[] { gene },
                Array.Empty<string>(),
                PlanReadinessMode.Coverage,
                readinessNotificationsEnabled: false,
                hasReadinessNotificationBaseline: true,
                lastReadinessNotificationStateWasReady: true);
            PlanGenepackInventorySnapshot inventory = EmptyInventory();
            var analyzer = new PlanGenepackRelevanceAnalyzer(new[] { plan }, inventory);

            IReadOnlyList<XenogermPlan> matches = analyzer.Evaluate(new[] { gene });

            Assert.That(matches, Has.Count.EqualTo(1));
            Assert.That(matches[0], Is.SameAs(plan));
            Assert.That(plan.ReadinessNotificationsEnabled, Is.False);
            Assert.That(plan.HasReadinessNotificationBaseline, Is.True);
            Assert.That(plan.LastReadinessNotificationStateWasReady, Is.True);
        }

        [Test]
        public void SharedRelevanceContractPreservesCoverageAndExactPayloadBoundaries()
        {
            GeneDef missingGene = CreateGene("MissingGene");
            GeneDef additionalGene = CreateGene("AdditionalGene");
            XenogermPlan coveragePlan = CreatePlan(
                "coverage-plan",
                "Coverage",
                PlanReadinessMode.Coverage,
                missingGene);
            var coverageAnalyzer = new PlanGenepackRelevanceAnalyzer(new[] { coveragePlan }, EmptyInventory());

            IReadOnlyList<XenogermPlan> coverageMatches =
                coverageAnalyzer.Evaluate(new[] { missingGene, additionalGene });

            Assert.That(coverageMatches, Is.EqualTo(new[] { coveragePlan }));

            GeneDef exactMissingGene = CreateGene("ExactMissingGene");
            GeneDef outsideGene = CreateGene("OutsideGene");
            XenogermPlan exactMissingPlan = CreatePlan(
                "exact-missing-plan",
                "Exact Missing",
                PlanReadinessMode.ExactPayload,
                exactMissingGene);
            var exactMissingAnalyzer = new PlanGenepackRelevanceAnalyzer(new[] { exactMissingPlan }, EmptyInventory());

            Assert.That(
                exactMissingAnalyzer.Evaluate(new[] { exactMissingGene }),
                Is.EqualTo(new[] { exactMissingPlan }));
            Assert.That(exactMissingAnalyzer.Evaluate(new[] { exactMissingGene, outsideGene }), Is.Empty);

            GeneDef conflictGene = CreateGene("ConflictGene");
            XenogermPlan exactConflictPlan = CreatePlan(
                "exact-conflict-plan",
                "Exact Conflict",
                PlanReadinessMode.ExactPayload,
                conflictGene);
            PlanReadinessResult conflictReadiness = CreateExactPayloadConflictResult(conflictGene);
            var exactConflictAnalyzer = new PlanGenepackRelevanceAnalyzer(
                new[] { exactConflictPlan },
                EmptyInventory(),
                (_, __) => conflictReadiness);

            Assert.That(
                exactConflictAnalyzer.Evaluate(new[] { conflictGene }),
                Is.EqualTo(new[] { exactConflictPlan }));
        }

        private static PlanGenepackInventorySnapshot EmptyInventory()
        {
            return PlanGenepackInventorySnapshot.CreateAvailable(Array.Empty<Genepack>());
        }

        private static PlanReadinessResult CreateExactPayloadConflictResult(GeneDef gene)
        {
            GeneDef additionalGene = CreateGene(gene.defName + "_Additional");
            var sourceComposition = new PlanGenepackCompositionDiagnostic(
                new[] { gene, additionalGene },
                1,
                false,
                new[] { additionalGene });
            var diagnostic = new PlanGeneCoverageDiagnostic(
                gene,
                PlanGeneCoverageState.ExactPayloadConflict,
                new[] { sourceComposition });

            return PlanReadinessResult.CreateNotReady(
                new[] { gene },
                Array.Empty<GeneDef>(),
                true,
                new[] { diagnostic });
        }

        private static XenogermPlan CreatePlan(
            string id,
            string name,
            PlanReadinessMode readinessMode,
            params GeneDef[] genes)
        {
            return new XenogermPlan(id, name, genes, Array.Empty<string>(), readinessMode);
        }

        private static GeneDef CreateGene(string defName)
        {
            return new GeneDef { defName = defName };
        }
    }
}