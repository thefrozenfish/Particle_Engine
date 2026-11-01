using System;
using ParticleEngine.Core;
using ParticleEngine.Core.Settings;
using Xunit;

namespace ParticleEngine.Tests
{
    /// <summary>
    /// Spec 9.5 acceptance: steady-state Core allocation per <see cref="CreeperSimulation.Step"/> is zero
    /// (±1 KB). Measured with <c>GC.GetAllocatedBytesForCurrentThread</c> after warm-up, so grid/trail
    /// buffers have already grown to their steady size. A bounce boundary keeps the cloud bounded so the
    /// grid does not keep growing. This is the structural proof that survives the probe pass (the probes
    /// change the maths inside the loop, not whether the loop allocates).
    /// </summary>
    public class ZeroAllocationTests
    {
        private const long TolerancePerStep = 1024; // spec 9.5: zero ± 1 KB

        private static readonly double[] Min3 = { -500, -500, -500 };
        private static readonly double[] Max3 = { 500, 500, 500 };

        private static CreeperSimulation Build(int count, TrailDataSettings? trail, params BehaviorSpec[] stack)
        {
            var spawn = new SpawnSettings(SpawnMode.Box, Min3, Max3, 1, count, Array.Empty<double>());
            var init = new InitSettings(spawn, 1 /*3D*/, 0 /*bounce: stays bounded*/);
            var move = new MoveSettings(false, 3, 0, 0, 0, 5, 0.5, 1);
            var sim = new CreeperSimulation();
            sim.Initialize(init, move, trail, 0x9E3779B97F4A7C15UL, new BehaviorData(stack));
            return sim;
        }

        private static double[] RandomPoints(int n, ulong seq)
        {
            var rng = new Pcg32(0xDEADBEEFUL, seq);
            var a = new double[n * 3];
            for (int i = 0; i < n * 3; i++) a[i] = -500 + 1000 * (rng.NextUInt() / (double)uint.MaxValue);
            return a;
        }

        // Average allocated bytes per step over k measured steps, after `warm` warm-up steps.
        private static long PerStep(CreeperSimulation sim, int warm, int k)
        {
            for (int i = 0; i < warm; i++) sim.Step();
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();
            long before = GC.GetAllocatedBytesForCurrentThread();
            for (int i = 0; i < k; i++) sim.Step();
            long after = GC.GetAllocatedBytesForCurrentThread();
            return (after - before) / k;
        }

        [Fact]
        public void Flocking_StepDoesNotAllocate()
        {
            var sim = Build(1000, null, new FlockingSpec(false, 180, 50, 0.24, 0.09, 0.045));
            long perStep = PerStep(sim, 60, 200);
            Assert.True(perStep <= TolerancePerStep, $"flocking allocated {perStep} B/step");
        }

        [Fact]
        public void AttractionRepulsion_StepDoesNotAllocate()
        {
            var sim = Build(2000, null,
                new ForceSpec(false, RandomPoints(10, 1), new[] { 200.0 }, 0.97, 0.06),
                new ForceSpec(true, RandomPoints(10, 2), new[] { 200.0 }, 1.0, 1.5));
            long perStep = PerStep(sim, 40, 200);
            Assert.True(perStep <= TolerancePerStep, $"forces allocated {perStep} B/step");
        }

        [Fact]
        public void Stigmergy_StepDoesNotAllocate()
        {
            // Trail cap 20 at step 1: trails fill within warm-up, then the trail grid is stable.
            var sim = Build(500, new TrailDataSettings(true, 1, 20), new StigmergySpec(60, 1.5, 80, 0, 5));
            long perStep = PerStep(sim, 40, 200);
            Assert.True(perStep <= TolerancePerStep, $"stigmergy allocated {perStep} B/step");
        }

        [Fact]
        public void PathTracking_StepDoesNotAllocate()
        {
            var paths = new double[5][];
            for (int p = 0; p < 5; p++)
            {
                var pl = new double[50 * 3];
                for (int k = 0; k < 50; k++) { pl[k * 3] = -500 + k * 20; pl[k * 3 + 1] = p * 10; pl[k * 3 + 2] = 0; }
                paths[p] = pl;
            }
            var sim = Build(500, null, new PathTrackingSpec(paths, 500, 50, 15));
            long perStep = PerStep(sim, 40, 200);
            Assert.True(perStep <= TolerancePerStep, $"path tracking allocated {perStep} B/step");
        }
    }
}
