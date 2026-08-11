using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using NUnit.Framework;
using Verse;
using XenogermPlanner.Plans;
using XenogermPlanner.Templates;

namespace XenogermPlanner.Tests.Templates
{
    [TestFixture]
    [Category("Performance")]
    [Explicit("Run locally in Release configuration to measure bounded template candidate search performance.")]
    public sealed class PlanXenogermTemplateCandidateSearchPerformanceTests
    {
        private const int TargetGeneCount = 16;
        private const int WarmUpCount = 2;
        private const int SampleCount = 10;

        [Test]
        public void Benchmark_PathologicalCoverageCandidateSpace()
        {
            CreateScenario(out XenogermPlan plan, out PlanXenogermTemplateTestData.PackFixture[] packs);
            PlanXenogermTemplateCandidateSearchResult lastResult = null;

            for (var index = 0; index < WarmUpCount; index++)
                lastResult = PlanXenogermTemplateTestData.Search(plan, packs);

            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();

            var samples = new double[SampleCount];

            for (var index = 0; index < SampleCount; index++)
            {
                long startedAt = Stopwatch.GetTimestamp();

                lastResult = PlanXenogermTemplateTestData.Search(plan, packs);

                long completedAt = Stopwatch.GetTimestamp();
                samples[index] = (completedAt - startedAt) * 1000d / Stopwatch.Frequency;
            }

            double[] sortedSamples = samples.OrderBy(sample => sample).ToArray();
            double median = (sortedSamples[SampleCount / 2 - 1] + sortedSamples[SampleCount / 2]) / 2d;
            int p95Index = (int)Math.Ceiling(SampleCount * 0.95d) - 1;
            double p95 = sortedSamples[p95Index];
            double maximum = sortedSamples[SampleCount - 1];

            TestContext.Progress.WriteLine("Pathological template candidate space");
            TestContext.Progress.WriteLine($"Target genes: {plan.DesiredGenes.Count}");
            TestContext.Progress.WriteLine($"Unique compositions: {packs.Length}");
            TestContext.Progress.WriteLine($"Returned candidates: {lastResult?.Candidates.Count ?? 0}");
            TestContext.Progress.WriteLine($"Complete search: {lastResult?.IsComplete ?? false}");
            TestContext.Progress.WriteLine($"Samples: {SampleCount}");
            TestContext.Progress.WriteLine(string.Empty);
            TestContext.Progress.WriteLine($"Median: {median:F3} ms");
            TestContext.Progress.WriteLine($"P95:    {p95:F3} ms");
            TestContext.Progress.WriteLine($"Max:    {maximum:F3} ms");

            Assert.That(lastResult, Is.Not.Null);
            Assert.That(lastResult.HasCandidate, Is.True);
            Assert.That(
                lastResult.Candidates.Count,
                Is.LessThanOrEqualTo(PlanXenogermTemplateCandidateSearchLimits.Default.MaxRetainedCandidates));
            Assert.That(lastResult.IsComplete, Is.False);
            AssertCandidateCoversTarget(lastResult.AutomaticCandidate, plan.DesiredGenes);
        }

        private static void CreateScenario(out XenogermPlan plan, out PlanXenogermTemplateTestData.PackFixture[] packs)
        {
            var targetGenes = new GeneDef[TargetGeneCount];
            var createdPacks = new List<PlanXenogermTemplateTestData.PackFixture>(TargetGeneCount * 2);

            for (var index = 0; index < TargetGeneCount; index++)
            {
                GeneDef target = PlanXenogermTemplateTestData.CreateGene($"Target{index:D2}");
                GeneDef firstExtra = PlanXenogermTemplateTestData.CreateGene($"Extra{index:D2}A");
                GeneDef secondExtra = PlanXenogermTemplateTestData.CreateGene($"Extra{index:D2}B");
                targetGenes[index] = target;
                createdPacks.Add(PlanXenogermTemplateTestData.CreatePack($"pack-{index:D2}-a", target, firstExtra));
                createdPacks.Add(PlanXenogermTemplateTestData.CreatePack($"pack-{index:D2}-b", target, secondExtra));
            }

            plan = PlanXenogermTemplateTestData.CreatePlan(PlanReadinessMode.Coverage, targetGenes);
            packs = createdPacks.ToArray();
        }

        private static void AssertCandidateCoversTarget(
            PlanXenogermTemplateCandidate candidate,
            IEnumerable<GeneDef> targetGenes)
        {
            var unionGenes = new HashSet<GeneDef>(candidate.UnionGenes);
            Assert.That(unionGenes.IsSupersetOf(targetGenes), Is.True);
        }
    }
}