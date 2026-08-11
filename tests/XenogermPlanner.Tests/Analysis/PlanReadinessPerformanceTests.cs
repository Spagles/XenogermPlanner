using System;
using System.Diagnostics;
using System.Linq;
using NUnit.Framework;
using XenogermPlanner.Analysis;

namespace XenogermPlanner.Tests.Analysis
{
    [TestFixture]
    [Category("Performance")]
    [Explicit("Run locally in Release configuration to measure readiness analysis performance.")]
    public sealed class PlanReadinessPerformanceTests
    {
        private const int WarmUpCount = 10;
        private const int SampleCount = 100;

        [Test]
        public void Benchmark_LargeCoverageAnalysis()
        {
            Measure("Large Coverage Ready", PlanReadinessStressScenario.CreateCoverageReady());
        }

        [Test]
        public void Benchmark_LargeExactReadyAnalysis()
        {
            Measure("Large Exact Ready", PlanReadinessStressScenario.CreateExactReady());
        }

        [Test]
        public void Benchmark_LargeExactConflictAnalysis()
        {
            Measure("Large Exact Conflict", PlanReadinessStressScenario.CreateExactConflict());
        }

        private static void Measure(string scenarioName, PlanReadinessStressScenario scenario)
        {
            PlanReadinessResult lastResult = null;

            for (var index = 0; index < WarmUpCount; index++)
                lastResult = Analyze(scenario);

            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();

            var samples = new double[SampleCount];

            for (var index = 0; index < SampleCount; index++)
            {
                long startedAt = Stopwatch.GetTimestamp();

                lastResult = Analyze(scenario);

                long completedAt = Stopwatch.GetTimestamp();

                samples[index] = (completedAt - startedAt) * 1000d / Stopwatch.Frequency;
            }

            double[] sortedSamples = samples.OrderBy(sample => sample).ToArray();

            double median = (sortedSamples[SampleCount / 2 - 1] + sortedSamples[SampleCount / 2]) / 2d;

            int p95Index = (int)Math.Ceiling(SampleCount * 0.95d) - 1;

            double p95 = sortedSamples[p95Index];
            double maximum = sortedSamples[SampleCount - 1];

            TestContext.Progress.WriteLine(scenarioName);
            TestContext.Progress.WriteLine($"Desired genes: {scenario.Plan.DesiredGenes.Count}");
            TestContext.Progress.WriteLine($"Physical genepacks: {scenario.Packs.Length}");
            TestContext.Progress.WriteLine($"Samples: {SampleCount}");
            TestContext.Progress.WriteLine(string.Empty);
            TestContext.Progress.WriteLine($"Median: {median:F3} ms");
            TestContext.Progress.WriteLine($"P95:    {p95:F3} ms");
            TestContext.Progress.WriteLine($"Max:    {maximum:F3} ms");

            AssertResultMatchesScenario(scenario, lastResult);
        }

        private static PlanReadinessResult Analyze(PlanReadinessStressScenario scenario)
        {
            return PlanReadinessTestData.Analyze(scenario.Plan, scenario.Packs);
        }

        private static void AssertResultMatchesScenario(
            PlanReadinessStressScenario scenario,
            PlanReadinessResult result)
        {
            Assert.That(result, Is.Not.Null);
            Assert.That(result.Status, Is.EqualTo(scenario.ExpectedStatus));
            Assert.That(result.CoveredGenes, Has.Count.EqualTo(scenario.ExpectedCoveredGeneCount));
            Assert.That(result.MissingGenes, Has.Count.EqualTo(scenario.ExpectedMissingGeneCount));
            Assert.That(result.HasExactPayloadConflict, Is.EqualTo(scenario.ExpectedExactPayloadConflict));
        }
    }
}