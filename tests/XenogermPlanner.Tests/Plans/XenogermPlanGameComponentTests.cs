using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using Verse;
using XenogermPlanner.Plans;

namespace XenogermPlanner.Tests.Plans
{
    [TestFixture]
    public sealed class XenogermPlanGameComponentTests
    {
        [Test]
        public void RemovePlan_RemovesMatchingStableId()
        {
            var component = new XenogermPlanGameComponent(null);
            var firstPlan = new XenogermPlan("First", Array.Empty<GeneDef>(), PlanReadinessMode.Coverage);
            var secondPlan = new XenogermPlan("Second", Array.Empty<GeneDef>(), PlanReadinessMode.ExactPayload);

            component.AddPlan(firstPlan);
            component.AddPlan(secondPlan);
            bool removed = component.RemovePlan(firstPlan.Id);

            Assert.That(removed, Is.True);
            Assert.That(component.Plans, Has.Count.EqualTo(1));
            Assert.That(component.Plans[0], Is.SameAs(secondPlan));
            Assert.That(component.RemovePlan(firstPlan.Id), Is.False);
        }

        [Test]
        public void AddPlan_RejectsDuplicateStableId()
        {
            var component = new XenogermPlanGameComponent(null);
            var firstPlan = new XenogermPlan(
                "stable-id",
                "First",
                Array.Empty<GeneDef>(),
                Array.Empty<string>(),
                PlanReadinessMode.Coverage);
            var duplicatePlan = new XenogermPlan(
                "stable-id",
                "Second",
                Array.Empty<GeneDef>(),
                Array.Empty<string>(),
                PlanReadinessMode.ExactPayload);

            component.AddPlan(firstPlan);

            void AddDuplicatePlan() => component.AddPlan(duplicatePlan);

            Assert.Throws<InvalidOperationException>((Action)AddDuplicatePlan);
            Assert.That(component.Plans, Has.Count.EqualTo(1));
            Assert.That(component.Plans[0], Is.SameAs(firstPlan));
        }

        [Test]
        public void AddPlan_RejectsDuplicateDisplayNameIgnoringCaseAndWhitespace()
        {
            var component = new XenogermPlanGameComponent(null);
            var firstPlan = new XenogermPlan("Existing", Array.Empty<GeneDef>(), PlanReadinessMode.Coverage);
            var duplicateNamePlan = new XenogermPlan(
                "  existing  ",
                Array.Empty<GeneDef>(),
                PlanReadinessMode.ExactPayload);

            component.AddPlan(firstPlan);

            Assert.That(
                (Action)(() => component.AddPlan(duplicateNamePlan)),
                Throws.TypeOf<InvalidOperationException>());

            Assert.That(component.Plans, Has.Count.EqualTo(1));
            Assert.That(component.Plans[0], Is.SameAs(firstPlan));
            Assert.That(duplicateNamePlan.Name, Is.EqualTo("existing"));
        }

        [Test]
        public void AddPlanWithAllocatedName_UsesFirstAvailableSuffix()
        {
            var component = new XenogermPlanGameComponent(null);
            component.AddPlan(new XenogermPlan("Plan", Array.Empty<GeneDef>(), PlanReadinessMode.Coverage));
            component.AddPlan(new XenogermPlan("Plan 2", Array.Empty<GeneDef>(), PlanReadinessMode.Coverage));

            var automaticPlan = new XenogermPlan("Plan", Array.Empty<GeneDef>(), PlanReadinessMode.ExactPayload);

            component.AddPlanWithAllocatedName(automaticPlan);

            Assert.That(automaticPlan.Name, Is.EqualTo("Plan 3"));
            Assert.That(component.Plans, Has.Count.EqualTo(3));
            Assert.That(component.Plans[2], Is.SameAs(automaticPlan));
        }

