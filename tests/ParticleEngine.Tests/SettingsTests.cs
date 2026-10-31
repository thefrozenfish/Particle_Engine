using ParticleEngine.Core.Settings;
using Xunit;

namespace ParticleEngine.Tests
{
    // The settings objects are immutable value carriers (spec 4.2). M1 only checks they hold their
    // values and describe themselves; engines populate and use them from M2.
    public class SettingsTests
    {
        [Fact]
        public void FlockingSpec_HoldsValuesAndDescribesItself()
        {
            var f = new FlockingSpec(connect: true, viewAngle: 180, searchRadius: 70,
                alignValue: 0.24, separationValue: 0.09, cohesionValue: 0.045);

            Assert.True(f.Connect);
            Assert.Equal(180, f.ViewAngle);
            Assert.Equal(0.045, f.CohesionValue);
            Assert.Equal("Flocking Behavior", f.SettingsTypeName);
        }

        [Fact]
        public void ForceSpec_NamesItselfByDirection()
        {
            var attract = new ForceSpec(repel: false, targets: new double[] { 0, 0, 0 }, thresholds: new double[] { 10 }, value: 0.97, max: 0.06);
            var repel = new ForceSpec(repel: true, targets: new double[] { 0, 0, 0 }, thresholds: new double[] { 10 }, value: 1, max: 1.5);

            Assert.Equal("Attraction Force", attract.SettingsTypeName);
            Assert.Equal("Repulsion Force", repel.SettingsTypeName);
        }

        [Fact]
        public void BehaviorData_ReportsStackCount()
        {
            var data = new BehaviorData(new BehaviorSpec[]
            {
                new FlockingSpec(false, 180, 70, 0.24, 0.09, 0.045),
                new BundlingSpec(100, 0.5, true, 40, 2),
            });

            Assert.Equal(2, data.Stack.Count);
            Assert.Contains("2", data.ToString());
        }

        [Fact]
        public void VisualSettings_DescribesDisplayMode()
        {
            Assert.Equal("Visual Settings (Geometry)", new VisualSettings(null, 1).ToString());
            Assert.Equal("Visual Settings (Graphic)", new VisualSettings(null, 0).ToString());
        }
    }
}
