using System;
using System.Collections.Generic;
using NUnit.Framework;
using XenogermPlanner.Api;

namespace XenogermPlanner.Tests.Api
{
    [TestFixture]
    public sealed class GenepackRelevanceRequestTests
    {
        [Test]
        public void Constructor_NullCollection_Throws()
        {
            Assert.Throws<ArgumentNullException>((Action)(() => { _ = new GenepackRelevanceRequest(null); }));
        }

        [Test]
        public void Constructor_CopiesInputAndPreservesOrder()
        {
            var source = new List<string> { "GeneA", "GeneB" };
            var request = new GenepackRelevanceRequest(source);

            source[0] = "Changed";
            source.Add("GeneC");

            Assert.That(request.GeneDefNames, Is.EqualTo(new[] { "GeneA", "GeneB" }));
        }

        [Test]
        public void Constructor_PreservesDuplicatesForBoundaryNormalization()
        {
            var request = new GenepackRelevanceRequest(new[] { "GeneA", "GeneA", "genea" });

            Assert.That(request.GeneDefNames, Is.EqualTo(new[] { "GeneA", "GeneA", "genea" }));
        }

        [Test]
        public void Constructor_PreservesStructurallyInvalidValuesForStatusBasedValidation()
        {
            var request = new GenepackRelevanceRequest(new[] { "GeneA", null, " " });

            Assert.That(request.GeneDefNames, Is.EqualTo(new[] { "GeneA", null, " " }));
        }
    }
}