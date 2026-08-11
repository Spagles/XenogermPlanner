using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using NUnit.Framework;
using RimWorld;
using Verse;
using XenogermPlanner.Analysis;
using XenogermPlanner.Assemblers;
using XenogermPlanner.Donors;
using XenogermPlanner.Genes;
using XenogermPlanner.Plans;
using XenogermPlanner.UI;

namespace XenogermPlanner.Tests.UI
{
    [TestFixture]
    public sealed class XenogermPlannerWindowAnalysisCacheTests
    {
        [Test]
        public void ProductReadiness_ReusesResultForStableInputs()
        {
            var fixture = new CacheFixture();
            XenogermPlannerWindowAnalysisCache cache = fixture.CreateCache();

            PlanReadinessResult first = cache.GetProductReadiness(fixture.Plan, fixture.InventorySnapshot);
            PlanReadinessResult second = cache.GetProductReadiness(fixture.Plan, fixture.InventorySnapshot);

            Assert.That(second, Is.SameAs(first));
            Assert.That(fixture.ProductReadinessCalls, Is.EqualTo(1));
        }

        [Test]
        public void ProductReadiness_RebuildsForInventoryGenesAndReadinessModeChanges()
        {
            var fixture = new CacheFixture();
            XenogermPlannerWindowAnalysisCache cache = fixture.CreateCache();

            cache.GetProductReadiness(fixture.Plan, fixture.InventorySnapshot);

            var replacementInventory = PlanGenepackInventorySnapshot.CreateAvailable(Array.Empty<Genepack>());
            cache.GetProductReadiness(fixture.Plan, replacementInventory);

            fixture.Plan.ReplaceDesiredGenes(new[] { fixture.Gene });
            cache.GetProductReadiness(fixture.Plan, replacementInventory);

            fixture.Plan.ChangeReadinessMode(PlanReadinessMode.ExactPayload);
            cache.GetProductReadiness(fixture.Plan, replacementInventory);

            Assert.That(fixture.ProductReadinessCalls, Is.EqualTo(4));
        }

        [Test]
        public void ProductReadiness_IgnoresNameAndNotificationChanges()
        {
            var fixture = new CacheFixture();
            XenogermPlannerWindowAnalysisCache cache = fixture.CreateCache();

            PlanReadinessResult first = cache.GetProductReadiness(fixture.Plan, fixture.InventorySnapshot);
            fixture.Plan.Rename("Renamed");
            fixture.Plan.ChangeReadinessNotificationsEnabled(false);
            PlanReadinessResult second = cache.GetProductReadiness(fixture.Plan, fixture.InventorySnapshot);

            Assert.That(second, Is.SameAs(first));
            Assert.That(fixture.ProductReadinessCalls, Is.EqualTo(1));
        }

        [Test]
        public void TargetAnalysis_DependsOnlyOnDesiredGeneCollectionIdentity()
        {
            var fixture = new CacheFixture();
            XenogermPlannerWindowAnalysisCache cache = fixture.CreateCache();

            PlanGeneTargetAnalysisResult first = cache.GetTargetAnalysis(fixture.Plan);
            fixture.Plan.Rename("Renamed");
            fixture.Plan.ChangeReadinessMode(PlanReadinessMode.ExactPayload);
            PlanGeneTargetAnalysisResult second = cache.GetTargetAnalysis(fixture.Plan);

            fixture.Plan.ReplaceDesiredGenes(new[] { fixture.Gene });
            PlanGeneTargetAnalysisResult rebuilt = cache.GetTargetAnalysis(fixture.Plan);

            Assert.That(second, Is.SameAs(first));
            Assert.That(rebuilt, Is.Not.SameAs(first));
            Assert.That(fixture.TargetAnalysisCalls, Is.EqualTo(2));
        }

        [Test]
        public void PotentialDonors_DoesNotScanWithoutMissingGenes()
        {
            var fixture = new CacheFixture();
            XenogermPlannerWindowAnalysisCache cache = fixture.CreateCache();
            var ready = PlanReadinessResult.CreateReady(new[] { fixture.Gene });

            PlanPotentialDonorAnalysisResult result = cache.GetPotentialDonorAnalysis(ready, fixture.Map);

            Assert.That(result, Is.Null);
            Assert.That(fixture.PotentialDonorScopeCalls, Is.Zero);
            Assert.That(fixture.PotentialDonorAnalysisCalls, Is.Zero);
        }

