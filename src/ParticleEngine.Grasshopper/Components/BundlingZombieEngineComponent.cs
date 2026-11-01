using Grasshopper.Kernel;
using ParticleEngine.Core;
using ParticleEngine.Core.Manifest;
using ParticleEngine.Core.Settings;

namespace ParticleEngine.Grasshopper.Components
{
    /// <summary>
    /// Bundling_ZombieEngine — runs curve-network bundling for a fixed number of Iterations in one go
    /// (no Timer), on the solution where Run goes false → true, then outputs the final curves. While Run
    /// stays unchanged it returns the cached result without recomputing. No Culebra equivalent; mirrors
    /// Creeper_ZombieEngine (spec 6.5). Synchronous for M6; a background-task version is a follow-up.
    /// </summary>
    public sealed class BundlingZombieEngineComponent : BundlingEngineBase
    {
        private bool _prevRun;
        private bool _hasResult;

        public BundlingZombieEngineComponent() : base(ComponentCatalog.ByKey("BundlingZombieEngine")) { }

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

            int iterations = 100;
            GhInput.GetInt(DA, 3, ref iterations);
            if (iterations < 0) iterations = 0;

            bool run = false;
            GhInput.GetBool(DA, 4, ref run);

            // Run-and-reset on the rising edge; while Run is unchanged, keep the cached result.
            if (run && !_prevRun)
            {
                double[][] curves = ReadCurves(DA, 0, spec);
                if (curves.Length == 0)
                {
                    AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "No curves to bundle.");
                    _prevRun = run;
                    return;
                }
                if (Bundle == null) Bundle = new BundlingSimulation();
                Bundle.Initialize(curves, spec);
                for (int f = 0; f < iterations; f++) Bundle.Step();
                _hasResult = true;
                Message = "ran " + iterations + " · " + Bundle.CurveCount + " curves";
            }
            _prevRun = run;

            if (_hasResult && Bundle != null) EmitGeometry(DA);
        }

        protected override void ClearEngineState()
        {
            base.ClearEngineState();
            _prevRun = false;
            _hasResult = false;
        }
    }
}
