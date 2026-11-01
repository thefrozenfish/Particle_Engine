using System.Collections.Generic;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Data;
using Grasshopper.Kernel.Types;
using ParticleEngine.Core;
using ParticleEngine.Core.Manifest;
using ParticleEngine.Core.Settings;
using Rhino.Display;
using Rhino.Geometry;

namespace ParticleEngine.Grasshopper.Components
{
    /// <summary>
    /// Creeper_Engine — the Timer-driven simulation engine (M2 scope: straight-line motion from Move
    /// Settings, boundaries, trails; behaviours arrive M3+). Advances exactly one frame per solution
    /// (spec R4): item access on every input plus a DA.Iteration guard. Reset re-initialises to frame 0.
    /// Graphic mode draws heads/trails through the display pipeline; Geometry mode outputs real geometry.
    /// </summary>
    public sealed class CreeperEngineComponent : ParticleComponentBase
    {
        // Fixed seed so identical inputs reproduce identical runs (spec 7.10).
        private const ulong Seed = 0x9E3779B97F4A7C15UL;

        private CreeperSimulation? _sim;
        private bool _initialized;
        private int _displayMode; // 0 = Graphic, 1 = Geometry

        // Cached draw buffers for Graphic mode (rebuilt only when a frame advances).
        private Point3d[] _drawHeads = System.Array.Empty<Point3d>();
        private readonly List<Line> _drawLines = new List<Line>();
        private BoundingBox _clip = BoundingBox.Empty;

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

            // Behavioral Settings (index 2) is read in M3+; ignored here.

            VisualSettings? visual = GhInput.GetSettings<VisualSettings>(DA, 3);
            _displayMode = visual?.DisplayMode ?? 0;
            TrailDataSettings? trail = visual?.Trail;

            bool reset = false;
            GhInput.GetBool(DA, 4, ref reset);

            if (_sim == null) _sim = new CreeperSimulation();

            // First solve or Reset -> frame 0; otherwise advance one frame. (InitSettings/Move changes
            // mid-run take effect on the next Reset — VERIFY P13 / spec 4.4.)
            if (!_initialized || reset)
            {
                _sim.Initialize(init, move, trail, Seed);
                _initialized = true;
            }
            else
            {
                _sim.Step();
            }

            Message = "frame " + _sim.Frame + " · " + _sim.Count + " agents";

            EmitOutputs(DA);
        }

        private void EmitOutputs(IGH_DataAccess DA)
        {
            ParticleState s = _sim!.State;
            int n = s.Count;

            // Creepers: head positions, always produced.
            var heads = new List<Point3d>(n);
            for (int i = 0; i < n; i++) heads.Add(new Point3d(s.Px[i], s.Py[i], s.Pz[i]));
            DA.SetDataList(0, heads);

            if (_displayMode == 1)
            {
                // Geometry mode: trail HISTORY as points, one branch per creeper (oldest -> newest ->
                // current head), so a downstream PolyLine/Interpolate/Nurbs component builds the agent
                // curve. The Trails port is generic, so points are valid output.
                var tree = new GH_Structure<GH_Point>();
                if (_sim.TrailsStored)
                {
                    TrailBuffer t = _sim.Trails;
                    for (int i = 0; i < n; i++)
                    {
                        var path = new GH_Path(i);
                        for (int k = 0; k < t.Length(i); k++)
                        {
                            t.Get(i, k, out double x, out double y, out double z);
                            tree.Append(new GH_Point(new Point3d(x, y, z)), path);
                        }
                        tree.Append(new GH_Point(heads[i]), path); // current position last
                    }
                }
                else
                {
                    AddRuntimeMessage(GH_RuntimeMessageLevel.Remark,
                        "Connect Trail Data (Trail = true) to Visual Settings to record trails.");
                }
                DA.SetDataTree(1, tree);
                _drawHeads = System.Array.Empty<Point3d>();
                _drawLines.Clear();
                _clip = BoundingBox.Empty;
            }
            else
            {
                // Graphic mode: no geometry outputs; build the draw buffers for DrawViewportWires.
                DA.SetDataTree(1, new GH_Structure<GH_Point>());
                BuildGraphicBuffers(heads);
            }

            // Connectivity is produced only by Flocking with Connect = true (M3); empty here.
            DA.SetDataList(2, new List<GH_Point>());
        }

        private void BuildGraphicBuffers(List<Point3d> heads)
        {
            _drawHeads = heads.ToArray();
            _drawLines.Clear();
            var clip = new BoundingBox(heads);

            if (_sim!.TrailsStored)
            {
                TrailBuffer t = _sim.Trails;
                for (int i = 0; i < heads.Count; i++)
                {
                    Point3d prev = default;
                    bool has = false;
                    for (int k = 0; k < t.Length(i); k++)
                    {
                        t.Get(i, k, out double x, out double y, out double z);
                        var p = new Point3d(x, y, z);
                        if (has) _drawLines.Add(new Line(prev, p));
                        prev = p; has = true;
                        clip.Union(p);
                    }
                    if (has) _drawLines.Add(new Line(prev, heads[i])); // last stored -> head
                }
            }

            _clip = clip;
        }

        public override void DrawViewportWires(IGH_PreviewArgs args)
        {
            if (_sim == null || _displayMode != 0) return; // Graphic mode only
            if (Hidden || Locked) return;

            var colour = Attributes.Selected ? args.WireColour_Selected : args.WireColour;
            if (_drawLines.Count > 0) args.Display.DrawLines(_drawLines, colour);
            if (_drawHeads.Length > 0) args.Display.DrawPoints(_drawHeads, PointStyle.RoundSimple, 3, colour);
        }

        public override BoundingBox ClippingBox => _clip;

        public override bool IsPreviewCapable => true;

        public override void RemovedFromDocument(GH_Document document)
        {
            _sim = null;
            _initialized = false;
            _drawHeads = System.Array.Empty<Point3d>();
            _drawLines.Clear();
            _clip = BoundingBox.Empty;
            base.RemovedFromDocument(document);
        }
    }
}