        [Test]
        public void AddPlanWithAllocatedName_RepeatedAddsUseSequentialNames()
        {
            var component = new XenogermPlanGameComponent(null);
            var first = new XenogermPlan("Template", Array.Empty<GeneDef>(), PlanReadinessMode.Coverage);
            var second = new XenogermPlan("Template", Array.Empty<GeneDef>(), PlanReadinessMode.Coverage);
            var third = new XenogermPlan("Template", Array.Empty<GeneDef>(), PlanReadinessMode.Coverage);

            component.AddPlanWithAllocatedName(first);
            component.AddPlanWithAllocatedName(second);
            component.AddPlanWithAllocatedName(third);

            Assert.That(
                component.Plans.Select(plan => plan.Name),
                Is.EqualTo(new[] { "Template", "Template 2", "Template 3" }));
        }

        [Test]
        public void TryValidatePlanName_AvailableName_ReturnsTrimmedName()
        {
            var component = new XenogermPlanGameComponent(null);

            bool valid = component.TryValidatePlanName(
                "  New Plan  ",
                null,
                out string normalizedName,
                out XenogermPlanNameValidationFailure failure);

            Assert.That(valid, Is.True);
            Assert.That(normalizedName, Is.EqualTo("New Plan"));
            Assert.That(failure, Is.EqualTo(XenogermPlanNameValidationFailure.None));
        }

        [Test]
        public void TryValidatePlanName_InvalidName_ReturnsInvalidNameFailure()
        {
            var component = new XenogermPlanGameComponent(null);

            bool valid = component.TryValidatePlanName(
                "   ",
                null,
                out string normalizedName,
                out XenogermPlanNameValidationFailure failure);

            Assert.That(valid, Is.False);
            Assert.That(normalizedName, Is.Null);
            Assert.That(failure, Is.EqualTo(XenogermPlanNameValidationFailure.InvalidName));
        }

        [Test]
        public void TryValidatePlanName_OccupiedNameIgnoringCase_ReturnsConflict()
        {
            var component = new XenogermPlanGameComponent(null);
            component.AddPlan(new XenogermPlan("Existing", Array.Empty<GeneDef>(), PlanReadinessMode.Coverage));

            bool valid = component.TryValidatePlanName(
                " existing ",
                null,
                out string normalizedName,
                out XenogermPlanNameValidationFailure failure);

            Assert.That(valid, Is.False);
            Assert.That(normalizedName, Is.EqualTo("existing"));
            Assert.That(failure, Is.EqualTo(XenogermPlanNameValidationFailure.NameConflict));
        }

        [Test]
        public void TryValidatePlanName_ExcludedPlanId_AllowsOwnName()
        {
            var component = new XenogermPlanGameComponent(null);
            var plan = new XenogermPlan("Existing", Array.Empty<GeneDef>(), PlanReadinessMode.Coverage);
            component.AddPlan(plan);

            bool valid = component.TryValidatePlanName(
                " existing ",
                plan.Id,
                out string normalizedName,
                out XenogermPlanNameValidationFailure failure);

            Assert.That(valid, Is.True);
            Assert.That(normalizedName, Is.EqualTo("existing"));
            Assert.That(failure, Is.EqualTo(XenogermPlanNameValidationFailure.None));
        }

        [Test]
        public void TryValidatePlanName_ExcludedPlanId_DoesNotHideConflictWithAnotherPlan()
        {
            var component = new XenogermPlanGameComponent(null);
            var firstPlan = new XenogermPlan("Existing", Array.Empty<GeneDef>(), PlanReadinessMode.Coverage);
            var secondPlan = new XenogermPlan("Other", Array.Empty<GeneDef>(), PlanReadinessMode.ExactPayload);
            component.AddPlan(firstPlan);
            component.AddPlan(secondPlan);

            bool valid = component.TryValidatePlanName(
                " other ",
                firstPlan.Id,
                out string normalizedName,
                out XenogermPlanNameValidationFailure failure);

            Assert.That(valid, Is.False);
            Assert.That(normalizedName, Is.EqualTo("other"));
            Assert.That(failure, Is.EqualTo(XenogermPlanNameValidationFailure.NameConflict));
        }

        [Test]
        public void AllocateUniquePlanName_UsesCurrentCollection()
        {
            var component = new XenogermPlanGameComponent(null);
            component.AddPlan(new XenogermPlan("Plan", Array.Empty<GeneDef>(), PlanReadinessMode.Coverage));
            component.AddPlan(new XenogermPlan("Plan 2", Array.Empty<GeneDef>(), PlanReadinessMode.ExactPayload));

            string allocatedName = component.AllocateUniquePlanName("Plan");

            Assert.That(allocatedName, Is.EqualTo("Plan 3"));
        }