        [Test]
        public void PotentialDonors_RefreshesByIntervalAndImmediatelyForInputChanges()
        {
            var fixture = new CacheFixture();
            XenogermPlannerWindowAnalysisCache cache = fixture.CreateCache();

            PlanPotentialDonorAnalysisResult first = cache.GetPotentialDonorAnalysis(
                fixture.ReadinessResult,
                fixture.Map);
            fixture.CurrentTick = XenogermPlannerWindowAnalysisCache.LiveStateRefreshIntervalTicks - 1;
            PlanPotentialDonorAnalysisResult cached = cache.GetPotentialDonorAnalysis(
                fixture.ReadinessResult,
                fixture.Map);

            fixture.CurrentTick = XenogermPlannerWindowAnalysisCache.LiveStateRefreshIntervalTicks;
            cache.GetPotentialDonorAnalysis(fixture.ReadinessResult, fixture.Map);

            var replacementReadiness = PlanReadinessResult.CreateNotReady(
                Array.Empty<GeneDef>(),
                new[] { fixture.Gene },
                hasExactPayloadConflict: false);
            cache.GetPotentialDonorAnalysis(replacementReadiness, fixture.Map);

            Map replacementMap = CreateUninitialized<Map>();
            cache.GetPotentialDonorAnalysis(replacementReadiness, replacementMap);

            Assert.That(cached, Is.SameAs(first));
            Assert.That(fixture.PotentialDonorScopeCalls, Is.EqualTo(4));
            Assert.That(fixture.PotentialDonorAnalysisCalls, Is.EqualTo(4));
        }

        [Test]
        public void SelectableAssemblers_RefreshesByIntervalAndMapIdentity()
        {
            var fixture = new CacheFixture();
            XenogermPlannerWindowAnalysisCache cache = fixture.CreateCache();

            IReadOnlyList<Building_GeneAssembler> first = cache.GetSelectableAssemblers(fixture.Map);
            IReadOnlyList<Building_GeneAssembler> cached = cache.GetSelectableAssemblers(fixture.Map);

            fixture.CurrentTick = XenogermPlannerWindowAnalysisCache.LiveStateRefreshIntervalTicks;
            cache.GetSelectableAssemblers(fixture.Map);

            cache.GetSelectableAssemblers(CreateUninitialized<Map>());

            Assert.That(cached, Is.SameAs(first));
            Assert.That(fixture.SelectableAssemblerCalls, Is.EqualTo(3));
        }

        [Test]
        public void AssemblerAnalysis_RefreshesByIntervalAndRelevantInputs()
        {
            var fixture = new CacheFixture();
            XenogermPlannerWindowAnalysisCache cache = fixture.CreateCache();

            XenogermPlannerWindowAssemblerAnalysis first = cache.GetAssemblerAnalysis(fixture.Plan, fixture.Assembler);
            XenogermPlannerWindowAssemblerAnalysis cached = cache.GetAssemblerAnalysis(fixture.Plan, fixture.Assembler);

            fixture.CurrentTick = XenogermPlannerWindowAnalysisCache.LiveStateRefreshIntervalTicks;
            cache.GetAssemblerAnalysis(fixture.Plan, fixture.Assembler);

            Building_GeneAssembler replacementAssembler = CreateUninitialized<Building_GeneAssembler>();
            cache.GetAssemblerAnalysis(fixture.Plan, replacementAssembler);

            fixture.Plan.ReplaceDesiredGenes(new[] { fixture.Gene });
            cache.GetAssemblerAnalysis(fixture.Plan, replacementAssembler);

            fixture.Plan.ChangeReadinessMode(PlanReadinessMode.ExactPayload);
            cache.GetAssemblerAnalysis(fixture.Plan, replacementAssembler);

            Assert.That(cached, Is.SameAs(first));
            Assert.That(fixture.AssemblerLiveStateCalls, Is.EqualTo(5));
            Assert.That(fixture.AssemblerReadinessCalls, Is.EqualTo(5));
        }

        [Test]
        public void LiveStateCaches_RefreshWhenTickMovesBackwards()
        {
            var fixture = new CacheFixture { CurrentTick = 100 };
            XenogermPlannerWindowAnalysisCache cache = fixture.CreateCache();

            cache.GetPotentialDonorAnalysis(fixture.ReadinessResult, fixture.Map);
            cache.GetSelectableAssemblers(fixture.Map);
            cache.GetAssemblerAnalysis(fixture.Plan, fixture.Assembler);

            fixture.CurrentTick = 10;

            cache.GetPotentialDonorAnalysis(fixture.ReadinessResult, fixture.Map);
            cache.GetSelectableAssemblers(fixture.Map);
            cache.GetAssemblerAnalysis(fixture.Plan, fixture.Assembler);

            Assert.That(fixture.PotentialDonorScopeCalls, Is.EqualTo(2));
            Assert.That(fixture.SelectableAssemblerCalls, Is.EqualTo(2));
            Assert.That(fixture.AssemblerLiveStateCalls, Is.EqualTo(2));
        }

