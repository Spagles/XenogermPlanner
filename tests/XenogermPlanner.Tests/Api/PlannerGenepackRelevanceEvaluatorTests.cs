using System;
using System.Collections.Generic;
using NUnit.Framework;
using RimWorld;
using Verse;
using XenogermPlanner.Analysis;
using XenogermPlanner.Api;
using XenogermPlanner.Api.Internal;
using XenogermPlanner.Genes;
using XenogermPlanner.Plans;

namespace XenogermPlanner.Tests.Api
{
    [TestFixture]
    public sealed class PlannerGenepackRelevanceEvaluatorTests
    {
        [Test]
        public void Evaluate_CoverageMissingGene_ReturnsPlanAndAllowsAdditionalGenes()
        {
            GeneDef missingGene = CreateGene("MissingGene");
            GeneDef additionalGene = CreateGene("AdditionalGene");
            XenogermPlan plan = CreatePlan("coverage-plan", "Coverage Plan", PlanReadinessMode.Coverage, missingGene);
            PlanReadinessResult readiness = CreateNotReadyResult(plan, (missingGene, PlanGeneCoverageState.Missing));
            PlannerGenepackRelevanceEvaluator evaluator = CreateEvaluator(plan, readiness);

            GenepackRelevanceItemResult result = evaluator.Evaluate(new[] { missingGene, additionalGene });

            AssertSingleMatch(result, plan);
        }

        [Test]
        public void Evaluate_CoverageAvailableTargetGene_DoesNotReturnPlan()
        {
            GeneDef availableGene = CreateGene("AvailableGene");
            GeneDef missingGene = CreateGene("MissingGene");
            XenogermPlan plan = CreatePlan(
                "coverage-plan",
                "Coverage Plan",
                PlanReadinessMode.Coverage,
                availableGene,
                missingGene);
            PlanReadinessResult readiness = CreateNotReadyResult(
                plan,
                (availableGene, PlanGeneCoverageState.Available),
                (missingGene, PlanGeneCoverageState.Missing));
            PlannerGenepackRelevanceEvaluator evaluator = CreateEvaluator(plan, readiness);

            GenepackRelevanceItemResult result = evaluator.Evaluate(new[] { availableGene });

            AssertSuccessfulWithoutMatches(result);
        }

        [Test]
        public void Evaluate_CoveragePrerequisiteOnlyGene_DoesNotReturnPlan()
        {
            GeneDef targetGene = CreateGene("TargetGene");
            GeneDef prerequisiteGene = CreateGene("PrerequisiteGene");
            XenogermPlan plan = CreatePlan("coverage-plan", "Coverage Plan", PlanReadinessMode.Coverage, targetGene);
            PlanReadinessResult readiness = CreateNotReadyResult(plan, (targetGene, PlanGeneCoverageState.Missing));
            PlannerGenepackRelevanceEvaluator evaluator = CreateEvaluator(plan, readiness);

            GenepackRelevanceItemResult result = evaluator.Evaluate(new[] { prerequisiteGene });

            AssertSuccessfulWithoutMatches(result);
        }

        [Test]
        public void Evaluate_ExactPayloadMissingGene_ReturnsPlan()
        {
            GeneDef missingGene = CreateGene("MissingGene");
            XenogermPlan plan = CreatePlan("exact-plan", "Exact Plan", PlanReadinessMode.ExactPayload, missingGene);
            PlanReadinessResult readiness = CreateNotReadyResult(plan, (missingGene, PlanGeneCoverageState.Missing));
            PlannerGenepackRelevanceEvaluator evaluator = CreateEvaluator(plan, readiness);

            GenepackRelevanceItemResult result = evaluator.Evaluate(new[] { missingGene });

            AssertSingleMatch(result, plan);
        }

        [Test]
        public void Evaluate_ExactPayloadConflictGene_ReturnsPlan()
        {
            GeneDef conflictGene = CreateGene("ConflictGene");
            XenogermPlan plan = CreatePlan("exact-plan", "Exact Plan", PlanReadinessMode.ExactPayload, conflictGene);
            PlanReadinessResult readiness = CreateNotReadyResult(
                plan,
                (conflictGene, PlanGeneCoverageState.ExactPayloadConflict));
            PlannerGenepackRelevanceEvaluator evaluator = CreateEvaluator(plan, readiness);

            GenepackRelevanceItemResult result = evaluator.Evaluate(new[] { conflictGene });

            AssertSingleMatch(result, plan);
        }

