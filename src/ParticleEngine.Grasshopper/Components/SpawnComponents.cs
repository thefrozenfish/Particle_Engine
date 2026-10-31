using System.Collections.Generic;
using Grasshopper.Kernel;
using ParticleEngine.Core.Manifest;
using ParticleEngine.Core.Settings;
using ParticleEngine.Grasshopper.Goo;
using Rhino.Geometry;

namespace ParticleEngine.Grasshopper.Components
{
    /// <summary>Box Spawn — spawn settings for creepers inside a box (grid or random, 2D/3D).</summary>
    public sealed class BoxSpawnComponent : ParticleComponentBase
    {
        public BoxSpawnComponent() : base(ComponentCatalog.ByKey("BoxSpawn")) { }

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            Box box = Box.Unset;
            if (!DA.GetData(0, ref box) || !box.IsValid)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "A valid Box is required.");
                return;
            }

            int location = 0, count = 100;
            GhInput.GetInt(DA, 1, ref location);
            GhInput.GetInt(DA, 2, ref count);
            if (count < 0) count = 0;

            GhInput.BoxBounds(box, out double[] min, out double[] max);
            var settings = new SpawnSettings(SpawnMode.Box, min, max, location, count, System.Array.Empty<double>());
            DA.SetData(0, new ParticleGoo<SpawnSettings>(settings));
        }
    }

    /// <summary>Point Spawn — one creeper per point; the box bounds the boundary mode.</summary>
    public sealed class PointSpawnComponent : ParticleComponentBase
    {
        public PointSpawnComponent() : base(ComponentCatalog.ByKey("PointSpawn")) { }

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            var points = new List<Point3d>();
            if (!DA.GetDataList(0, points) || points.Count == 0)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "At least one point is required.");
                return;
            }

            Box box = Box.Unset;
            double[] min, max;
            if (DA.GetData(1, ref box) && box.IsValid)
            {
                GhInput.BoxBounds(box, out min, out max);
            }
            else
            {
                // No box supplied: pad the point cloud's bounding box so there is a boundary volume.
                var bb = new BoundingBox(points);
                double pad = bb.Diagonal.Length * 0.1 + 1.0;
                min = new[] { bb.Min.X - pad, bb.Min.Y - pad, bb.Min.Z - pad };
                max = new[] { bb.Max.X + pad, bb.Max.Y + pad, bb.Max.Z + pad };
            }

            var settings = new SpawnSettings(SpawnMode.Points, min, max, 1, 0, GhInput.PointsToFlat(points));
            DA.SetData(0, new ParticleGoo<SpawnSettings>(settings));
        }
    }
}
