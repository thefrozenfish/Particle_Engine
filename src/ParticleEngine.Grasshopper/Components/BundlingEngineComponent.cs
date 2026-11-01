using Grasshopper.Kernel;
using ParticleEngine.Core;
using ParticleEngine.Core.Manifest;
using ParticleEngine.Core.Settings;

namespace ParticleEngine.Grasshopper.Components
{
    /// <summary>
    /// Bundling_Engine — the Timer-driven curve-network bundling engine. Advances exactly one cycle per
    /// solution (spec R4): item access on the settings inputs plus a DA.Iteration guard. Reset re-reads
    /// the input curves and starts over. One cycle draws neighbouring curves a little closer together.
    /// </summary>
    public sealed class BundlingEngineComponent : BundlingEngineBase
    {
        private bool _initialized;

        public BundlingEngineComponent() : base(ComponentCatalog.ByKey("BundlingEngine")) { }

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            if (DA.Iteration > 0)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Warning,
                    "The engine runs one bundling per solution; connect single values (Item access).");
                return;
            }

            BundlingSpec? spec = ReadBundlingSpec(DA, 1);
            if (spec == null)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error,
                    "Behavioral Settings must contain a Bundling behaviour.");
                return;
            }

            VisualSettings? visual = GhInput.GetSettings<VisualSettings>(DA, 2);
            DisplayMode = visual?.DisplayMode ?? 0;

            bool reset = false;
            GhInput.GetBool(DA, 3, ref reset);

            if (Bundle == null) Bundle = new BundlingSimulation();

            if (!_initialized || reset)
            {
                double[][] curves = ReadCurves(DA, 0, spec);
                if (curves.Length == 0)
                {
                    AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "No curves to bundle.");
                    return;
                }
                Bundle.Initialize(curves, spec);
                _initialized = true;
            }
            else
            {
                Bundle.Step();
            }

            Message = "cycle " + Bundle.Cycle + " · " + Bundle.CurveCount + " curves";
            EmitGeometry(DA);
        }

        protected override void ClearEngineState()
        {
            base.ClearEngineState();
            _initialized = false;
        }
    }
}
