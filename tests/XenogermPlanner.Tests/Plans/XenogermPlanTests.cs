using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using Verse;
using XenogermPlanner.Plans;

namespace XenogermPlanner.Tests.Plans
{
    [TestFixture]
    public sealed class XenogermPlanTests
    {
        [Test]
        public void Constructor_AllowsEmptyDesiredGeneSet()
        {
            var plan = new XenogermPlan("Empty", Array.Empty<GeneDef>(), PlanReadinessMode.Coverage);

            Assert.That(plan.Id, Is.Not.Null.And.Not.Empty);
            Assert.That(plan.Name, Is.EqualTo("Empty"));
            Assert.That(plan.DesiredGenes, Is.Empty);
            Assert.That(plan.UnresolvedDesiredGeneDefNames, Is.Empty);
            Assert.That(plan.IsDegraded, Is.False);
            Assert.That(plan.ReadinessMode, Is.EqualTo(PlanReadinessMode.Coverage));
        }

        [Test]
        public void Constructor_TrimsOuterWhitespaceFromName()
        {
            var plan = new XenogermPlan("  Plan  ", Array.Empty<GeneDef>(), PlanReadinessMode.Coverage);

            Assert.That(plan.Name, Is.EqualTo("Plan"));
        }

        [TestCase(null)]
        [TestCase("")]
        [TestCase("   ")]
        public void Constructor_RejectsInvalidName(string name)
        {
            XenogermPlan createdPlan = null;

            Assert.That(
                (Action)(() =>
                {
                    createdPlan = new XenogermPlan(name, Array.Empty<GeneDef>(), PlanReadinessMode.Coverage);
                }),
                Throws.InstanceOf<ArgumentException>());

            Assert.That(createdPlan, Is.Null);
        }

        [Test]
        public void Rename_TrimsNameAndPreservesStableId()
        {
            var plan = new XenogermPlan("Original", Array.Empty<GeneDef>(), PlanReadinessMode.Coverage);
            string originalId = plan.Id;

            plan.Rename("  Renamed  ");

            Assert.That(plan.Id, Is.EqualTo(originalId));
            Assert.That(plan.Name, Is.EqualTo("Renamed"));
        }

        [TestCase(null)]
        [TestCase("")]
        [TestCase("   ")]
        public void Rename_InvalidName_DoesNotMutatePlan(string name)
        {
            var plan = new XenogermPlan("Original", Array.Empty<GeneDef>(), PlanReadinessMode.Coverage);
            string originalId = plan.Id;

            Assert.That((Action)(() => plan.Rename(name)), Throws.InstanceOf<ArgumentException>());

            Assert.That(plan.Id, Is.EqualTo(originalId));
            Assert.That(plan.Name, Is.EqualTo("Original"));
        }

        [Test]
        public void Constructor_NormalizesDuplicateDesiredGenes()
        {
            GeneDef geneA = PlanTestData.CreateGene("GeneA");
            GeneDef geneB = PlanTestData.CreateGene("GeneB");

            var plan = new XenogermPlan("Plan", new[] { geneA, geneA, geneB }, PlanReadinessMode.ExactPayload);

            Assert.That(plan.DesiredGenes.Select(gene => gene.defName), Is.EquivalentTo(new[] { "GeneA", "GeneB" }));
            Assert.That(plan.DesiredGenes.Count, Is.EqualTo(2));
            Assert.That(plan.ReadinessMode, Is.EqualTo(PlanReadinessMode.ExactPayload));
        }

        [Test]
        public void Constructor_PreservesDistinctConflictingGenes()
        {
            GeneDef first = PlanTestData.CreateGene("First");
            GeneDef second = PlanTestData.CreateGene("Second");
            first.exclusionTags = new List<string> { "Conflict" };
            second.exclusionTags = new List<string> { "Conflict" };

            var plan = new XenogermPlan("Plan", new[] { first, second }, PlanReadinessMode.Coverage);

            Assert.That(plan.DesiredGenes, Is.EquivalentTo(new[] { first, second }));
        }

        [Test]
        public void Constructor_CopiesMutableDesiredGeneSelection()
        {
            GeneDef geneA = PlanTestData.CreateGene("GeneA");
            GeneDef geneB = PlanTestData.CreateGene("GeneB");
            var selection = new HashSet<GeneDef>
            {
                geneA
            };

            var plan = new XenogermPlan("Plan", selection, PlanReadinessMode.Coverage);

            selection.Clear();
            selection.Add(geneB);

            Assert.That(selection.Select(gene => gene.defName), Is.EquivalentTo(new[] { "GeneB" }));
            Assert.That(plan.DesiredGenes.Select(gene => gene.defName), Is.EquivalentTo(new[] { "GeneA" }));
        }

