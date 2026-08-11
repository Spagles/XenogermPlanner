using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using RimWorld;
using Verse;
using XenogermPlanner.Plans;

namespace XenogermPlanner.Tests.Plans
{
    [TestFixture]
    public sealed class CustomXenogermPlanImporterTests
    {
        [Test]
        public void TryReadSource_FlattensGeneSetsAndNormalizesDuplicateGenes()
        {
            GeneDef geneA = PlanTestData.CreateGene("GeneA");
            GeneDef geneB = PlanTestData.CreateGene("GeneB");
            GeneDef geneC = PlanTestData.CreateGene("GeneC");
            CustomXenogerm source = PlanTestData.CreateCustomXenogerm(
                "Template",
                PlanTestData.CreateGeneSet(geneA),
                PlanTestData.CreateGeneSet(geneA, geneB),
                PlanTestData.CreateGeneSet(geneC));

            bool imported = CustomXenogermPlanImporter.TryReadSource(
                source,
                out CustomXenogermPlanImportData importData,
                out CustomXenogermPlanImportFailure failure);

            Assert.That(imported, Is.True);
            Assert.That(failure, Is.EqualTo(CustomXenogermPlanImportFailure.None));
            Assert.That(importData, Is.Not.Null);
            Assert.That(importData.Name, Is.EqualTo("Template"));
            Assert.That(importData.DesiredGenes.Count, Is.EqualTo(3));
            Assert.That(
                importData.DesiredGenes.Select(gene => gene.defName),
                Is.EquivalentTo(new[] { "GeneA", "GeneB", "GeneC" }));
        }

        [Test]
        public void TryReadSource_TrimsSourceName()
        {
            CustomXenogerm source = PlanTestData.CreateCustomXenogerm(
                "  Template  ",
                PlanTestData.CreateGeneSet(PlanTestData.CreateGene("GeneA")));

            bool imported = CustomXenogermPlanImporter.TryReadSource(
                source,
                out CustomXenogermPlanImportData importData,
                out CustomXenogermPlanImportFailure failure);

            Assert.That(imported, Is.True);
            Assert.That(failure, Is.EqualTo(CustomXenogermPlanImportFailure.None));
            Assert.That(importData.Name, Is.EqualTo("Template"));
        }

        [Test]
        public void ImportedPlans_WithSameSourceName_AreAllocatedUniquelyByDestination()
        {
            GeneDef gene = PlanTestData.CreateGene("GeneA");
            CustomXenogerm firstSource = PlanTestData.CreateCustomXenogerm(
                "Template",
                PlanTestData.CreateGeneSet(gene));
            CustomXenogerm secondSource = PlanTestData.CreateCustomXenogerm(
                "Template",
                PlanTestData.CreateGeneSet(gene));

            CustomXenogermPlanImporter.TryReadSource(firstSource, out CustomXenogermPlanImportData firstImport, out _);
            CustomXenogermPlanImporter.TryReadSource(
                secondSource,
                out CustomXenogermPlanImportData secondImport,
                out _);

            var component = new XenogermPlanGameComponent(null);
            var firstPlan = new XenogermPlan(firstImport.Name, firstImport.DesiredGenes, PlanReadinessMode.Coverage);
            var secondPlan = new XenogermPlan(secondImport.Name, secondImport.DesiredGenes, PlanReadinessMode.Coverage);

            component.AddPlanWithAllocatedName(firstPlan);
            component.AddPlanWithAllocatedName(secondPlan);

            Assert.That(component.Plans.Select(plan => plan.Name), Is.EqualTo(new[] { "Template", "Template 2" }));
        }

        [Test]
        public void TryReadSource_PreservesConflictingGenesAcrossGeneSets()
        {
            GeneDef first = PlanTestData.CreateGene("First");
            GeneDef second = PlanTestData.CreateGene("Second");
            first.exclusionTags = new List<string> { "Conflict" };
            second.exclusionTags = new List<string> { "Conflict" };
            CustomXenogerm source = PlanTestData.CreateCustomXenogerm(
                "Template",
                PlanTestData.CreateGeneSet(first, second),
                PlanTestData.CreateGeneSet(first));

            bool imported = CustomXenogermPlanImporter.TryReadSource(
                source,
                out CustomXenogermPlanImportData importData,
                out CustomXenogermPlanImportFailure failure);

            Assert.That(imported, Is.True);
            Assert.That(failure, Is.EqualTo(CustomXenogermPlanImportFailure.None));
            Assert.That(importData.DesiredGenes, Is.EquivalentTo(new[] { first, second }));
        }