        [Test]
        public void Evaluate_ExactPayloadOfferedGeneOutsideTarget_DoesNotReturnPlan()
        {
            GeneDef missingGene = CreateGene("MissingGene");
            GeneDef outsideGene = CreateGene("OutsideGene");
            XenogermPlan plan = CreatePlan("exact-plan", "Exact Plan", PlanReadinessMode.ExactPayload, missingGene);
            PlanReadinessResult readiness = CreateNotReadyResult(plan, (missingGene, PlanGeneCoverageState.Missing));
            PlannerGenepackRelevanceEvaluator evaluator = CreateEvaluator(plan, readiness);

            GenepackRelevanceItemResult result = evaluator.Evaluate(new[] { missingGene, outsideGene });

            AssertSuccessfulWithoutMatches(result);
        }

        [Test]
        public void Evaluate_ExactPayloadOnlyAvailableTargetGenes_DoesNotReturnPlan()
        {
            GeneDef availableGene = CreateGene("AvailableGene");
            GeneDef missingGene = CreateGene("MissingGene");
            XenogermPlan plan = CreatePlan(
                "exact-plan",
                "Exact Plan",
                PlanReadinessMode.ExactPayload,
                availableGene,
                missingGene);
            PlanReadinessResult readiness = CreateNotReadyResult(
                plan,
                (availableGene, PlanGeneCoverageState.Available),
                (missingGene, PlanGeneCoverageState.Missing));
            PlannerGenepackRelevanceEvaluator evaluator = CreateEvaluator(plan, readiness);

            GenepackRelevanceItemResult result = evaluator.Evaluate(new[] { availableGene });

            AssertSuccessfulWithoutMatches(result);
        }

        [TestCase(PlanReadinessStatus.Ready)]
        [TestCase(PlanReadinessStatus.EmptyTarget)]
        [TestCase(PlanReadinessStatus.Degraded)]
        [TestCase(PlanReadinessStatus.Unavailable)]
        public void Evaluate_NonNotReadyState_DoesNotReturnPlan(PlanReadinessStatus status)
        {
            GeneDef gene = CreateGene("GeneA");
            XenogermPlan plan = CreatePlan("plan-id", "Plan Name", PlanReadinessMode.Coverage, gene);
            PlannerGenepackRelevanceEvaluator evaluator = CreateEvaluator(plan, CreateResult(status, gene));

            GenepackRelevanceItemResult result = evaluator.Evaluate(new[] { gene });

            AssertSuccessfulWithoutMatches(result);
        }

        [Test]
        public void Evaluate_NotReadyWithoutRelevantDiagnostics_DoesNotReturnPlan()
        {
            GeneDef gene = CreateGene("GeneA");
            XenogermPlan plan = CreatePlan("plan-id", "Plan Name", PlanReadinessMode.Coverage, gene);
            var readiness = PlanReadinessResult.CreateNotReady(new[] { gene }, Array.Empty<GeneDef>(), false);
            PlannerGenepackRelevanceEvaluator evaluator = CreateEvaluator(plan, readiness);

            GenepackRelevanceItemResult result = evaluator.Evaluate(new[] { gene });

            AssertSuccessfulWithoutMatches(result);
        }

        [Test]
        public void Constructor_AnalyzesEachPlanOnceRegardlessOfEvaluatedCompositionCount()
        {
            GeneDef firstGene = CreateGene("FirstGene");
            GeneDef secondGene = CreateGene("SecondGene");
            XenogermPlan firstPlan = CreatePlan("first-plan", "First Plan", PlanReadinessMode.Coverage, firstGene);
            XenogermPlan secondPlan = CreatePlan("second-plan", "Second Plan", PlanReadinessMode.Coverage, secondGene);
            var callCount = 0;
            var readinessByPlan = new Dictionary<XenogermPlan, PlanReadinessResult>
            {
                [firstPlan] = CreateNotReadyResult(firstPlan, (firstGene, PlanGeneCoverageState.Missing)),
                [secondPlan] = CreateNotReadyResult(secondPlan, (secondGene, PlanGeneCoverageState.Missing))
            };

            var evaluator = new PlannerGenepackRelevanceEvaluator(
                new[] { firstPlan, secondPlan },
                PlanGenepackInventorySnapshot.CreateAvailable(Array.Empty<Genepack>()),
                (plan, _) =>
                {
                    callCount++;
                    return readinessByPlan[plan];
                });

            evaluator.Evaluate(new[] { firstGene });
            evaluator.Evaluate(new[] { secondGene });
            evaluator.Evaluate(new[] { firstGene, secondGene });

            Assert.That(callCount, Is.EqualTo(2));
        }