        [Test]
        public void AllocateUniquePlanName_PreservesPreferredCasing()
        {
            var component = new XenogermPlanGameComponent(null);
            component.AddPlan(new XenogermPlan("plan", Array.Empty<GeneDef>(), PlanReadinessMode.Coverage));

            string allocatedName = component.AllocateUniquePlanName("PLAN");

            Assert.That(allocatedName, Is.EqualTo("PLAN 2"));
        }

        [Test]
        public void RestorePlans_RestoresMultipleValidRecordsInOrder()
        {
            GeneDef geneA = PlanTestData.CreateGene("GeneA");
            GeneDef geneB = PlanTestData.CreateGene("GeneB");
            XenogermPlanSaveRecord[] records = new[]
            {
                CreateRecord("plan-a", "First", new[] { "GeneA" }, PlanReadinessMode.Coverage),
                CreateRecord("plan-b", "Second", new[] { "GeneB" }, PlanReadinessMode.ExactPayload)
            };
            var errors = new List<string>();
            var component = new XenogermPlanGameComponent(null);

            component.RestorePlans(records, PlanTestData.CreateResolver(geneA, geneB), errors.Add);

            Assert.That(errors, Is.Empty);
            Assert.That(component.Plans, Has.Count.EqualTo(2));
            Assert.That(component.Plans.Select(plan => plan.Id), Is.EqualTo(new[] { "plan-a", "plan-b" }));
            Assert.That(component.Plans[0].Name, Is.EqualTo("First"));
            Assert.That(component.Plans[0].ReadinessMode, Is.EqualTo(PlanReadinessMode.Coverage));
            Assert.That(component.Plans[1].Name, Is.EqualTo("Second"));
            Assert.That(component.Plans[1].ReadinessMode, Is.EqualTo(PlanReadinessMode.ExactPayload));
        }

        [Test]
        public void RestorePlans_AllocatesUniqueNamesInRecordOrder()
        {
            XenogermPlanSaveRecord[] records =
            {
                CreateRecord("plan-a", "Plan", Array.Empty<string>(), PlanReadinessMode.Coverage),
                CreateRecord("plan-b", "Plan", Array.Empty<string>(), PlanReadinessMode.Coverage),
                CreateRecord("plan-c", "Plan", Array.Empty<string>(), PlanReadinessMode.ExactPayload)
            };
            var errors = new List<string>();
            var component = new XenogermPlanGameComponent(null);

            component.RestorePlans(records, PlanTestData.CreateResolver(), errors.Add);

            Assert.That(errors, Is.Empty);
            Assert.That(component.Plans.Select(plan => plan.Name), Is.EqualTo(new[] { "Plan", "Plan 2", "Plan 3" }));
            Assert.That(component.Plans.Select(plan => plan.Id), Is.EqualTo(new[] { "plan-a", "plan-b", "plan-c" }));
        }

        [Test]
        public void RestorePlans_UsesNormalizedCaseInsensitiveNameKeys()
        {
            XenogermPlanSaveRecord[] records =
            {
                CreateRecord("plan-a", "  Plan  ", Array.Empty<string>(), PlanReadinessMode.Coverage),
                CreateRecord("plan-b", "plan", Array.Empty<string>(), PlanReadinessMode.Coverage),
                CreateRecord("plan-c", "PLAN", Array.Empty<string>(), PlanReadinessMode.Coverage)
            };
            var errors = new List<string>();
            var component = new XenogermPlanGameComponent(null);

            component.RestorePlans(records, PlanTestData.CreateResolver(), errors.Add);

            Assert.That(errors, Is.Empty);
            Assert.That(component.Plans.Select(plan => plan.Name), Is.EqualTo(new[] { "Plan", "plan 2", "PLAN 3" }));
        }

