using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using RimWorld;
using Verse;
using XenogermPlanner.Assemblers;

namespace XenogermPlanner.Tests.Assemblers
{
    [TestFixture]
    public sealed class PlanAssemblerScopeScannerTests
    {
        [Test]
        public void GetSelectableAssemblers_NullMapReturnsEmptyList()
        {
            IReadOnlyList<Building_GeneAssembler> assemblers =
                PlanAssemblerScopeScanner.GetSelectableAssemblers((Map)null);

            Assert.That(assemblers, Is.Empty);
        }

        [Test]
        public void GetSelectableAssemblers_IncludesMultipleGeneAssemblers()
        {
            PlanAssemblerScopeTestData.AssemblerFixture first = PlanAssemblerScopeTestData.CreateAssembler();
            PlanAssemblerScopeTestData.AssemblerFixture second = PlanAssemblerScopeTestData.CreateAssembler();

            IReadOnlyList<Building_GeneAssembler> assemblers =
                PlanAssemblerScopeTestData.GetSelectableAssemblers(first.Assembler, second.Assembler);

            Assert.That(assemblers.Count, Is.EqualTo(2));
            Assert.That(assemblers[0], Is.SameAs(first.Assembler));
            Assert.That(assemblers[1], Is.SameAs(second.Assembler));
        }

        [Test]
        public void GetSelectableAssemblers_ExcludesOrdinaryBuildings()
        {
            PlanAssemblerScopeTestData.AssemblerFixture assembler = PlanAssemblerScopeTestData.CreateAssembler();

            IReadOnlyList<Building_GeneAssembler> assemblers = PlanAssemblerScopeTestData.GetSelectableAssemblers(
                PlanAssemblerScopeTestData.CreateOrdinaryBuilding(),
                assembler.Assembler);

            Assert.That(assemblers.Count, Is.EqualTo(1));
            Assert.That(assemblers[0], Is.SameAs(assembler.Assembler));
        }

        [Test]
        public void GetVisibleGenepacks_IncludesPacksFromAllConnectedContainerFacilities()
        {
            Genepack firstPack = PlanAssemblerScopeTestData.CreateGenepack();
            Genepack secondPack = PlanAssemblerScopeTestData.CreateGenepack();

            PlanAssemblerScopeTestData.AssemblerFixture assembler = PlanAssemblerScopeTestData.CreateAssembler(
                PlanAssemblerScopeTestData.CreateFacility(firstPack),
                PlanAssemblerScopeTestData.CreateFacility(secondPack));

            IReadOnlyList<Genepack> genepacks = PlanAssemblerScopeTestData.Scan(assembler);

            Assert.That(genepacks.Count, Is.EqualTo(2));
            Assert.That(genepacks.Any(genepack => ReferenceEquals(genepack, firstPack)), Is.True);
            Assert.That(genepacks.Any(genepack => ReferenceEquals(genepack, secondPack)), Is.True);
        }

        [Test]
        public void GetVisibleGenepacks_IgnoresConnectedFacilityWithoutContainer()
        {
            Genepack pack = PlanAssemblerScopeTestData.CreateGenepack();

            PlanAssemblerScopeTestData.AssemblerFixture assembler =
                PlanAssemblerScopeTestData.CreateAssembler(
                    PlanAssemblerScopeTestData.CreateFacilityWithoutContainer(pack));

            IReadOnlyList<Genepack> genepacks = PlanAssemblerScopeTestData.Scan(assembler);

            Assert.That(genepacks, Is.Empty);
        }

        [Test]
        public void GetVisibleGenepacks_ExcludesDisconnectedContainerFacility()
        {
            Genepack connectedPack = PlanAssemblerScopeTestData.CreateGenepack();

            PlanAssemblerScopeTestData.FacilityFixture connectedFacility =
                PlanAssemblerScopeTestData.CreateFacility(connectedPack);

            PlanAssemblerScopeTestData.AssemblerFixture assembler =
                PlanAssemblerScopeTestData.CreateAssembler(connectedFacility);

            IReadOnlyList<Genepack> genepacks = PlanAssemblerScopeTestData.Scan(assembler);

            Assert.That(genepacks.Count, Is.EqualTo(1));
            Assert.That(genepacks[0], Is.SameAs(connectedPack));
        }

        [Test]
        public void GetVisibleGenepacks_IncludesUnpoweredConnectedContainerFacility()
        {
            Genepack pack = PlanAssemblerScopeTestData.CreateGenepack();

            PlanAssemblerScopeTestData.FacilityFixture facility = PlanAssemblerScopeTestData.CreateFacility(pack);

            facility.PowerOn = false;

            PlanAssemblerScopeTestData.AssemblerFixture
                assembler = PlanAssemblerScopeTestData.CreateAssembler(facility);

            IReadOnlyList<Genepack> genepacks = PlanAssemblerScopeTestData.Scan(assembler);

            Assert.That(genepacks.Count, Is.EqualTo(1));
            Assert.That(genepacks[0], Is.SameAs(pack));
        }

        [Test]
        public void GetVisibleGenepacks_DeduplicatesRepeatedPhysicalReference()
        {
            Genepack pack = PlanAssemblerScopeTestData.CreateGenepack();

            PlanAssemblerScopeTestData.FacilityFixture facility = PlanAssemblerScopeTestData.CreateFacility(pack);

            PlanAssemblerScopeTestData.AssemblerFixture assembler =
                PlanAssemblerScopeTestData.CreateAssembler(facility, facility);

            IReadOnlyList<Genepack> genepacks = PlanAssemblerScopeTestData.Scan(assembler);

            Assert.That(genepacks.Count, Is.EqualTo(1));
            Assert.That(genepacks[0], Is.SameAs(pack));
        }

