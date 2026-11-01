using ParticleEngine.Core;
using ParticleEngine.Core.Settings;
using Xunit;

namespace ParticleEngine.Tests
{
    public class M6Tests
    {
        // A straight horizontal line at height y, n points evenly from x0 to x1, flat x,y,z.
        private static double[] Line(double y, int n, double x0 = 0, double x1 = 4)
        {
            var arr = new double[n * 3];
            for (int k = 0; k < n; k++)
            {
                double x = x0 + (x1 - x0) * k / (n - 1);
                arr[k * 3] = x; arr[k * 3 + 1] = y; arr[k * 3 + 2] = 0;
            }
            return arr;
        }

        private static BundlingSpec Spec(double threshold, double ratio, int weld)
            => new BundlingSpec(threshold, ratio, rebuild: false, pointCount: 0, weldCount: weld);

        [Fact]
        public void NearbyCurvesConverge()
        {
            var sim = new BundlingSimulation();
            sim.Initialize(new[] { Line(0, 5), Line(2, 5) }, Spec(threshold: 5, ratio: 0.1, weld: 0));
            for (int f = 0; f < 20; f++) sim.Step();

            double[][] o = sim.ToCurves();
            double ay = o[0][2 * 3 + 1]; // middle point of the lower line
            double by = o[1][2 * 3 + 1]; // middle point of the upper line
            Assert.True(ay > 0.05, "lower line moves up toward the other");
            Assert.True(by < 1.95, "upper line moves down toward the other");
            Assert.True(by - ay < 2.0, "the gap between the lines shrinks");
        }

        [Fact]
        public void CurvesBeyondThresholdDoNotMove()
        {
            var sim = new BundlingSimulation();
            sim.Initialize(new[] { Line(0, 5), Line(100, 5) }, Spec(threshold: 5, ratio: 0.1, weld: 0));
            for (int f = 0; f < 10; f++) sim.Step();

            double[][] o = sim.ToCurves();
            Assert.Equal(0.0, o[0][2 * 3 + 1], 1e-9);
            Assert.Equal(100.0, o[1][2 * 3 + 1], 1e-9);
        }

        [Fact]
        public void WeldedEndpointsStayPinned()
        {
            var sim = new BundlingSimulation();
            sim.Initialize(new[] { Line(0, 5), Line(2, 5) }, Spec(threshold: 5, ratio: 0.1, weld: 2));
            for (int f = 0; f < 20; f++) sim.Step();

            double[][] o = sim.ToCurves();
            // First two and last two points of the lower line are welded (unchanged at y = 0).
            Assert.Equal(0.0, o[0][0 * 3 + 1], 1e-9);
            Assert.Equal(0.0, o[0][1 * 3 + 1], 1e-9);
            Assert.Equal(0.0, o[0][3 * 3 + 1], 1e-9);
            Assert.Equal(0.0, o[0][4 * 3 + 1], 1e-9);
            Assert.True(o[0][2 * 3 + 1] > 0.05, "the free middle point still moves");
        }

        [Fact]
        public void BundlingIsDeterministic()
        {
            var a = new BundlingSimulation();
            var b = new BundlingSimulation();
            a.Initialize(new[] { Line(0, 6), Line(1.5, 6), Line(3, 6) }, Spec(4, 0.1, 1));
            b.Initialize(new[] { Line(0, 6), Line(1.5, 6), Line(3, 6) }, Spec(4, 0.1, 1));
            for (int f = 0; f < 30; f++) { a.Step(); b.Step(); }

            double[][] oa = a.ToCurves(), ob = b.ToCurves();
            for (int c = 0; c < oa.Length; c++)
                for (int k = 0; k < oa[c].Length; k++)
                    Assert.Equal(oa[c][k], ob[c][k], 1e-12);
        }

        [Fact]
        public void ToCurvesPreservesCurveAndPointCounts()
        {
            var sim = new BundlingSimulation();
            sim.Initialize(new[] { Line(0, 5), Line(2, 8) }, Spec(5, 0.1, 0));
            double[][] o = sim.ToCurves();
            Assert.Equal(2, o.Length);
            Assert.Equal(5 * 3, o[0].Length);
            Assert.Equal(8 * 3, o[1].Length);
            Assert.Equal(2, sim.CurveCount);
            Assert.Equal(13, sim.PointCount);
        }

        [Fact]
        public void ResetReturnsToInputCurves()
        {
            var spec = Spec(5, 0.1, 0);
            var sim = new BundlingSimulation();
            sim.Initialize(new[] { Line(0, 5), Line(2, 5) }, spec);
            for (int f = 0; f < 10; f++) sim.Step();
            sim.Reset();

            double[][] o = sim.ToCurves();
            Assert.Equal(0, sim.Cycle);
            Assert.Equal(0.0, o[0][2 * 3 + 1], 1e-9);
            Assert.Equal(2.0, o[1][2 * 3 + 1], 1e-9);
        }
    }
}