        [Test]
        public void Constructor_CreatesDistinctIdsForNewPlans()
        {
            var firstPlan = new XenogermPlan("Plan", Array.Empty<GeneDef>(), PlanReadinessMode.Coverage);
            var secondPlan = new XenogermPlan("Plan", Array.Empty<GeneDef>(), PlanReadinessMode.Coverage);

            Assert.That(firstPlan.Id, Is.Not.EqualTo(secondPlan.Id));
            Assert.That(firstPlan.Name, Is.EqualTo(secondPlan.Name));
        }

        [Test]
        public void CreateDuplicate_CopiesPopulatedPlanWithNewStableId()
        {
            GeneDef geneA = PlanTestData.CreateGene("GeneA");
            GeneDef geneB = PlanTestData.CreateGene("GeneB");

            var source = new XenogermPlan("Plan", new[] { geneA, geneB }, PlanReadinessMode.ExactPayload);

            XenogermPlan duplicate = source.CreateDuplicate();

            Assert.That(duplicate.Id, Is.Not.EqualTo(source.Id));
            Assert.That(duplicate.Name, Is.EqualTo(source.Name));
            Assert.That(duplicate.DesiredGenes, Is.EquivalentTo(source.DesiredGenes));
            Assert.That(duplicate.UnresolvedDesiredGeneDefNames, Is.Empty);
            Assert.That(duplicate.IsDegraded, Is.False);
            Assert.That(duplicate.ReadinessMode, Is.EqualTo(source.ReadinessMode));
        }

        [Test]
        public void CreateDuplicate_CopiesEmptyPlan()
        {
            var source = new XenogermPlan("Empty", Array.Empty<GeneDef>(), PlanReadinessMode.Coverage);

            XenogermPlan duplicate = source.CreateDuplicate();

            Assert.That(duplicate.Id, Is.Not.EqualTo(source.Id));
            Assert.That(duplicate.Name, Is.EqualTo("Empty"));
            Assert.That(duplicate.DesiredGenes, Is.Empty);
            Assert.That(duplicate.UnresolvedDesiredGeneDefNames, Is.Empty);
            Assert.That(duplicate.IsDegraded, Is.False);
            Assert.That(duplicate.ReadinessMode, Is.EqualTo(PlanReadinessMode.Coverage));
        }

        [Test]
        public void CreateDuplicate_PreservesDegradedRequirements()
        {
            GeneDef geneA = PlanTestData.CreateGene("GeneA");

            var source = new XenogermPlan(
                "stable-id",
                "Degraded",
                new[] { geneA },
                new[] { "MissingGeneA", "MissingGeneB" },
                PlanReadinessMode.ExactPayload);

            XenogermPlan duplicate = source.CreateDuplicate();

            Assert.That(duplicate.Id, Is.Not.EqualTo(source.Id));
            Assert.That(duplicate.Name, Is.EqualTo(source.Name));
            Assert.That(duplicate.DesiredGenes, Is.EquivalentTo(source.DesiredGenes));
            Assert.That(duplicate.UnresolvedDesiredGeneDefNames, Is.EquivalentTo(source.UnresolvedDesiredGeneDefNames));
            Assert.That(duplicate.IsDegraded, Is.True);
            Assert.That(duplicate.ReadinessMode, Is.EqualTo(source.ReadinessMode));
        }

        [Test]
        public void CreateDuplicate_CreatesIndependentMutablePlan()
        {
            GeneDef geneA = PlanTestData.CreateGene("GeneA");
            GeneDef geneB = PlanTestData.CreateGene("GeneB");
            GeneDef geneC = PlanTestData.CreateGene("GeneC");
            GeneDef geneD = PlanTestData.CreateGene("GeneD");

            var source = new XenogermPlan("Plan", new[] { geneA }, PlanReadinessMode.Coverage);

            XenogermPlan duplicate = source.CreateDuplicate();

            source.Rename("Source renamed");
            source.ReplaceDesiredGenes(new[] { geneB });
            source.ChangeReadinessMode(PlanReadinessMode.ExactPayload);

            Assert.That(duplicate.Name, Is.EqualTo("Plan"));
            Assert.That(duplicate.DesiredGenes, Is.EquivalentTo(new[] { geneA }));
            Assert.That(duplicate.ReadinessMode, Is.EqualTo(PlanReadinessMode.Coverage));

            duplicate.Rename("Duplicate renamed");
            duplicate.ReplaceDesiredGenes(new[] { geneC });
            duplicate.ChangeReadinessMode(PlanReadinessMode.ExactPayload);

            source.Rename("Source final");
            source.ReplaceDesiredGenes(new[] { geneD });
            source.ChangeReadinessMode(PlanReadinessMode.Coverage);

            Assert.That(source.Name, Is.EqualTo("Source final"));
            Assert.That(source.DesiredGenes, Is.EquivalentTo(new[] { geneD }));
            Assert.That(source.ReadinessMode, Is.EqualTo(PlanReadinessMode.Coverage));
            Assert.That(duplicate.Name, Is.EqualTo("Duplicate renamed"));
            Assert.That(duplicate.DesiredGenes, Is.EquivalentTo(new[] { geneC }));
            Assert.That(duplicate.ReadinessMode, Is.EqualTo(PlanReadinessMode.ExactPayload));
        }