        [Test]
        public void Evaluate_ReturnsStableIdentityAndDeterministicOrdering()
        {
            GeneDef gene = CreateGene("GeneA");
            XenogermPlan lastByName = CreatePlan("plan-c", "Zulu", PlanReadinessMode.Coverage, gene);
            XenogermPlan secondById = CreatePlan("plan-b", "alpha", PlanReadinessMode.Coverage, gene);
            XenogermPlan firstById = CreatePlan("plan-a", "Alpha", PlanReadinessMode.Coverage, gene);
            var readinessByPlan = new Dictionary<XenogermPlan, PlanReadinessResult>
            {
                [lastByName] = CreateNotReadyResult(lastByName, (gene, PlanGeneCoverageState.Missing)),
                [secondById] = CreateNotReadyResult(secondById, (gene, PlanGeneCoverageState.Missing)),
                [firstById] = CreateNotReadyResult(firstById, (gene, PlanGeneCoverageState.Missing))
            };
            var evaluator = new PlannerGenepackRelevanceEvaluator(
                new[] { lastByName, secondById, firstById },
                PlanGenepackInventorySnapshot.CreateAvailable(Array.Empty<Genepack>()),
                (plan, _) => readinessByPlan[plan]);

            GenepackRelevanceItemResult result = evaluator.Evaluate(new[] { gene });

            Assert.That(result.Status, Is.EqualTo(GenepackRelevanceItemStatus.Success));
            Assert.That(result.Matches, Has.Count.EqualTo(3));
            Assert.That(result.Matches[0].PlanId, Is.EqualTo("plan-a"));
            Assert.That(result.Matches[0].DisplayName, Is.EqualTo("Alpha"));
            Assert.That(result.Matches[1].PlanId, Is.EqualTo("plan-b"));
            Assert.That(result.Matches[1].DisplayName, Is.EqualTo("alpha"));
            Assert.That(result.Matches[2].PlanId, Is.EqualTo("plan-c"));
            Assert.That(result.Matches[2].DisplayName, Is.EqualTo("Zulu"));
        }

        [Test]
        public void Evaluate_EquivalentOfferedSetsInDifferentOrder_ReturnEquivalentResults()
        {
            GeneDef firstGene = CreateGene("FirstGene");
            GeneDef secondGene = CreateGene("SecondGene");
            XenogermPlan plan = CreatePlan("plan-id", "Plan Name", PlanReadinessMode.Coverage, firstGene, secondGene);
            PlanReadinessResult readiness = CreateNotReadyResult(
                plan,
                (firstGene, PlanGeneCoverageState.Missing),
                (secondGene, PlanGeneCoverageState.Missing));
            PlannerGenepackRelevanceEvaluator evaluator = CreateEvaluator(plan, readiness);

            GenepackRelevanceItemResult firstResult = evaluator.Evaluate(new[] { firstGene, secondGene });
            GenepackRelevanceItemResult secondResult = evaluator.Evaluate(new[] { secondGene, firstGene });

            Assert.That(secondResult.Status, Is.EqualTo(firstResult.Status));
            Assert.That(secondResult.Matches, Has.Count.EqualTo(firstResult.Matches.Count));
            Assert.That(secondResult.Matches[0].PlanId, Is.EqualTo(firstResult.Matches[0].PlanId));
            Assert.That(secondResult.Matches[0].DisplayName, Is.EqualTo(firstResult.Matches[0].DisplayName));
        }

        [Test]
        public void Evaluate_DoesNotMutatePlanOwnedState()
        {
            GeneDef gene = CreateGene("GeneA");
            var plan = new XenogermPlan(
                "plan-id",
                "  Plan Name  ",
                new[] { gene },
                Array.Empty<string>(),
                PlanReadinessMode.Coverage,
                readinessNotificationsEnabled: false,
                hasReadinessNotificationBaseline: true,
                lastReadinessNotificationStateWasReady: true);
            string originalName = plan.Name;
            string originalId = plan.Id;
            PlanReadinessMode originalMode = plan.ReadinessMode;
            bool originalNotificationsEnabled = plan.ReadinessNotificationsEnabled;
            bool originalHasBaseline = plan.HasReadinessNotificationBaseline;
            bool originalLastStateWasReady = plan.LastReadinessNotificationStateWasReady;
            GeneDef[] originalGenes = CopyGenes(plan.DesiredGenes);
            PlannerGenepackRelevanceEvaluator evaluator = CreateEvaluator(
                plan,
                CreateNotReadyResult(plan, (gene, PlanGeneCoverageState.Missing)));

            evaluator.Evaluate(new[] { gene });

            Assert.That(plan.Id, Is.EqualTo(originalId));
            Assert.That(plan.Name, Is.EqualTo(originalName));
            Assert.That(plan.ReadinessMode, Is.EqualTo(originalMode));
            Assert.That(plan.ReadinessNotificationsEnabled, Is.EqualTo(originalNotificationsEnabled));
            Assert.That(plan.HasReadinessNotificationBaseline, Is.EqualTo(originalHasBaseline));
            Assert.That(plan.LastReadinessNotificationStateWasReady, Is.EqualTo(originalLastStateWasReady));
            Assert.That(plan.DesiredGenes, Is.EquivalentTo(originalGenes));
        }