        [Test]
        public void RestorePlans_ContinuesExistingNumericSuffix()
        {
            XenogermPlanSaveRecord[] records =
            {
                CreateRecord("plan-a", "Plan 2", Array.Empty<string>(), PlanReadinessMode.Coverage),
                CreateRecord("plan-b", "Plan 2", Array.Empty<string>(), PlanReadinessMode.ExactPayload)
            };
            var errors = new List<string>();
            var component = new XenogermPlanGameComponent(null);

            component.RestorePlans(records, PlanTestData.CreateResolver(), errors.Add);

            Assert.That(errors, Is.Empty);
            Assert.That(component.Plans.Select(plan => plan.Name), Is.EqualTo(new[] { "Plan 2", "Plan 3" }));
        }

        [Test]
        public void RestorePlans_IsolatesBlankNameRecordAndKeepsFollowingPlans()
        {
            XenogermPlanSaveRecord[] records =
            {
                CreateRecord("plan-a", "First", Array.Empty<string>(), PlanReadinessMode.Coverage),
                CreateRecord("plan-b", "   ", Array.Empty<string>(), PlanReadinessMode.Coverage),
                CreateRecord("plan-c", "Third", Array.Empty<string>(), PlanReadinessMode.ExactPayload)
            };
            var errors = new List<string>();
            var component = new XenogermPlanGameComponent(null);

            component.RestorePlans(records, PlanTestData.CreateResolver(), errors.Add);

            Assert.That(component.Plans.Select(plan => plan.Id), Is.EqualTo(new[] { "plan-a", "plan-c" }));
            Assert.That(errors, Has.Count.EqualTo(1));
            Assert.That(errors[0], Does.Contain("index 1"));
            Assert.That(errors[0], Does.Contain("name"));
        }

        [Test]
        public void RestorePlans_IsolatesInvalidRecordAndKeepsDegradedPlan()
        {
            GeneDef geneA = PlanTestData.CreateGene("GeneA");
            GeneDef geneC = PlanTestData.CreateGene("GeneC");
            XenogermPlanSaveRecord[] records = new[]
            {
                CreateRecord("plan-a", "First", new[] { "GeneA" }, PlanReadinessMode.Coverage),
                new XenogermPlanSaveRecord(
                    string.Empty,
                    "Invalid",
                    Array.Empty<string>(),
                    (int)PlanReadinessMode.Coverage),
                CreateRecord("plan-b", "Degraded", new[] { "MissingGene" }, PlanReadinessMode.Coverage),
                CreateRecord("plan-c", "Third", new[] { "GeneC" }, PlanReadinessMode.ExactPayload)
            };
            var errors = new List<string>();
            var component = new XenogermPlanGameComponent(null);

            component.RestorePlans(records, PlanTestData.CreateResolver(geneA, geneC), errors.Add);

            Assert.That(component.Plans.Select(plan => plan.Id), Is.EqualTo(new[] { "plan-a", "plan-b", "plan-c" }));
            Assert.That(component.Plans[1].IsDegraded, Is.True);
            Assert.That(component.Plans[1].UnresolvedDesiredGeneDefNames, Is.EquivalentTo(new[] { "MissingGene" }));
            Assert.That(errors, Has.Count.EqualTo(1));
            Assert.That(errors[0], Does.Contain("index 1"));
        }

        [Test]
        public void RestorePlans_IsolatesNullRecord()
        {
            XenogermPlanSaveRecord[] records =
            {
                CreateRecord("plan-a", "First", Array.Empty<string>(), PlanReadinessMode.Coverage),
                null,
                CreateRecord("plan-b", "Second", Array.Empty<string>(), PlanReadinessMode.ExactPayload)
            };
            var errors = new List<string>();
            var component = new XenogermPlanGameComponent(null);

            component.RestorePlans(records, PlanTestData.CreateResolver(), errors.Add);

            Assert.That(component.Plans.Select(plan => plan.Id), Is.EqualTo(new[] { "plan-a", "plan-b" }));
            Assert.That(errors, Has.Count.EqualTo(1));
            Assert.That(errors[0], Does.Contain("index 1"));
            Assert.That(errors[0], Does.Contain("record is null"));
        }