        [Test]
        public void PlanMutations_PreserveStableId()
        {
            GeneDef geneA = PlanTestData.CreateGene("GeneA");
            GeneDef geneB = PlanTestData.CreateGene("GeneB");

            var plan = new XenogermPlan("Original", new[] { geneA }, PlanReadinessMode.Coverage);
            string originalId = plan.Id;

            plan.Rename("Renamed");
            plan.ReplaceDesiredGenes(new[] { geneB });
            plan.ChangeReadinessMode(PlanReadinessMode.ExactPayload);

            Assert.That(plan.Id, Is.EqualTo(originalId));
            Assert.That(plan.Name, Is.EqualTo("Renamed"));
            Assert.That(plan.DesiredGenes.Select(gene => gene.defName), Is.EquivalentTo(new[] { "GeneB" }));
            Assert.That(plan.ReadinessMode, Is.EqualTo(PlanReadinessMode.ExactPayload));
        }

        [Test]
        public void Rehydration_NormalizesUnresolvedNamesAndPrefersResolvedGene()
        {
            GeneDef geneA = PlanTestData.CreateGene("GeneA");

            var plan = new XenogermPlan(
                "stable-id",
                "Plan",
                new[] { geneA },
                new[] { "GeneA", "MissingGene", "MissingGene" },
                PlanReadinessMode.Coverage);

            Assert.That(plan.DesiredGenes.Select(gene => gene.defName), Is.EquivalentTo(new[] { "GeneA" }));
            Assert.That(plan.UnresolvedDesiredGeneDefNames, Is.EquivalentTo(new[] { "MissingGene" }));
            Assert.That(plan.IsDegraded, Is.True);
        }

        [Test]
        public void ReplaceDesiredGenes_ClearsUnresolvedRequirements()
        {
            GeneDef geneA = PlanTestData.CreateGene("GeneA");
            GeneDef geneB = PlanTestData.CreateGene("GeneB");

            var plan = new XenogermPlan(
                "stable-id",
                "Plan",
                new[] { geneA },
                new[] { "MissingGene" },
                PlanReadinessMode.Coverage);

            plan.ReplaceDesiredGenes(new[] { geneB });

            Assert.That(plan.DesiredGenes.Select(gene => gene.defName), Is.EquivalentTo(new[] { "GeneB" }));
            Assert.That(plan.UnresolvedDesiredGeneDefNames, Is.Empty);
            Assert.That(plan.IsDegraded, Is.False);
        }

        [Test]
        public void ReplaceDesiredGenes_PreservesDistinctConflictingGenes()
        {
            GeneDef first = PlanTestData.CreateGene("First");
            GeneDef second = PlanTestData.CreateGene("Second");
            first.exclusionTags = new List<string> { "Conflict" };
            second.exclusionTags = new List<string> { "Conflict" };
            var plan = new XenogermPlan("Plan", Array.Empty<GeneDef>(), PlanReadinessMode.Coverage);

            plan.ReplaceDesiredGenes(new[] { first, second, first });

            Assert.That(plan.DesiredGenes, Is.EquivalentTo(new[] { first, second }));
        }

        [Test]
        public void AddDesiredGene_DoesNotRemoveExistingConflictingGene()
        {
            GeneDef first = PlanTestData.CreateGene("First");
            GeneDef second = PlanTestData.CreateGene("Second");
            first.exclusionTags = new List<string> { "Conflict" };
            second.exclusionTags = new List<string> { "Conflict" };
            var plan = new XenogermPlan("Plan", new[] { first }, PlanReadinessMode.Coverage);

            bool added = plan.AddDesiredGene(second);

            Assert.That(added, Is.True);
            Assert.That(plan.DesiredGenes, Is.EquivalentTo(new[] { first, second }));
        }

