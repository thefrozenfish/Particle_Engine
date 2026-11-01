using Grasshopper.Kernel;
using ParticleEngine.Core;
using ParticleEngine.Core.Manifest;
using ParticleEngine.Core.Settings;

namespace ParticleEngine.Grasshopper.Components
{
    /// <summary>
    /// Creeper_ZombieEngine — runs the whole simulation for a fixed number of Iterations in one go
    /// (no Timer), on the solution where Run goes false → true, then outputs the final state. While Run
    /// stays unchanged it returns the cached result without recomputing (spec 6.5).
    ///
    /// M5 is synchronous: it runs on the solve thread. The background-task + cancellation version (so a
    /// 10k-agent, 1k-iteration run keeps the UI responsive) is a follow-up refinement.
    /// </summary>
    public sealed class CreeperZombieEngineComponent : EngineComponentBase
    {
        private bool _prevRun;
        private bool _hasResult;

        public CreeperZombieEngineComponent() : base(ComponentCatalog.ByKey("CreeperZombieEngine")) { }

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            if (DA.Iteration > 0)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Warning,
                    "The engine runs one simulation per solution; connect single values (Item access).");
                return;
            }

            InitSettings? init = GhInput.GetSettings<InitSettings>(DA, 0);
            if (init == null)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Init Settings is required.");
                return;
            }

            MoveSettings move = GhInput.GetSettings<MoveSettings>(DA, 1)
                                ?? new MoveSettings(false, 1, 0, 0, 0, 5, 0.5, 1);
            BehaviorData? behaviors = GhInput.GetSettings<BehaviorData>(DA, 2);

            VisualSettings? visual = GhInput.GetSettings<VisualSettings>(DA, 3);
            DisplayMode = visual?.DisplayMode ?? 0;
            TrailDataSettings? trail = visual?.Trail;

            int iterations = 100;
            GhInput.GetInt(DA, 4, ref iterations);
            if (iterations < 0) iterations = 0;

            bool run = false;
            GhInput.GetBool(DA, 5, ref run);

            // Run-and-reset on the rising edge; while Run is unchanged, keep the cached result.
            if (run && !_prevRun)
            {
                if (Sim == null) Sim = new CreeperSimulation();
                Sim.Initialize(init, move, trail, Seed, behaviors);
                for (int f = 0; f < iterations; f++) Sim.Step();
                _hasResult = true;
                Message = "ran " + iterations + " · " + Sim.Count + " agents";
            }
            _prevRun = run;

            if (_hasResult && Sim != null) EmitOutputs(DA);
        }

        protected override void ClearEngineState()
        {
            base.ClearEngineState();
            _prevRun = false;
            _hasResult = false;
        }
    }
}