        [Test]
        public void TryReadSource_CopiesVisibleGeneComposition()
        {
            GeneDef geneA = PlanTestData.CreateGene("GeneA");
            GeneDef geneB = PlanTestData.CreateGene("GeneB");
            GeneSet sourceGeneSet = PlanTestData.CreateGeneSet(geneA);
            CustomXenogerm source = PlanTestData.CreateCustomXenogerm("Template", sourceGeneSet);

            bool imported = CustomXenogermPlanImporter.TryReadSource(
                source,
                out CustomXenogermPlanImportData importData,
                out CustomXenogermPlanImportFailure failure);

            sourceGeneSet.AddGene(geneB);

            Assert.That(imported, Is.True);
            Assert.That(failure, Is.EqualTo(CustomXenogermPlanImportFailure.None));
            Assert.That(importData.DesiredGenes.Select(gene => gene.defName), Is.EquivalentTo(new[] { "GeneA" }));
            Assert.That(
                sourceGeneSet.GenesListForReading.Select(gene => gene.defName),
                Is.EquivalentTo(new[] { "GeneA", "GeneB" }));
        }

        [TestCase(PlanReadinessMode.Coverage)]
        [TestCase(PlanReadinessMode.ExactPayload)]
        public void ImportedPlan_PreservesImportDataAndSelectedReadinessMode(PlanReadinessMode readinessMode)
        {
            GeneDef geneA = PlanTestData.CreateGene("GeneA");
            GeneDef geneB = PlanTestData.CreateGene("GeneB");
            CustomXenogerm source = PlanTestData.CreateCustomXenogerm(
                "Template",
                PlanTestData.CreateGeneSet(geneA, geneB));

            bool imported = CustomXenogermPlanImporter.TryReadSource(
                source,
                out CustomXenogermPlanImportData importData,
                out CustomXenogermPlanImportFailure failure);

            var plan = new XenogermPlan(importData.Name, importData.DesiredGenes, readinessMode);

            Assert.That(imported, Is.True);
            Assert.That(failure, Is.EqualTo(CustomXenogermPlanImportFailure.None));
            Assert.That(plan.Id, Is.Not.Null.And.Not.Empty);
            Assert.That(plan.Name, Is.EqualTo("Template"));
            Assert.That(plan.DesiredGenes.Select(gene => gene.defName), Is.EquivalentTo(new[] { "GeneA", "GeneB" }));
            Assert.That(plan.IsDegraded, Is.False);
            Assert.That(plan.ReadinessMode, Is.EqualTo(readinessMode));
        }

        [Test]
        public void ImportedPlan_RemainsIndependentAfterSourceReplacementAndRemoval()
        {
            GeneDef geneA = PlanTestData.CreateGene("GeneA");
            GeneDef geneB = PlanTestData.CreateGene("GeneB");
            GeneDef geneC = PlanTestData.CreateGene("GeneC");
            CustomXenogerm source = PlanTestData.CreateCustomXenogerm(
                "Template",
                PlanTestData.CreateGeneSet(geneA, geneB));
            CustomXenogerm replacement = PlanTestData.CreateCustomXenogerm(
                "Template",
                PlanTestData.CreateGeneSet(geneC));
            var runtimeSources = new List<CustomXenogerm>
            {
                source
            };

            bool imported = CustomXenogermPlanImporter.TryReadSource(
                runtimeSources[0],
                out CustomXenogermPlanImportData importData,
                out CustomXenogermPlanImportFailure failure);

            var plan = new XenogermPlan(importData.Name, importData.DesiredGenes, PlanReadinessMode.Coverage);

            runtimeSources[0] = replacement;

            Assert.That(imported, Is.True);
            Assert.That(failure, Is.EqualTo(CustomXenogermPlanImportFailure.None));
            Assert.That(plan.Name, Is.EqualTo("Template"));
            Assert.That(plan.DesiredGenes.Select(gene => gene.defName), Is.EquivalentTo(new[] { "GeneA", "GeneB" }));

            runtimeSources.Clear();

            Assert.That(plan.Name, Is.EqualTo("Template"));
            Assert.That(plan.DesiredGenes.Select(gene => gene.defName), Is.EquivalentTo(new[] { "GeneA", "GeneB" }));
        }