        private static PlannerGenepackRelevanceEvaluator CreateEvaluator(
            XenogermPlan plan,
            PlanReadinessResult readiness)
        {
            return new PlannerGenepackRelevanceEvaluator(
                new[] { plan },
                PlanGenepackInventorySnapshot.CreateAvailable(Array.Empty<Genepack>()),
                (_, __) => readiness);
        }

        private static XenogermPlan CreatePlan(
            string id,
            string name,
            PlanReadinessMode readinessMode,
            params GeneDef[] desiredGenes)
        {
            return new XenogermPlan(id, name, desiredGenes, Array.Empty<string>(), readinessMode);
        }

        private static PlanReadinessResult CreateNotReadyResult(
            XenogermPlan plan,
            params (GeneDef Gene, PlanGeneCoverageState State)[] diagnostics)
        {
            var coveredGenes = new List<GeneDef>();
            var missingGenes = new List<GeneDef>();
            var geneDiagnostics = new List<PlanGeneCoverageDiagnostic>();

            foreach ((GeneDef gene, PlanGeneCoverageState state) in diagnostics)
            {
                PlanGeneCoverageDiagnostic diagnostic = CreateDiagnostic(gene, state);
                geneDiagnostics.Add(diagnostic);

                if (diagnostic.IsCovered)
                    coveredGenes.Add(gene);
                else
                    missingGenes.Add(gene);
            }

            return PlanReadinessResult.CreateNotReady(
                coveredGenes,
                missingGenes,
                plan.ReadinessMode == PlanReadinessMode.ExactPayload && missingGenes.Count == 0,
                geneDiagnostics);
        }

        private static PlanGeneCoverageDiagnostic CreateDiagnostic(GeneDef gene, PlanGeneCoverageState state)
        {
            switch (state)
            {
                case PlanGeneCoverageState.Missing:
                    return new PlanGeneCoverageDiagnostic(
                        gene,
                        state,
                        Array.Empty<PlanGenepackCompositionDiagnostic>());

                case PlanGeneCoverageState.Available:
                    return new PlanGeneCoverageDiagnostic(
                        gene,
                        state,
                        new[]
                        {
                            new PlanGenepackCompositionDiagnostic(new[] { gene }, 1, true, Array.Empty<GeneDef>())
                        });

                case PlanGeneCoverageState.ExactPayloadConflict:
                    GeneDef additionalGene = CreateGene(gene.defName + "_Additional");

                    return new PlanGeneCoverageDiagnostic(
                        gene,
                        state,
                        new[]
                        {
                            new PlanGenepackCompositionDiagnostic(
                                new[] { gene, additionalGene },
                                1,
                                false,
                                new[] { additionalGene })
                        });

                default:
                    throw new ArgumentOutOfRangeException(nameof(state), state, "Unknown diagnostic state.");
            }
        }

        private static PlanReadinessResult CreateResult(PlanReadinessStatus status, GeneDef gene)
        {
            switch (status)
            {
                case PlanReadinessStatus.Ready:
                    return PlanReadinessResult.CreateReady(new[] { gene });

                case PlanReadinessStatus.EmptyTarget:
                    return PlanReadinessResult.CreateEmptyTarget();

                case PlanReadinessStatus.Degraded:
                    return PlanReadinessResult.CreateDegraded(Array.Empty<GeneDef>(), new[] { gene });

                case PlanReadinessStatus.Unavailable:
                    return PlanReadinessResult.CreateUnavailable(PlanReadinessUnavailableReason.NoActiveMap);

                default:
                    throw new ArgumentOutOfRangeException(nameof(status), status, "Unsupported test status.");
            }
        }

        private static GeneDef[] CopyGenes(IEnumerable<GeneDef> genes)
        {
            return new List<GeneDef>(genes).ToArray();
        }

        private static GeneDef CreateGene(string defName)
        {
            return new GeneDef { defName = defName };
        }

        private static void AssertSingleMatch(GenepackRelevanceItemResult result, XenogermPlan expectedPlan)
        {
            Assert.That(result.Status, Is.EqualTo(GenepackRelevanceItemStatus.Success));
            Assert.That(result.Matches, Has.Count.EqualTo(1));
            Assert.That(result.Matches[0].PlanId, Is.EqualTo(expectedPlan.Id));
            Assert.That(result.Matches[0].DisplayName, Is.EqualTo(expectedPlan.Name));
        }

        private static void AssertSuccessfulWithoutMatches(GenepackRelevanceItemResult result)
        {
            Assert.That(result.Status, Is.EqualTo(GenepackRelevanceItemStatus.Success));
            Assert.That(result.Matches, Is.Empty);
        }
    }
}