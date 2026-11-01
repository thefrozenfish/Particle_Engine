using Grasshopper.Kernel;
using ParticleEngine.Core;
using ParticleEngine.Core.Manifest;
using ParticleEngine.Core.Settings;

namespace ParticleEngine.Grasshopper.Components
{
    /// <summary>
    /// Creeper_Engine — the Timer-driven simulation engine. Advances exactly one frame per solution
    /// (spec R4): item access on every input plus a DA.Iteration guard. Reset re-initialises to frame 0.
    /// Graphic mode draws heads/trails through the display pipeline; Geometry mode outputs real geometry.
    /// </summary>
    public sealed class CreeperEngineComponent : EngineComponentBase
    {
        private bool _initialized;

        public CreeperEngineComponent() : base(ComponentCatalog.ByKey("CreeperEngine")) { }

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            // One simulation per solution: ignore extra branches/iterations (spec R4).
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

            bool reset = false;
            GhInput.GetBool(DA, 4, ref reset);

            if (Sim == null) Sim = new CreeperSimulation();

            // First solve or Reset -> frame 0; otherwise advance one frame. Behaviour/Visual changes take
            // effect on the next frame; Init/Move changes apply on the next Reset (VERIFY P13 / spec 4.4).
            if (!_initialized || reset)
            {
                Sim.Initialize(init, move, trail, Seed, behaviors);
                _initialized = true;
            }
            else
            {
                Sim.Behaviors = behaviors;
                Sim.Step();
            }

            Message = "frame " + Sim.Frame + " · " + Sim.Count + " agents";
            EmitOutputs(DA);
        }

        protected override void ClearEngineState()
        {
            base.ClearEngineState();
            _initialized = false;
        }
    }
}
