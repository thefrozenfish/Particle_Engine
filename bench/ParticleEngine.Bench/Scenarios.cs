using System;
using ParticleEngine.Core;
using ParticleEngine.Core.Settings;

namespace ParticleEngine.Bench
{
    /// <summary>The fixed synthetic scenarios from spec 9.1. Each builds a ready-to-step
    /// <see cref="CreeperSimulation"/> deterministically (own PRNG, no System.Random), with a warm-up
    /// count so the measured steps are steady-state.</summary>
    internal sealed class Scenario
    {
        public Scenario(string name, string target, int warm, Func<CreeperSimulation> build)
        {
            Name = name; Target = target; Warm = warm; Build = build;
        }

        public string Name { get; }
        public string Target { get; }   // the spec 9.1 target, for the report
        public int Warm { get; }        // steps to run before measuring
        public Func<CreeperSimulation> Build { get; }
    }

    internal static class Scenarios
    {
        private static readonly double[] Min3 = { -500, -500, -500 };
        private static readonly double[] Max3 = { 500, 500, 500 };

        private static CreeperSimulation Build(int count, int boundary, MoveSettings move,
            TrailDataSettings? trail, BehaviorData behaviors)
        {
            var spawn = new SpawnSettings(SpawnMode.Box, Min3, Max3, 1 /*random*/, count, Array.Empty<double>());
            var init = new InitSettings(spawn, 1 /*3D*/, boundary);
            var sim = new CreeperSimulation();
            sim.Initialize(init, move, trail, 0x9E3779B97F4A7C15UL, behaviors);
            return sim;
        }

        private static MoveSettings Move(double maxSpeed = 5, double maxForce = 0.5)
            => new MoveSettings(false, 3, 0, 0, 0, maxSpeed, maxForce, 1);

        // Deterministic point cloud in the domain, as flat x,y,z triples.
        private static double[] RandomPoints(int n, ulong seq)
        {
            var rng = new Pcg32(0xDEADBEEFUL, seq);
            var a = new double[n * 3];
            for (int i = 0; i < n; i++)
            {
                a[i * 3] = Lerp(rng, -500, 500);
                a[i * 3 + 1] = Lerp(rng, -500, 500);
                a[i * 3 + 2] = Lerp(rng, -500, 500);
            }
            return a;
        }

        private static double Lerp(Pcg32 rng, double lo, double hi)
            => lo + (hi - lo) * (rng.NextUInt() / (double)uint.MaxValue);

        public static Scenario[] All()
        {
            return new[]
            {
                new Scenario("5 000 agents, Flocking, 3D", "<= 10 ms", 30, () =>
                    Build(5000, 2, Move(), null, new BehaviorData(new BehaviorSpec[]
                    {
                        // SR ~ 5 % of the 1000-wide domain.
                        new FlockingSpec(false, 180, 50, 0.24, 0.09, 0.045),
                    }))),

                new Scenario("1 000 agents, Stigmergy, trail 100, step 6", "<= 15 ms",
                    // Warm past cap (100 points at step 6 -> ~600 frames) so trails are full.
                    630, () =>
                    Build(1000, 2, Move(), new TrailDataSettings(true, 6, 100), new BehaviorData(new BehaviorSpec[]
                    {
                        new StigmergySpec(60, 1.5, 80, 0, 5),
                    }))),

                new Scenario("10 000 agents, Attraction + Repulsion (20 targets)", "<= 5 ms", 30, () =>
                {
                    double[] attract = RandomPoints(20, 1);
                    double[] repel = RandomPoints(20, 2);
                    double[] thr = { 200 };
                    return Build(10000, 2, Move(), null, new BehaviorData(new BehaviorSpec[]
                    {
                        new ForceSpec(false, attract, thr, 0.97, 0.06),
                        new ForceSpec(true, repel, thr, 1.0, 1.5),
                    }));
                }),

                new Scenario("2 000 agents, Multi Path Tracking, 20 paths x 200 seg", "<= 8 ms", 30, () =>
                {
                    var paths = new double[20][];
                    var rng = new Pcg32(0xABCDEFUL, 7);
                    for (int p = 0; p < 20; p++)
                    {
                        // A 200-segment polyline wandering across the domain.
                        var pl = new double[201 * 3];
                        double x = Lerp(rng, -500, 500), y = Lerp(rng, -500, 500), z = Lerp(rng, -500, 500);
                        double dx = Lerp(rng, -5, 5), dy = Lerp(rng, -5, 5), dz = Lerp(rng, -5, 5);
                        for (int k = 0; k <= 200; k++)
                        {
                            pl[k * 3] = x; pl[k * 3 + 1] = y; pl[k * 3 + 2] = z;
                            x += dx; y += dy; z += dz;
                        }
                        paths[p] = pl;
                    }
                    return Build(2000, 2, Move(), null, new BehaviorData(new BehaviorSpec[]
                    {
                        new PathTrackingSpec(paths, 500, 50, 15),
                    }));
                }),
            };
        }
    }
}