        [Test]
        public void ImportedPlanEditing_DoesNotMutateSource()
        {
            GeneDef geneA = PlanTestData.CreateGene("GeneA");
            GeneDef geneB = PlanTestData.CreateGene("GeneB");
            GeneDef geneC = PlanTestData.CreateGene("GeneC");
            CustomXenogerm source = PlanTestData.CreateCustomXenogerm(
                "Template",
                PlanTestData.CreateGeneSet(geneA, geneB));

            bool imported = CustomXenogermPlanImporter.TryReadSource(
                source,
                out CustomXenogermPlanImportData importData,
                out CustomXenogermPlanImportFailure failure);

            var plan = new XenogermPlan(importData.Name, importData.DesiredGenes, PlanReadinessMode.Coverage);

            plan.RemoveDesiredGene(geneA);
            plan.AddDesiredGene(geneC);
            plan.Rename("Edited");
            plan.ChangeReadinessMode(PlanReadinessMode.ExactPayload);

            Assert.That(imported, Is.True);
            Assert.That(failure, Is.EqualTo(CustomXenogermPlanImportFailure.None));
            Assert.That(plan.Name, Is.EqualTo("Edited"));
            Assert.That(plan.DesiredGenes.Select(gene => gene.defName), Is.EquivalentTo(new[] { "GeneB", "GeneC" }));
            Assert.That(plan.ReadinessMode, Is.EqualTo(PlanReadinessMode.ExactPayload));

            Assert.That(source.name, Is.EqualTo("Template"));
            Assert.That(GetSourceGeneDefNames(source), Is.EquivalentTo(new[] { "GeneA", "GeneB" }));
        }

        [Test]
        public void TryReadSource_RejectsNullSource()
        {
            AssertImportFailure(null, CustomXenogermPlanImportFailure.SourceUnavailable);
        }

        [TestCase(null)]
        [TestCase("")]
        [TestCase("   ")]
        public void TryReadSource_RejectsInvalidSourceName(string name)
        {
            var source = new CustomXenogerm
            {
                name = name,
                genesets = new List<GeneSet>
                {
                    PlanTestData.CreateGeneSet(PlanTestData.CreateGene("GeneA"))
                }
            };

            AssertImportFailure(source, CustomXenogermPlanImportFailure.InvalidSourceData);
        }

        [Test]
        public void TryReadSource_RejectsMissingGeneSetCollection()
        {
            var source = new CustomXenogerm
            {
                name = "Template",
                genesets = null
            };

            AssertImportFailure(source, CustomXenogermPlanImportFailure.InvalidSourceData);
        }

        [Test]
        public void TryReadSource_RejectsEmptyGeneSetCollection()
        {
            var source = new CustomXenogerm
            {
                name = "Template",
                genesets = new List<GeneSet>()
            };

            AssertImportFailure(source, CustomXenogermPlanImportFailure.EmptySource);
        }

        [Test]
        public void TryReadSource_RejectsNullGeneSet()
        {
            GeneDef geneA = PlanTestData.CreateGene("GeneA");
            GeneDef geneB = PlanTestData.CreateGene("GeneB");
            var source = new CustomXenogerm
            {
                name = "Template",
                genesets = new List<GeneSet>
                {
                    PlanTestData.CreateGeneSet(geneA),
                    null,
                    PlanTestData.CreateGeneSet(geneB)
                }
            };

            AssertImportFailure(source, CustomXenogermPlanImportFailure.InvalidSourceData);
        }

        [Test]
        public void TryReadSource_RejectsEmptyGeneSet()
        {
            CustomXenogerm source = PlanTestData.CreateCustomXenogerm("Template", PlanTestData.CreateGeneSet());

            AssertImportFailure(source, CustomXenogermPlanImportFailure.EmptySource);
        }

        [Test]
        public void TryReadSource_RejectsNullGene()
        {
            GeneDef geneA = PlanTestData.CreateGene("GeneA");
            GeneSet geneSet = PlanTestData.CreateGeneSet(geneA);

            geneSet.GenesListForReading.Add(null);

            CustomXenogerm source = PlanTestData.CreateCustomXenogerm("Template", geneSet);

            AssertImportFailure(source, CustomXenogermPlanImportFailure.InvalidSourceData);
        }

        private static IEnumerable<string> GetSourceGeneDefNames(CustomXenogerm source)
        {
            return source.genesets.SelectMany(geneSet => geneSet.GenesListForReading).Select(gene => gene.defName);
        }

        private static void AssertImportFailure(CustomXenogerm source, CustomXenogermPlanImportFailure expectedFailure)
        {
            bool imported = CustomXenogermPlanImporter.TryReadSource(
                source,
                out CustomXenogermPlanImportData importData,
                out CustomXenogermPlanImportFailure failure);

            Assert.That(imported, Is.False);
            Assert.That(importData, Is.Null);
            Assert.That(failure, Is.EqualTo(expectedFailure));
        }
    }
}