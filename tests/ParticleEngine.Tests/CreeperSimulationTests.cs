using System;
using ParticleEngine.Core;
using ParticleEngine.Core.Settings;
using Xunit;

namespace ParticleEngine.Tests
{
    public class CreeperSimulationTests
    {
        private static readonly double[] Origin = { 0, 0, 0 };
        private static double[] Min(double v) => new[] { v, v, v };

        private static SpawnSettings Points(double[] pts, double[] min, double[] max)
            => new SpawnSettings(SpawnMode.Points, min, max, 1, 0, pts);

        private static SpawnSettings Box(double[] min, double[] max, int location, int count)
            => new SpawnSettings(SpawnMode.Box, min, max, location, count, Array.Empty<double>());

        // Vector velocity so motion is deterministic (no random direction).
        private static MoveSettings Vel(double vx, double vy, double vz, double maxSpeed = 1000, double vm = 1)
            => new MoveSettings(true, 0, vx, vy, vz, maxSpeed, 0.5, vm);

        private static InitSettings Init(SpawnSettings sp, int dim, int boundary)
            => new InitSettings(sp, dim, boundary);

        [Fact]
        public void BoxSpawnProducesRequestedCount()
        {
            var sim = new CreeperSimulation();
            sim.Initialize(Init(Box(Min(0), Min(10), 1, 50), 1, 2), Vel(1, 0, 0), null);
            Assert.Equal(50, sim.Count);
        }

        [Fact]
        public void PointSpawnProducesOnePerPoint()
        {
            var sim = new CreeperSimulation();
            var pts = new double[] { 0, 0, 0, 1, 1, 1, 2, 2, 2 };
            sim.Initialize(Init(Points(pts, Min(-100), Min(100)), 1, 2), Vel(0, 0, 0), null);
            Assert.Equal(3, sim.Count);
        }

        [Fact]
        public void StraightLineMotion_DoNothingBoundary()
        {
            var sim = new CreeperSimulation();
            sim.Initialize(Init(Points(Origin, Min(-1000), Min(1000)), 1, 2), Vel(2, 0, 0, vm: 1), null);
            sim.Step(); sim.Step(); sim.Step();
            Assert.Equal(6.0, sim.State.Px[0], 1e-9); // 3 steps * v2 * vm1
            Assert.Equal(3, sim.Frame);
        }

        [Fact]
        public void VelocityMultiplierScalesDisplacement()
        {
            var sim = new CreeperSimulation();
            sim.Initialize(Init(Points(Origin, Min(-1000), Min(1000)), 1, 2), Vel(1, 0, 0, vm: 2.5), null);
            sim.Step();
            Assert.Equal(2.5, sim.State.Px[0], 1e-9);
        }

        [Fact]
        public void MaxSpeedClampsVelocity()
        {
            var sim = new CreeperSimulation();
            sim.Initialize(Init(Points(Origin, Min(-1000), Min(1000)), 1, 2), Vel(10, 0, 0, maxSpeed: 3), null);
            sim.Step();
            Assert.Equal(3.0, sim.State.Px[0], 1e-9); // 10 clamped to 3
        }

        [Fact]
        public void TwoDHoldsZAtSpawn()
        {
            var sim = new CreeperSimulation();
            var pts = new double[] { 0, 0, 5 };
            sim.Initialize(Init(Points(pts, Min(-1000), Min(1000)), 0 /*2D*/, 2), Vel(1, 1, 9), null);
            sim.Step(); sim.Step();
            Assert.Equal(5.0, sim.State.Pz[0], 1e-12); // z never moves in 2D
        }

        [Fact]
        public void BounceReflectsAtFace()
        {
            var sim = new CreeperSimulation();
            // start at x=9 in box [0,10], move +2 -> 11 -> reflect to 9 with reversed velocity
            sim.Initialize(Init(Points(new double[] { 9, 5, 5 }, Min(0), Min(10)), 1, 0 /*bounce*/), Vel(2, 0, 0), null);
            sim.Step();
            Assert.Equal(9.0, sim.State.Px[0], 1e-9);
            Assert.Equal(-2.0, sim.State.Vx[0], 1e-9); // velocity reversed
        }