        [Test]
        public void GetVisibleGenepacks_RetainsDistinctPhysicalReferences()
        {
            Genepack firstPack = PlanAssemblerScopeTestData.CreateGenepack();
            Genepack secondPack = PlanAssemblerScopeTestData.CreateGenepack();

            PlanAssemblerScopeTestData.AssemblerFixture assembler =
                PlanAssemblerScopeTestData.CreateAssembler(
                    PlanAssemblerScopeTestData.CreateFacility(firstPack, secondPack));

            IReadOnlyList<Genepack> genepacks = PlanAssemblerScopeTestData.Scan(assembler);

            Assert.That(genepacks.Count, Is.EqualTo(2));
            Assert.That(genepacks.Any(genepack => ReferenceEquals(genepack, firstPack)), Is.True);
            Assert.That(genepacks.Any(genepack => ReferenceEquals(genepack, secondPack)), Is.True);
        }

        [Test]
        public void GetVisibleGenepacks_AllowsOverlappingAssemblerFacilityScopes()
        {
            Genepack pack = PlanAssemblerScopeTestData.CreateGenepack();

            PlanAssemblerScopeTestData.FacilityFixture sharedFacility = PlanAssemblerScopeTestData.CreateFacility(pack);

            PlanAssemblerScopeTestData.AssemblerFixture firstAssembler =
                PlanAssemblerScopeTestData.CreateAssembler(sharedFacility);

            PlanAssemblerScopeTestData.AssemblerFixture secondAssembler =
                PlanAssemblerScopeTestData.CreateAssembler(sharedFacility);

            IReadOnlyList<Genepack> firstScope = PlanAssemblerScopeTestData.Scan(firstAssembler);
            IReadOnlyList<Genepack> secondScope = PlanAssemblerScopeTestData.Scan(secondAssembler);

            Assert.That(firstScope.Count, Is.EqualTo(1));
            Assert.That(firstScope[0], Is.SameAs(pack));
            Assert.That(secondScope.Count, Is.EqualTo(1));
            Assert.That(secondScope[0], Is.SameAs(pack));
        }


        [Test]
        public void ScanSnapshot_PreservesExactPhysicalPackReference()
        {
            Genepack pack = PlanAssemblerScopeTestData.CreateGenepack();

            PlanAssemblerScopeTestData.AssemblerFixture assembler =
                PlanAssemblerScopeTestData.CreateAssembler(PlanAssemblerScopeTestData.CreateFacility(pack));

            PlanAssemblerScopeSnapshot snapshot = PlanAssemblerScopeTestData.ScanSnapshot(assembler);

            Assert.That(snapshot.Sources.Count, Is.EqualTo(1));
            Assert.That(snapshot.Sources[0].Genepack, Is.SameAs(pack));
            Assert.That(snapshot.VisibleGenepacks[0], Is.SameAs(pack));
        }

        [Test]
        public void ScanSnapshot_LinksPackToContainingConnectedFacility()
        {
            Genepack pack = PlanAssemblerScopeTestData.CreateGenepack();
            PlanAssemblerScopeTestData.FacilityFixture facility = PlanAssemblerScopeTestData.CreateFacility(pack);
            PlanAssemblerScopeTestData.AssemblerFixture
                assembler = PlanAssemblerScopeTestData.CreateAssembler(facility);

            PlanAssemblerScopeSnapshot snapshot = PlanAssemblerScopeTestData.ScanSnapshot(assembler);

            Assert.That(snapshot.Sources.Count, Is.EqualTo(1));
            Assert.That(snapshot.Sources[0].Facility, Is.SameAs(facility.Facility));
        }

        [Test]
        public void ScanSnapshot_PreservesPoweredFacilityState()
        {
            Genepack pack = PlanAssemblerScopeTestData.CreateGenepack();
            PlanAssemblerScopeTestData.FacilityFixture facility = PlanAssemblerScopeTestData.CreateFacility(pack);
            facility.PowerOn = true;

            PlanAssemblerScopeSnapshot snapshot =
                PlanAssemblerScopeTestData.ScanSnapshot(PlanAssemblerScopeTestData.CreateAssembler(facility));

            Assert.That(snapshot.Sources[0].IsFacilityPowered, Is.True);
        }

        [Test]
        public void ScanSnapshot_PreservesUnpoweredFacilityStateWithoutExcludingPack()
        {
            Genepack pack = PlanAssemblerScopeTestData.CreateGenepack();
            PlanAssemblerScopeTestData.FacilityFixture facility = PlanAssemblerScopeTestData.CreateFacility(pack);
            facility.PowerOn = false;

            PlanAssemblerScopeSnapshot snapshot =
                PlanAssemblerScopeTestData.ScanSnapshot(PlanAssemblerScopeTestData.CreateAssembler(facility));

            Assert.That(snapshot.Sources.Count, Is.EqualTo(1));
            Assert.That(snapshot.Sources[0].Genepack, Is.SameAs(pack));
            Assert.That(snapshot.Sources[0].IsFacilityPowered, Is.False);
        }

        [Test]
        public void GetVisibleGenepacks_EmptyConnectedContainerReturnsEmptyList()
        {
            PlanAssemblerScopeTestData.AssemblerFixture assembler =
                PlanAssemblerScopeTestData.CreateAssembler(PlanAssemblerScopeTestData.CreateFacility());

            IReadOnlyList<Genepack> genepacks = PlanAssemblerScopeTestData.Scan(assembler);

            Assert.That(genepacks, Is.Empty);
        }
    }
}