        [Test]
        public void Invalidate_ForcesAllCachedResultsToRebuild()
        {
            var fixture = new CacheFixture();
            XenogermPlannerWindowAnalysisCache cache = fixture.CreateCache();

            cache.GetProductReadiness(fixture.Plan, fixture.InventorySnapshot);
            cache.GetTargetAnalysis(fixture.Plan);
            cache.GetPotentialDonorAnalysis(fixture.ReadinessResult, fixture.Map);
            cache.GetSelectableAssemblers(fixture.Map);
            cache.GetAssemblerAnalysis(fixture.Plan, fixture.Assembler);

            cache.Invalidate();

            cache.GetProductReadiness(fixture.Plan, fixture.InventorySnapshot);
            cache.GetTargetAnalysis(fixture.Plan);
            cache.GetPotentialDonorAnalysis(fixture.ReadinessResult, fixture.Map);
            cache.GetSelectableAssemblers(fixture.Map);
            cache.GetAssemblerAnalysis(fixture.Plan, fixture.Assembler);

            Assert.That(fixture.ProductReadinessCalls, Is.EqualTo(2));
            Assert.That(fixture.TargetAnalysisCalls, Is.EqualTo(2));
            Assert.That(fixture.PotentialDonorScopeCalls, Is.EqualTo(2));
            Assert.That(fixture.SelectableAssemblerCalls, Is.EqualTo(2));
            Assert.That(fixture.AssemblerLiveStateCalls, Is.EqualTo(2));
        }

        private static T CreateUninitialized<T>() where T : class
        {
            return (T)FormatterServices.GetUninitializedObject(typeof(T));
        }

        private sealed class CacheFixture
        {
            internal GeneDef Gene { get; }
            internal XenogermPlan Plan { get; }
            internal PlanGenepackInventorySnapshot InventorySnapshot { get; }
            internal PlanReadinessResult ReadinessResult { get; }
            internal PlanPotentialDonorScopeSnapshot PotentialDonorScope { get; }
            internal PlanPotentialDonorAnalysisResult PotentialDonorAnalysisResult { get; }
            internal Map Map { get; }
            internal Building_GeneAssembler Assembler { get; }
            internal PlanAssemblerLiveState AssemblerLiveState { get; }
            internal PlanAssemblerReadinessResult AssemblerReadinessResult { get; }
            internal IReadOnlyList<Building_GeneAssembler> SelectableAssemblers { get; }

            internal int CurrentTick { get; set; }
            internal int ProductReadinessCalls { get; private set; }
            internal int TargetAnalysisCalls { get; private set; }
            internal int PotentialDonorScopeCalls { get; private set; }
            internal int PotentialDonorAnalysisCalls { get; private set; }
            internal int SelectableAssemblerCalls { get; private set; }
            internal int AssemblerLiveStateCalls { get; private set; }
            internal int AssemblerReadinessCalls { get; private set; }

            internal CacheFixture()
            {
                Gene = new GeneDef { defName = "Gene" };
                Plan = new XenogermPlan("Plan", new[] { Gene }, PlanReadinessMode.Coverage);
                InventorySnapshot = PlanGenepackInventorySnapshot.CreateAvailable(Array.Empty<Genepack>());
                ReadinessResult = PlanReadinessResult.CreateNotReady(
                    Array.Empty<GeneDef>(),
                    new[] { Gene },
                    hasExactPayloadConflict: false);
                PotentialDonorScope = PlanPotentialDonorScopeSnapshot.CreateAvailable(Array.Empty<Pawn>());
                PotentialDonorAnalysisResult = PlanPotentialDonorAnalysisResult.CreateAvailable(
                    new[] { new PlanPotentialDonorGeneDiagnostic(Gene, Array.Empty<Pawn>()) });
                Map = CreateUninitialized<Map>();
                Assembler = CreateUninitialized<Building_GeneAssembler>();
                SelectableAssemblers = new[] { Assembler };
                AssemblerLiveState = new PlanAssemblerLiveState(
                    new PlanAssemblerScopeSnapshot(Array.Empty<PlanAssemblerGenepackSource>()),
                    isAssemblerPowered: true,
                    maxComplexity: 0,
                    availableArchiteCapsules: 0,
                    isArchogeneticsFinished: true);
                AssemblerReadinessResult = PlanAssemblerReadinessResult.CreateEmptyTarget(
                    PlanReadinessResult.CreateEmptyTarget(),
                    visibleGenepackCount: 0);
            }

            internal XenogermPlannerWindowAnalysisCache CreateCache()
            {
                return new XenogermPlannerWindowAnalysisCache(
                    () => CurrentTick,
                    (_, __) =>
                    {
                        ProductReadinessCalls++;
                        return ReadinessResult;
                    },
                    _ =>
                    {
                        TargetAnalysisCalls++;
                        return new PlanGeneTargetAnalysisResult(
                            Array.Empty<PlanGeneConflictDiagnostic>(),
                            Array.Empty<PlanGeneRandomChoiceGroupDiagnostic>(),
                            Array.Empty<PlanGenePrerequisiteDiagnostic>());
                    },
                    _ =>
                    {
                        PotentialDonorScopeCalls++;
                        return PotentialDonorScope;
                    },
                    (_, __) =>
                    {
                        PotentialDonorAnalysisCalls++;
                        return PotentialDonorAnalysisResult;
                    },
                    _ =>
                    {
                        SelectableAssemblerCalls++;
                        return SelectableAssemblers;
                    },
                    _ =>
                    {
                        AssemblerLiveStateCalls++;
                        return AssemblerLiveState;
                    },
                    (_, __) =>
                    {
                        AssemblerReadinessCalls++;
                        return AssemblerReadinessResult;
                    });
            }
        }
    }
}