        [Fact]
        public void BounceKeepsEveryParticleInsideBoxForLargeSteps()
        {
            var sim = new CreeperSimulation();
            // big velocity that crosses several box widths each step
            sim.Initialize(Init(Box(Min(0), Min(10), 1, 200), 1, 0), Vel(37, -23, 51, maxSpeed: 1000), null);
            for (int s = 0; s < 20; s++) sim.Step();
            for (int i = 0; i < sim.Count; i++)
            {
                Assert.InRange(sim.State.Px[i], 0.0, 10.0);
                Assert.InRange(sim.State.Py[i], 0.0, 10.0);
                Assert.InRange(sim.State.Pz[i], 0.0, 10.0);
            }
        }

        [Fact]
        public void RespawnPlacesParticleBackInsideBox()
        {
            var sim = new CreeperSimulation();
            sim.Initialize(Init(Points(new double[] { 5, 5, 5 }, Min(0), Min(10)), 1, 1 /*respawn*/), Vel(100, 0, 0), null);
            sim.Step(); // 5 -> 105, outside -> respawn inside
            Assert.InRange(sim.State.Px[0], 0.0, 10.0);
            Assert.InRange(sim.State.Py[0], 0.0, 10.0);
        }

        [Fact]
        public void DoNothingLetsParticleLeave()
        {
            var sim = new CreeperSimulation();
            sim.Initialize(Init(Points(new double[] { 5, 5, 5 }, Min(0), Min(10)), 1, 2 /*do nothing*/), Vel(100, 0, 0), null);
            sim.Step();
            Assert.Equal(105.0, sim.State.Px[0], 1e-9);
        }

        [Fact]
        public void DeterministicForSameSeed()
        {
            var a = new CreeperSimulation();
            var b = new CreeperSimulation();
            var init = Init(Box(Min(0), Min(10), 1, 100), 1, 0);
            var move = new MoveSettings(false, 3, 0, 0, 0, 5, 0.5, 1); // scalar speed -> random dirs
            a.Initialize(init, move, null, seed: 123);
            b.Initialize(init, move, null, seed: 123);
            for (int s = 0; s < 100; s++) { a.Step(); b.Step(); }
            for (int i = 0; i < a.Count; i++)
            {
                Assert.Equal(a.State.Px[i], b.State.Px[i], 1e-12);
                Assert.Equal(a.State.Py[i], b.State.Py[i], 1e-12);
            }
        }

        [Fact]
        public void TrailsStoredWhenEnabled_AndResetReturnsToFrameZero()
        {
            var sim = new CreeperSimulation();
            var trail = new TrailDataSettings(trail: true, trailStep: 1, maxTrailSize: 100);
            sim.Initialize(Init(Points(Origin, Min(-1000), Min(1000)), 1, 2), Vel(1, 0, 0), trail);
            Assert.True(sim.TrailsStored);
            Assert.Equal(1, sim.Trails.Length(0)); // initial point at frame 0
            sim.Step(); sim.Step();
            Assert.Equal(3, sim.Trails.Length(0)); // + one per step
            sim.Reset();
            Assert.Equal(0, sim.Frame);
            Assert.Equal(1, sim.Trails.Length(0));
            Assert.Equal(0.0, sim.State.Px[0], 1e-12);
        }

        [Fact]
        public void RespawnClearsTrailSoNoLineAcrossDomain()
        {
            var sim = new CreeperSimulation();
            var trail = new TrailDataSettings(true, 1, 100);
            sim.Initialize(Init(Points(new double[] { 5, 5, 5 }, Min(0), Min(10)), 1, 1 /*respawn*/), Vel(100, 0, 0), trail);
            sim.Step(); // respawn clears the ring, then the new head point is pushed
            Assert.Equal(1, sim.Trails.Length(0));
        }
    }
}
