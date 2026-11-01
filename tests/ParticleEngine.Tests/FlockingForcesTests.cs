using System;
using ParticleEngine.Core;
using ParticleEngine.Core.Settings;
using Xunit;

namespace ParticleEngine.Tests
{
    public class FlockingForcesTests
    {
        private static double[] Min(double v) => new[] { v, v, v };

        // All agents start at rest (vector velocity 0,0,0); tests set specific velocities afterward.
        private static MoveSettings AtRest(double maxSpeed = 5, double maxForce = 1)
            => new MoveSettings(true, 0, 0, 0, 0, maxSpeed, maxForce, 1);

        private static CreeperSimulation Sim(double[] points, BehaviorData behaviors, int dim = 1)
        {
            var sp = new SpawnSettings(SpawnMode.Points, Min(-1000), Min(1000), 1, 0, points);
            var init = new InitSettings(sp, dim, 2 /*do nothing*/);
            var s = new CreeperSimulation();
            s.Initialize(init, AtRest(), null, 0, behaviors);
            return s;
        }

        private static FlockingSpec Flock(double va, double sr, double av, double sv, double cv)
            => new FlockingSpec(false, va, sr, av, sv, cv);

        private static BehaviorData Stack(params BehaviorSpec[] specs) => new BehaviorData(specs);

        [Fact]
        public void Separation_PushesNeighborsApart()
        {
            var sim = Sim(new double[] { 0, 0, 0, 1, 0, 0 }, Stack(Flock(360, 5, 0, 1, 0)));
            sim.Step();
            Assert.True(sim.State.Vx[0] < 0, "agent 0 should be pushed away from the +x neighbour");
            Assert.True(sim.State.Vx[1] > 0, "agent 1 should be pushed away from the -x neighbour");
        }

        [Fact]
        public void Alignment_SteersTowardNeighborVelocity()
        {
            var sim = Sim(new double[] { 0, 0, 0, 1, 0, 0 }, Stack(Flock(360, 5, 1, 0, 0)));
            sim.State.Vy[1] = 5; // neighbour moving +y
            sim.Step();
            Assert.True(sim.State.Vy[0] > 0, "agent 0 should steer toward the neighbour's +y heading");
        }

        [Fact]
        public void Cohesion_SeeksAverageNeighborPosition()
        {
            var sim = Sim(new double[] { 0, 0, 0, 3, 0, 0 }, Stack(Flock(360, 5, 0, 0, 1)));
            sim.Step();
            Assert.True(sim.State.Vx[0] > 0, "agent 0 should move toward the +x neighbour");
        }

        [Fact]
        public void ViewCone_IgnoresNeighborsBehindWhenNarrow()
        {
            // Agent 0 moves +x; neighbour sits behind it at -x.
            var narrow = Sim(new double[] { 0, 0, 0, -1, 0, 0 }, Stack(Flock(90, 5, 0, 0, 1)));
            narrow.State.Vx[0] = 5;
            narrow.Step();

            var wide = Sim(new double[] { 0, 0, 0, -1, 0, 0 }, Stack(Flock(360, 5, 0, 0, 1)));
            wide.State.Vx[0] = 5;
            wide.Step();

            // Narrow cone can't see the neighbour behind -> full speed forward; wide cone is pulled back.
            Assert.Equal(5.0, narrow.State.Vx[0], 1e-9);
            Assert.True(wide.State.Vx[0] < narrow.State.Vx[0]);
        }

        private static ForceSpec Force(bool repel, double[] targets, double[] thresholds, double value, double max)
            => new ForceSpec(repel, targets, thresholds, value, max);

        [Fact]
        public void Attraction_PullsTowardTargetWithinThreshold()
        {
            var sim = Sim(new double[] { 0, 0, 0 }, Stack(Force(false, new double[] { 3, 0, 0 }, new double[] { 10 }, 1, 5)));
            sim.Step();
            Assert.True(sim.State.Vx[0] > 0, "should be pulled toward the +x target");
        }

        [Fact]
        public void Repulsion_PushesAwayFromTarget()
        {
            var sim = Sim(new double[] { 0, 0, 0 }, Stack(Force(true, new double[] { 3, 0, 0 }, new double[] { 10 }, 1, 5)));
            sim.Step();
            Assert.True(sim.State.Vx[0] < 0, "should be pushed away from the +x target");
        }

        [Fact]
        public void Force_IsZeroOutsideThreshold()
        {
            var sim = Sim(new double[] { 0, 0, 0 }, Stack(Force(false, new double[] { 20, 0, 0 }, new double[] { 10 }, 1, 5)));
            sim.Step();
            Assert.Equal(0.0, sim.State.Vx[0], 1e-12); // target beyond threshold -> no force
        }

        [Fact]
        public void Force_MagnitudeClampedByMax()
        {
            // Very close, huge value: unclamped magnitude would be ~value, but Max caps it.
            var big = Sim(new double[] { 0, 0, 0 }, Stack(Force(false, new double[] { 1, 0, 0 }, new double[] { 10 }, 100, 0.2)));
            big.Step();
            // Acceleration is capped by Max (0.2) then by Max Force (1); either way < the uncapped value.
            Assert.InRange(big.State.Vx[0], 0.0, 0.2 + 1e-9);
        }
    }
}
