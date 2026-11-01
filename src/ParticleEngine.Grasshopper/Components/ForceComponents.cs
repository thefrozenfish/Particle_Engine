using System.Collections.Generic;
using Grasshopper.Kernel;
using ParticleEngine.Core.Manifest;
using ParticleEngine.Core.Settings;
using ParticleEngine.Grasshopper.Goo;
using Rhino.Geometry;

namespace ParticleEngine.Grasshopper.Components
{
    /// <summary>Attraction Force — attracts creepers toward target points within a threshold.</summary>
    public sealed class AttractionForceComponent : ParticleComponentBase
    {
        public AttractionForceComponent() : base(ComponentCatalog.ByKey("AttractionForce")) { }

        protected override void SolveInstance(IGH_DataAccess DA) =>
            ForceSolve.Solve(this, DA, repel: false, defaultValue: 0.97, defaultMax: 0.06);
    }

    /// <summary>Repulsion Force — repels creepers away from target points within a threshold.</summary>
    public sealed class RepulsionForceComponent : ParticleComponentBase
    {
        public RepulsionForceComponent() : base(ComponentCatalog.ByKey("RepulsionForce")) { }

        protected override void SolveInstance(IGH_DataAccess DA) =>
            ForceSolve.Solve(this, DA, repel: true, defaultValue: 1.0, defaultMax: 1.5);
    }

    internal static class ForceSolve
    {
        internal static void Solve(ParticleComponentBase comp, IGH_DataAccess DA, bool repel,
            double defaultValue, double defaultMax)
        {
            var targets = new List<Point3d>();
            if (!DA.GetDataList(0, targets) || targets.Count == 0)
            {
                comp.Warn("At least one Target point is required.");
                return;
            }

            var thresholds = new List<double>();
            DA.GetDataList(1, thresholds); // may be empty -> fallback used

            double value = defaultValue, max = defaultMax;
            GhInput.GetDouble(DA, 2, ref value);
            GhInput.GetDouble(DA, 3, ref max);

            var spec = new ForceSpec(repel, GhInput.PointsToFlat(targets), thresholds.ToArray(), value, max);
            DA.SetData(0, new ParticleGoo<ForceSpec>(spec));

            // Threshold visualisation: one circle per target on the world XY plane. VERIFY: plane per dimension.
            var circles = new List<Circle>(targets.Count);
            for (int i = 0; i < targets.Count; i++)
                circles.Add(new Circle(new Plane(targets[i], Vector3d.ZAxis), GhInput.ThresholdFor(thresholds, i)));
            DA.SetDataList(1, circles);
        }
    }
}
