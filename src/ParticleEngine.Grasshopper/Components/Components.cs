using Grasshopper.Kernel;
using ParticleEngine.Core.Manifest;
using ParticleEngine.Core.Settings;
using ParticleEngine.Grasshopper.Goo;

namespace ParticleEngine.Grasshopper.Components
{
    // All components now carry real behaviour. The bundling engines live in their own files
    // (BundlingEngineComponent.cs, BundlingZombieEngineComponent.cs); only the Bundling settings
    // component remains here.

    /// <summary>Bundling — settings for curve-network self-organisation, consumed by a bundling engine.</summary>
    public sealed class BundlingComponent : ParticleComponentBase
    {
        public BundlingComponent() : base(ComponentCatalog.ByKey("Bundling")) { }

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            double threshold = 100, ratio = 0.5;
            bool rebuild = true;
            int pointCount = 40, weldCount = 2;
            GhInput.GetDouble(DA, 0, ref threshold);
            GhInput.GetDouble(DA, 1, ref ratio);
            GhInput.GetBool(DA, 2, ref rebuild);
            GhInput.GetInt(DA, 3, ref pointCount);
            GhInput.GetInt(DA, 4, ref weldCount);

            DA.SetData(0, new ParticleGoo<BundlingSpec>(
                new BundlingSpec(threshold, ratio, rebuild, pointCount, weldCount)));
        }
    }
}
