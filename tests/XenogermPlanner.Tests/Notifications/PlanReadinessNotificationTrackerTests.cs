using System;
using NUnit.Framework;
using Verse;
using XenogermPlanner.Analysis;
using XenogermPlanner.Notifications;
using XenogermPlanner.Plans;

namespace XenogermPlanner.Tests.Notifications
{
    [TestFixture]
    public sealed class PlanReadinessNotificationTrackerTests
    {
        [Test]
        public void FirstDeterminateReadyResultEstablishesBaselineWithoutAnnouncement()
        {
            XenogermPlan plan = CreatePlan();

            bool shouldAnnounce = PlanReadinessNotificationTracker.Update(plan, CreateReady());

            Assert.That(shouldAnnounce, Is.False);
            Assert.That(plan.HasReadinessNotificationBaseline, Is.True);
            Assert.That(plan.LastReadinessNotificationStateWasReady, Is.True);
        }

        [Test]
        public void FirstDeterminateNotReadyResultEstablishesBaselineWithoutAnnouncement()
        {
            XenogermPlan plan = CreatePlan();

            bool shouldAnnounce = PlanReadinessNotificationTracker.Update(plan, CreateNotReady());

            Assert.That(shouldAnnounce, Is.False);
            Assert.That(plan.HasReadinessNotificationBaseline, Is.True);
            Assert.That(plan.LastReadinessNotificationStateWasReady, Is.False);
        }

        [TestCase(PlanReadinessStatus.NotReady)]
        [TestCase(PlanReadinessStatus.EmptyTarget)]
        [TestCase(PlanReadinessStatus.Degraded)]
        public void DeterminateNonReadyToReadyAnnouncesOnce(PlanReadinessStatus initialStatus)
        {
            XenogermPlan plan = CreatePlan();
            PlanReadinessNotificationTracker.Update(plan, CreateResult(initialStatus));

            bool firstReady = PlanReadinessNotificationTracker.Update(plan, CreateReady());
            bool repeatedReady = PlanReadinessNotificationTracker.Update(plan, CreateReady());

            Assert.That(firstReady, Is.True);
            Assert.That(repeatedReady, Is.False);
            Assert.That(plan.LastReadinessNotificationStateWasReady, Is.True);
        }

        [Test]
        public void ReadyToNotReadyRearmsLaterReadyAnnouncement()
        {
            XenogermPlan plan = CreatePlan();
            PlanReadinessNotificationTracker.Update(plan, CreateReady());

            bool notReady = PlanReadinessNotificationTracker.Update(plan, CreateNotReady());
            bool readyAgain = PlanReadinessNotificationTracker.Update(plan, CreateReady());

            Assert.That(notReady, Is.False);
            Assert.That(readyAgain, Is.True);
        }

        [Test]
        public void UnavailableResultDoesNotEstablishOrChangeBaseline()
        {
            XenogermPlan plan = CreatePlan();

            bool firstUnavailable = PlanReadinessNotificationTracker.Update(plan, CreateUnavailable());

            Assert.That(firstUnavailable, Is.False);
            Assert.That(plan.HasReadinessNotificationBaseline, Is.False);

            PlanReadinessNotificationTracker.Update(plan, CreateNotReady());
            bool laterUnavailable = PlanReadinessNotificationTracker.Update(plan, CreateUnavailable());

            Assert.That(laterUnavailable, Is.False);
            Assert.That(plan.HasReadinessNotificationBaseline, Is.True);
            Assert.That(plan.LastReadinessNotificationStateWasReady, Is.False);
        }

        [Test]
        public void ReadyUnavailableReadyDoesNotRepeatAnnouncement()
        {
            XenogermPlan plan = CreatePlan();
            PlanReadinessNotificationTracker.Update(plan, CreateNotReady());
            Assert.That(PlanReadinessNotificationTracker.Update(plan, CreateReady()), Is.True);

            PlanReadinessNotificationTracker.Update(plan, CreateUnavailable());
            bool readyAfterUnavailable = PlanReadinessNotificationTracker.Update(plan, CreateReady());

            Assert.That(readyAfterUnavailable, Is.False);
        }

        [Test]
        public void NotReadyUnavailableReadyStillAnnounces()
        {
            XenogermPlan plan = CreatePlan();
            PlanReadinessNotificationTracker.Update(plan, CreateNotReady());
            PlanReadinessNotificationTracker.Update(plan, CreateUnavailable());

            bool shouldAnnounce = PlanReadinessNotificationTracker.Update(plan, CreateReady());

            Assert.That(shouldAnnounce, Is.True);
        }

        [Test]
        public void DisabledNotificationsSuppressDeliveryButStillUpdateBaseline()
        {
            XenogermPlan plan = CreatePlan(readinessNotificationsEnabled: false);
            PlanReadinessNotificationTracker.Update(plan, CreateNotReady());

            bool shouldAnnounce = PlanReadinessNotificationTracker.Update(plan, CreateReady());

            Assert.That(shouldAnnounce, Is.False);
            Assert.That(plan.LastReadinessNotificationStateWasReady, Is.True);
        }

        [Test]
        public void ReenablingAlreadyReadyPlanDoesNotCreateRetroactiveAnnouncement()
        {
            XenogermPlan plan = CreatePlan(readinessNotificationsEnabled: false);
            PlanReadinessNotificationTracker.Update(plan, CreateNotReady());
            PlanReadinessNotificationTracker.Update(plan, CreateReady());
            plan.ChangeReadinessNotificationsEnabled(true);

            bool shouldAnnounce = PlanReadinessNotificationTracker.Update(plan, CreateReady());

            Assert.That(shouldAnnounce, Is.False);
        }

        private static XenogermPlan CreatePlan(bool readinessNotificationsEnabled = true)
        {
            return new XenogermPlan(
                "Plan",
                Array.Empty<GeneDef>(),
                PlanReadinessMode.Coverage,
                readinessNotificationsEnabled);
        }

        private static PlanReadinessResult CreateResult(PlanReadinessStatus status)
        {
            switch (status)
            {
                case PlanReadinessStatus.Ready:
                    return CreateReady();

                case PlanReadinessStatus.NotReady:
                    return CreateNotReady();

                case PlanReadinessStatus.EmptyTarget:
                    return PlanReadinessResult.CreateEmptyTarget();

                case PlanReadinessStatus.Degraded:
                    return PlanReadinessResult.CreateDegraded(Array.Empty<GeneDef>(), Array.Empty<GeneDef>());

                case PlanReadinessStatus.Unavailable:
                    return CreateUnavailable();

                default:
                    throw new ArgumentOutOfRangeException(nameof(status), status, "Unsupported readiness status.");
            }
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

        private static PlanReadinessResult CreateUnavailable()
        {
            return PlanReadinessResult.CreateUnavailable(PlanReadinessUnavailableReason.NoActiveMap);
        }
    }
}