using System;
using System.Collections.Generic;
using NUnit.Framework;
using RimWorld;
using Verse;
using XenogermPlanner.Analysis;
using XenogermPlanner.Genes;
using XenogermPlanner.Notifications;
using XenogermPlanner.Plans;

namespace XenogermPlanner.Tests.Notifications
{
    [TestFixture]
    public sealed class PlanReadinessNotificationGameComponentTests
    {
        [Test]
        public void FirstTickUsesOneSnapshotForAllPlansAndEstablishesBaselines()
        {
            XenogermPlan firstPlan = CreatePlan("First");
            XenogermPlan secondPlan = CreatePlan("Second");
            PlanGenepackInventorySnapshot snapshot = CreateSnapshot();
            var snapshotReads = 0;
            var analyzedPlans = new List<XenogermPlan>();
            var announcements = new List<XenogermPlan>();
            PlanReadinessNotificationGameComponent component = CreateComponent(
                () => new[] { firstPlan, secondPlan },
                () =>
                {
                    snapshotReads++;
                    return snapshot;
                },
                (plan, actualSnapshot) =>
                {
                    Assert.That(actualSnapshot, Is.SameAs(snapshot));
                    analyzedPlans.Add(plan);
                    return CreateNotReady();
                },
                announcements.Add);

            component.GameComponentTick();

            Assert.That(snapshotReads, Is.EqualTo(1));
            Assert.That(analyzedPlans, Is.EqualTo(new[] { firstPlan, secondPlan }));
            Assert.That(announcements, Is.Empty);
            Assert.That(firstPlan.HasReadinessNotificationBaseline, Is.True);
            Assert.That(secondPlan.HasReadinessNotificationBaseline, Is.True);
        }

        [Test]
        public void UnchangedSnapshotDoesNotRepeatAnalysis()
        {
            XenogermPlan plan = CreatePlan("Plan");
            PlanGenepackInventorySnapshot snapshot = CreateSnapshot();
            var analysisCount = 0;
            PlanReadinessNotificationGameComponent component = CreateComponent(
                () => new[] { plan },
                () => snapshot,
                (_, __) =>
                {
                    analysisCount++;
                    return CreateNotReady();
                });

            component.GameComponentTick();
            component.GameComponentTick();

            Assert.That(analysisCount, Is.EqualTo(1));
        }

        [Test]
        public void NewSnapshotsDriveReadinessTransitionsWithoutRepeatedAnnouncements()
        {
            XenogermPlan plan = CreatePlan("Plan");

            var state = new ReadinessTestState(CreateSnapshot(), CreateNotReady());

            var announcements = new List<XenogermPlan>();

            PlanReadinessNotificationGameComponent component = CreateComponent(
                () => new[] { plan },
                () => state.Snapshot,
                (_, __) => state.Result,
                announcements.Add);

            component.GameComponentTick();

            state.Result = CreateReady();
            state.Snapshot = CreateSnapshot();
            component.GameComponentTick();

            state.Snapshot = CreateSnapshot();
            component.GameComponentTick();

            state.Result = CreateNotReady();
            state.Snapshot = CreateSnapshot();
            component.GameComponentTick();

            state.Result = CreateReady();
            state.Snapshot = CreateSnapshot();
            component.GameComponentTick();

            Assert.That(announcements, Is.EqualTo(new[] { plan, plan }));
        }

        [Test]
        public void InvalidateTriggersEvaluationWithSameSnapshot()
        {
            XenogermPlan plan = CreatePlan("Plan");
            PlanGenepackInventorySnapshot snapshot = CreateSnapshot();
            var analysisCount = 0;
            PlanReadinessNotificationGameComponent component = CreateComponent(
                () => new[] { plan },
                () => snapshot,
                (_, __) =>
                {
                    analysisCount++;
                    return CreateNotReady();
                });

            component.GameComponentTick();
            component.Invalidate();
            component.GameComponentTick();

            Assert.That(analysisCount, Is.EqualTo(2));
        }

        [Test]
        public void DisabledPlanUpdatesStateWithoutAnnouncement()
        {
            var plan = new XenogermPlan(
                "Plan",
                Array.Empty<GeneDef>(),
                PlanReadinessMode.Coverage,
                readinessNotificationsEnabled: false);
            plan.UpdateReadinessNotificationState(false);
            var announcements = new List<XenogermPlan>();
            PlanReadinessNotificationGameComponent component = CreateComponent(
                () => new[] { plan },
                CreateSnapshot,
                (_, __) => CreateReady(),
                announcements.Add);

            component.GameComponentTick();

            Assert.That(announcements, Is.Empty);
            Assert.That(plan.LastReadinessNotificationStateWasReady, Is.True);
        }