        [Test]
        public void RestorePlans_KeepsFirstRecordWhenPersistedIdsAreDuplicated()
        {
            XenogermPlanSaveRecord[] records = new[]
            {
                CreateRecord("plan-a", "First", Array.Empty<string>(), PlanReadinessMode.Coverage),
                CreateRecord("plan-a", "Duplicate", Array.Empty<string>(), PlanReadinessMode.ExactPayload),
                CreateRecord("plan-b", "Unrelated", Array.Empty<string>(), PlanReadinessMode.ExactPayload)
            };
            var errors = new List<string>();
            var component = new XenogermPlanGameComponent(null);

            component.RestorePlans(records, PlanTestData.CreateResolver(), errors.Add);

            Assert.That(component.Plans, Has.Count.EqualTo(2));
            Assert.That(component.Plans.Select(plan => plan.Id), Is.EqualTo(new[] { "plan-a", "plan-b" }));
            Assert.That(component.Plans[0].Name, Is.EqualTo("First"));
            Assert.That(errors, Has.Count.EqualTo(1));
            Assert.That(errors[0], Does.Contain("duplicate plan ID 'plan-a'"));
        }

        [Test]
        public void RestorePlans_TreatsNullRecordsAsEmptyCollection()
        {
            var component = new XenogermPlanGameComponent(null);
            component.AddPlan(new XenogermPlan("Existing", Array.Empty<GeneDef>(), PlanReadinessMode.Coverage));
            var errors = new List<string>();

            component.RestorePlans(null, PlanTestData.CreateResolver(), errors.Add);

            Assert.That(component.Plans, Is.Empty);
            Assert.That(errors, Is.Empty);
        }

        [Test]
        public void RestorePlans_TreatsEmptyRecordsAsEmptyCollection()
        {
            var component = new XenogermPlanGameComponent(null);
            component.AddPlan(new XenogermPlan("Existing", Array.Empty<GeneDef>(), PlanReadinessMode.Coverage));
            var errors = new List<string>();

            component.RestorePlans(Array.Empty<XenogermPlanSaveRecord>(), PlanTestData.CreateResolver(), errors.Add);

            Assert.That(component.Plans, Is.Empty);
            Assert.That(errors, Is.Empty);
        }

        [Test]
        public void RestorePlans_PreservesIndependentNotificationSettingsAndBaselines()
        {
            XenogermPlanSaveRecord[] records =
            {
                new XenogermPlanSaveRecord(
                    "plan-a",
                    "First",
                    Array.Empty<string>(),
                    (int)PlanReadinessMode.Coverage,
                    readinessNotificationsEnabled: false,
                    hasReadinessNotificationBaseline: true,
                    lastReadinessNotificationStateWasReady: true),
                new XenogermPlanSaveRecord(
                    "plan-b",
                    "Second",
                    Array.Empty<string>(),
                    (int)PlanReadinessMode.ExactPayload,
                    readinessNotificationsEnabled: true,
                    hasReadinessNotificationBaseline: true,
                    lastReadinessNotificationStateWasReady: false)
            };
            var errors = new List<string>();
            var component = new XenogermPlanGameComponent(null);

            component.RestorePlans(records, PlanTestData.CreateResolver(), errors.Add);

            Assert.That(errors, Is.Empty);
            Assert.That(component.Plans, Has.Count.EqualTo(2));
            Assert.That(component.Plans[0].ReadinessNotificationsEnabled, Is.False);
            Assert.That(component.Plans[0].HasReadinessNotificationBaseline, Is.True);
            Assert.That(component.Plans[0].LastReadinessNotificationStateWasReady, Is.True);
            Assert.That(component.Plans[1].ReadinessNotificationsEnabled, Is.True);
            Assert.That(component.Plans[1].HasReadinessNotificationBaseline, Is.True);
            Assert.That(component.Plans[1].LastReadinessNotificationStateWasReady, Is.False);
        }

        private static XenogermPlanSaveRecord CreateRecord(
            string id,
            string name,
            IEnumerable<string> desiredGeneDefNames,
            PlanReadinessMode readinessMode)
        {
            return new XenogermPlanSaveRecord(id, name, desiredGeneDefNames, (int)readinessMode);
        }
    }
}