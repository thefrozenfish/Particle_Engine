using System.Collections.Generic;
using Grasshopper.Kernel;
using ParticleEngine.Core.Manifest;
using ParticleEngine.Core.Settings;
using ParticleEngine.Grasshopper.Goo;
using Rhino.Geometry;

namespace ParticleEngine.Grasshopper.Components
{
    /// <summary>Stigmergy — chase other creepers' stored trails.</summary>
    public sealed class StigmergyComponent : ParticleComponentBase
    {
        public StigmergyComponent() : base(ComponentCatalog.ByKey("Stigmergy")) { }

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            double va = 60, cm = 1.5, cr = 80, sm = 0, sr = 5;
            GhInput.GetDouble(DA, 0, ref va);
            GhInput.GetDouble(DA, 1, ref cm);
            GhInput.GetDouble(DA, 2, ref cr);
            GhInput.GetDouble(DA, 3, ref sm);
            GhInput.GetDouble(DA, 4, ref sr);
            DA.SetData(0, new ParticleGoo<StigmergySpec>(new StigmergySpec(va, cm, cr, sm, sr)));
        }
    }

    /// <summary>Multi Path Tracking — follow the nearest of several polyline paths.</summary>
    public sealed class MultiPathTrackingComponent : ParticleComponentBase
    {
        public MultiPathTrackingComponent() : base(ComponentCatalog.ByKey("MultiPathTracking")) { }

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            var curves = new List<Curve>();
            if (!DA.GetDataList(0, curves) || curves.Count == 0)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "At least one path curve is required.");
                return;
            }

            double pt = 500, pd = 50, pr = 15;
            GhInput.GetDouble(DA, 1, ref pt);
            GhInput.GetDouble(DA, 2, ref pd);
            GhInput.GetDouble(DA, 3, ref pr);

            var polys = new double[curves.Count][];
            for (int i = 0; i < curves.Count; i++) polys[i] = GhInput.CurveToFlat(curves[i]);

            DA.SetData(0, new ParticleGoo<PathTrackingSpec>(new PathTrackingSpec(polys, pt, pd, pr)));
        }
    }

    /// <summary>Mesh Crawl — move creepers along the surface of a mesh.</summary>
    public sealed class MeshCrawlComponent : ParticleComponentBase
    {
        public MeshCrawlComponent() : base(ComponentCatalog.ByKey("MeshCrawl")) { }

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            Mesh mesh = null!;
            if (!DA.GetData(0, ref mesh) || mesh == null || !mesh.IsValid)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "A valid Mesh is required.");
                return;
            }

            double mt = 1.5, mpd = 2, mult = 1.5;
            GhInput.GetDouble(DA, 1, ref mt);
            GhInput.GetDouble(DA, 2, ref mpd);
            GhInput.GetDouble(DA, 3, ref mult);

            var spec = new MeshCrawlSpec(new MeshSurface(mesh), mt, mpd, mult);
            DA.SetData(0, new ParticleGoo<MeshCrawlSpec>(spec));
        }
    }
}
