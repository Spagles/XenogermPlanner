using System.Reflection;
using Escarval.RimWorld.UI;
using NUnit.Framework;
using XenogermPlanner.Plans;

namespace XenogermPlanner.Tests.UI.SharedUi
{
    [TestFixture]
    public sealed class SharedUiSourceIntegrationTests
    {
        [Test]
        public void SharedUiTypes_AreCompiledIntoXenogermPlannerAssembly()
        {
            Assembly productionAssembly = typeof(XenogermPlan).Assembly;

            Assert.That(typeof(FixedHeightScrollListLayout).Assembly, Is.SameAs(productionAssembly));
            Assert.That(typeof(VariableHeightScrollListLayout).Assembly, Is.SameAs(productionAssembly));
            Assert.That(typeof(VariableHeightScrollListLayoutCache).Assembly, Is.SameAs(productionAssembly));
            Assert.That(typeof(RimWorldUiWidgets).Assembly, Is.SameAs(productionAssembly));
            Assert.That(typeof(RimWorldUiStyle).Assembly, Is.SameAs(productionAssembly));
            Assert.That(typeof(ImGuiStateScope).Assembly, Is.SameAs(productionAssembly));
            Assert.That(typeof(ReloadableTexture2D).Assembly, Is.SameAs(productionAssembly));
            Assert.That(productionAssembly.GetName().Name, Is.EqualTo("XenogermPlanner"));
        }
    }
}