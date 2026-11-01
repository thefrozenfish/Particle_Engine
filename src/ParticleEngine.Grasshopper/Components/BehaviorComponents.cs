using Grasshopper.Kernel;
using ParticleEngine.Core.Manifest;
using ParticleEngine.Core.Settings;
using ParticleEngine.Grasshopper.Goo;

namespace ParticleEngine.Grasshopper.Components
{
    /// <summary>Flocking — separation/alignment/cohesion behaviour for the Controller.</summary>
    public sealed class FlockingComponent : ParticleComponentBase
    {
        public FlockingComponent() : base(ComponentCatalog.ByKey("Flocking")) { }

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            bool connect = false;
            double va = 180, sr = 70, av = 0.24, sv = 0.09, cv = 0.045;
            GhInput.GetBool(DA, 0, ref connect);
            GhInput.GetDouble(DA, 1, ref va);
            GhInput.GetDouble(DA, 2, ref sr);
            GhInput.GetDouble(DA, 3, ref av);
            GhInput.GetDouble(DA, 4, ref sv);
            GhInput.GetDouble(DA, 5, ref cv);

            DA.SetData(0, new ParticleGoo<FlockingSpec>(new FlockingSpec(connect, va, sr, av, sv, cv)));
        }
    }
}