        [Test]
        public void ExceptionForOnePlanDoesNotBlockOtherPlans()
        {
            XenogermPlan failingPlan = CreatePlan("Failing");
            XenogermPlan readyPlan = CreatePlan("Ready");
            readyPlan.UpdateReadinessNotificationState(false);
            var announcements = new List<XenogermPlan>();
            var errors = new List<string>();
            PlanReadinessNotificationGameComponent component = CreateComponent(
                () => new[] { failingPlan, readyPlan },
                CreateSnapshot,
                (plan, _) =>
                {
                    if (ReferenceEquals(plan, failingPlan))
                        throw new InvalidOperationException("Expected test failure.");

                    return CreateReady();
                },
                announcements.Add,
                errors.Add);

            component.GameComponentTick();

            Assert.That(announcements, Is.EqualTo(new[] { readyPlan }));
            Assert.That(errors, Has.Count.EqualTo(1));
            Assert.That(errors[0], Does.Contain(failingPlan.Id));
        }

        [Test]
        public void NullPlansOrSnapshotAreSafeNoOps()
        {
            var analysisCount = 0;
            PlanReadinessNotificationGameComponent noPlansComponent = CreateComponent(
                () => null,
                CreateSnapshot,
                (_, __) =>
                {
                    analysisCount++;
                    return CreateReady();
                });
            PlanReadinessNotificationGameComponent noSnapshotComponent = CreateComponent(
                Array.Empty<XenogermPlan>,
                () => null,
                (_, __) =>
                {
                    analysisCount++;
                    return CreateReady();
                });

            noPlansComponent.GameComponentTick();
            noSnapshotComponent.GameComponentTick();

            Assert.That(analysisCount, Is.Zero);
        }

        [Test]
        public void UnavailableSnapshotDoesNotChangeExistingBaseline()
        {
            XenogermPlan plan = CreatePlan("Plan");
            plan.UpdateReadinessNotificationState(false);
            PlanReadinessNotificationGameComponent component = CreateComponent(
                () => new[] { plan },
                () => PlanGenepackInventorySnapshot.Unavailable,
                (_, __) => PlanReadinessResult.CreateUnavailable(PlanReadinessUnavailableReason.NoActiveMap));

            component.GameComponentTick();

            Assert.That(plan.HasReadinessNotificationBaseline, Is.True);
            Assert.That(plan.LastReadinessNotificationStateWasReady, Is.False);
        }

        private static PlanReadinessNotificationGameComponent CreateComponent(
            Func<IReadOnlyList<XenogermPlan>> getPlans,
            Func<PlanGenepackInventorySnapshot> getSnapshot,
            Func<XenogermPlan, PlanGenepackInventorySnapshot, PlanReadinessResult> analyze,
            Action<XenogermPlan> announce = null,
            Action<string> reportError = null)
        {
            return new PlanReadinessNotificationGameComponent(
                getPlans,
                getSnapshot,
                analyze,
                announce ?? (_ => { }),
                reportError ?? (_ => { }));
        }

        private static XenogermPlan CreatePlan(string name)
        {
            return new XenogermPlan(name, Array.Empty<GeneDef>(), PlanReadinessMode.Coverage);
        }

        private static PlanGenepackInventorySnapshot CreateSnapshot()
        {
            return PlanGenepackInventorySnapshot.CreateAvailable(Array.Empty<Genepack>());
        }

        private static PlanReadinessResult CreateReady()
        {
            return PlanReadinessResult.CreateReady(Array.Empty<GeneDef>());
        }

        private static PlanReadinessResult CreateNotReady()
        {
            return PlanReadinessResult.CreateNotReady(
                Array.Empty<GeneDef>(),
                Array.Empty<GeneDef>(),
                hasExactPayloadConflict: false);
        }

        private sealed class ReadinessTestState
        {
            public ReadinessTestState(PlanGenepackInventorySnapshot snapshot, PlanReadinessResult result)
            {
                Snapshot = snapshot;
                Result = result;
            }

            public PlanGenepackInventorySnapshot Snapshot { get; set; }

            public PlanReadinessResult Result { get; set; }
        }
    }
}