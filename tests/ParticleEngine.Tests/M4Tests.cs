using System;
using ParticleEngine.Core;
using ParticleEngine.Core.Settings;
using Xunit;

namespace ParticleEngine.Tests
{
    public class M4Tests
    {
        private static double[] Min(double v) => new[] { v, v, v };

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

        private static BehaviorData Stack(params BehaviorSpec[] specs) => new BehaviorData(specs);

        // ---- Stigmergy ----

        [Fact]
        public void Stigmergy_CohesionSteersTowardOtherTrails()
        {
            // A at origin, B at +x; each has a spawn trail point. B should steer toward A's trail.
            var sim = Sim(new double[] { 0, 0, 0, 5, 0, 0 },
                Stack(new StigmergySpec(viewAngle: 360, cohesionMag: 1, cohesionRange: 10, separationMag: 0, separationRange: 0)));
            sim.Step();
            Assert.True(sim.State.Vx[1] < 0, "B should steer toward A's trail at the origin");
            Assert.True(sim.State.Vx[0] > 0, "A should steer toward B's trail at +x");
        }

        [Fact]
        public void Stigmergy_SeparationSteersAwayFromOtherTrails()
        {
            var sim = Sim(new double[] { 0, 0, 0, 5, 0, 0 },
                Stack(new StigmergySpec(viewAngle: 360, cohesionMag: 0, cohesionRange: 0, separationMag: 1, separationRange: 10)));
            sim.Step();
            Assert.True(sim.State.Vx[1] > 0, "B should steer away from A's trail at the origin");
        }

        [Fact]
        public void Stigmergy_ForcesTrailStorageEvenWithoutTrailData()
        {
            var sim = Sim(new double[] { 0, 0, 0 },
                Stack(new StigmergySpec(60, 1.5, 80, 0, 5)));
            Assert.True(sim.TrailsStored); // trail storage active because Stigmergy is present (spec 7.8)
            Assert.Equal(1, sim.Trails.Length(0));
        }

        // ---- Multi Path Tracking ----

        [Fact]
        public void PathTracking_SteersTowardTheNearbyPath()
        {
            // Straight path along +x at y=0; agent 5 units off in +y, moving +x.
            var path = new double[] { 0, 0, 0, 100, 0, 0 };
            var sim = Sim(new double[] { 10, 5, 0 },
                Stack(new PathTrackingSpec(new[] { path }, threshold: 50, projectionDistance: 5, radius: 1)));
            sim.State.Vx[0] = 3; // moving along the path's axis
            sim.Step();
            Assert.True(sim.State.Vy[0] < 0, "agent should steer back toward the path (−y)");
        }

        [Fact]
        public void PathTracking_NoSteeringInsideCorridor()
        {
            var path = new double[] { 0, 0, 0, 100, 0, 0 };
            var sim = Sim(new double[] { 10, 0.2, 0 }, // within radius 1 of the path
                Stack(new PathTrackingSpec(new[] { path }, threshold: 50, projectionDistance: 5, radius: 1)));
            sim.State.Vx[0] = 3;
            sim.Step();
            Assert.Equal(0.0, sim.State.Vy[0], 1e-9); // no cross-path steering
        }

        // ---- Mesh Crawl ----

        private sealed class FlatPlane : IMeshSurface
        {
            public bool Closest(double x, double y, double z, double maxDistance,
                out double cx, out double cy, out double cz, out double nx, out double ny, out double nz)
            {
                cx = x; cy = y; cz = 0; nx = 0; ny = 0; nz = 1; // closest point on z=0, upward normal
                return true;
            }
        }

        [Fact]
        public void MeshCrawl_SnapsToSurfaceAndFlattensVelocity()
        {
            var sim = Sim(new double[] { 0, 0, 5 }, // 5 above the z=0 plane
                Stack(new MeshCrawlSpec(new FlatPlane(), meshThreshold: 0.1, meshProjectionDistance: 0, multiplier: 1)));
            sim.State.Vz[0] = -2; // moving down toward the plane
            sim.Step();
            Assert.Equal(0.0, sim.State.Pz[0], 1e-9);  // snapped onto the surface
            Assert.Equal(0.0, sim.State.Vz[0], 1e-9);  // normal (z) velocity removed
        }

        [Fact]
        public void MeshCrawl_StaysOnSurfaceWhileMovingTangentially()
        {
            var sim = Sim(new double[] { 0, 0, 0 },
                Stack(new MeshCrawlSpec(new FlatPlane(), 0.1, 2, 1)));
            sim.State.Vx[0] = 3; // along the surface
            sim.Step();
            Assert.Equal(0.0, sim.State.Pz[0], 1e-9); // remains on the plane
            Assert.True(sim.State.Px[0] > 0, "moves along the surface");
        }
    }
}