        [Test]
        public void AddDesiredGene_ResolvesMatchingUnresolvedRequirementOnly()
        {
            GeneDef geneA = PlanTestData.CreateGene("GeneA");
            GeneDef missingGene = PlanTestData.CreateGene("MissingGene");

            var plan = new XenogermPlan(
                "stable-id",
                "Plan",
                Array.Empty<GeneDef>(),
                new[] { "MissingGene", "OtherMissingGene" },
                PlanReadinessMode.Coverage);

            plan.AddDesiredGene(geneA);
            plan.AddDesiredGene(missingGene);

            Assert.That(
                plan.DesiredGenes.Select(gene => gene.defName),
                Is.EquivalentTo(new[] { "GeneA", "MissingGene" }));
            Assert.That(plan.UnresolvedDesiredGeneDefNames, Is.EquivalentTo(new[] { "OtherMissingGene" }));
            Assert.That(plan.IsDegraded, Is.True);
        }


        [Test]
        public void Constructor_EnablesReadinessNotificationsByDefaultWithoutBaseline()
        {
            var plan = new XenogermPlan("Plan", Array.Empty<GeneDef>(), PlanReadinessMode.Coverage);

            Assert.That(plan.ReadinessNotificationsEnabled, Is.True);
            Assert.That(plan.HasReadinessNotificationBaseline, Is.False);
            Assert.That(plan.LastReadinessNotificationStateWasReady, Is.False);
        }

        [Test]
        public void Constructor_AllowsReadinessNotificationsToBeDisabled()
        {
            var plan = new XenogermPlan(
                "Plan",
                Array.Empty<GeneDef>(),
                PlanReadinessMode.Coverage,
                readinessNotificationsEnabled: false);

            Assert.That(plan.ReadinessNotificationsEnabled, Is.False);
            Assert.That(plan.HasReadinessNotificationBaseline, Is.False);
        }

        [Test]
        public void ChangeReadinessNotificationsEnabled_PreservesPlanIdentityAndTarget()
        {
            GeneDef gene = PlanTestData.CreateGene("GeneA");
            var plan = new XenogermPlan("Plan", new[] { gene }, PlanReadinessMode.ExactPayload);
            string id = plan.Id;

            plan.ChangeReadinessNotificationsEnabled(false);

            Assert.That(plan.Id, Is.EqualTo(id));
            Assert.That(plan.ReadinessNotificationsEnabled, Is.False);
            Assert.That(plan.DesiredGenes, Is.EquivalentTo(new[] { gene }));
            Assert.That(plan.ReadinessMode, Is.EqualTo(PlanReadinessMode.ExactPayload));
        }

        [Test]
        public void CreateDuplicate_CopiesNotificationSettingButStartsWithoutTransitionBaseline()
        {
            var source = new XenogermPlan(
                "Plan",
                Array.Empty<GeneDef>(),
                PlanReadinessMode.Coverage,
                readinessNotificationsEnabled: false);
            source.UpdateReadinessNotificationState(true);

            XenogermPlan duplicate = source.CreateDuplicate();

            Assert.That(duplicate.Id, Is.Not.EqualTo(source.Id));
            Assert.That(duplicate.ReadinessNotificationsEnabled, Is.False);
            Assert.That(duplicate.HasReadinessNotificationBaseline, Is.False);
            Assert.That(duplicate.LastReadinessNotificationStateWasReady, Is.False);
            Assert.That(source.HasReadinessNotificationBaseline, Is.True);
            Assert.That(source.LastReadinessNotificationStateWasReady, Is.True);
        }

        [Test]
        public void DuplicateAndSourceNotificationSettingsRemainIndependent()
        {
            var source = new XenogermPlan("Plan", Array.Empty<GeneDef>(), PlanReadinessMode.Coverage);
            XenogermPlan duplicate = source.CreateDuplicate();

            source.ChangeReadinessNotificationsEnabled(false);

            Assert.That(source.ReadinessNotificationsEnabled, Is.False);
            Assert.That(duplicate.ReadinessNotificationsEnabled, Is.True);

            duplicate.ChangeReadinessNotificationsEnabled(false);
            source.ChangeReadinessNotificationsEnabled(true);

            Assert.That(source.ReadinessNotificationsEnabled, Is.True);
            Assert.That(duplicate.ReadinessNotificationsEnabled, Is.False);
        }
    }
}