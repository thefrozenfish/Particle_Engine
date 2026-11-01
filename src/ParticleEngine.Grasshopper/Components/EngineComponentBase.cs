using System;
using System.Collections.Generic;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Data;
using Grasshopper.Kernel.Types;
using ParticleEngine.Core;
using ParticleEngine.Core.Manifest;
using Rhino.Display;
using Rhino.Geometry;

namespace ParticleEngine.Grasshopper.Components
{
    /// <summary>
    /// Shared machinery for the creeper engines (Creeper_Engine, Creeper_ZombieEngine): holds the
    /// simulation instance, emits the Creepers / Trails / Connectivity outputs, and draws heads/trails
    /// in Graphic mode. Subclasses differ only in how they drive the sim (Timer step vs run-N-in-one-go).
    /// </summary>
    public abstract class EngineComponentBase : ParticleComponentBase
    {
        // Fixed seed so identical inputs reproduce identical runs (spec 7.10).
        protected const ulong Seed = 0x9E3779B97F4A7C15UL;

        protected CreeperSimulation? Sim;
        protected int DisplayMode; // 0 = Graphic, 1 = Geometry

        private Point3d[] _drawHeads = Array.Empty<Point3d>();
        private readonly List<Line> _drawLines = new List<Line>();
        private BoundingBox _clip = BoundingBox.Empty;

        protected EngineComponentBase(ComponentSpec spec) : base(spec) { }

        /// <summary>Emit head points, trail point-tree (Geometry mode) and connectivity lines; build the
        /// Graphic-mode draw buffers.</summary>
        protected void EmitOutputs(IGH_DataAccess DA)
        {
            ParticleState s = Sim!.State;
            int n = s.Count;

            var heads = new List<Point3d>(n);
            for (int i = 0; i < n; i++) heads.Add(new Point3d(s.Px[i], s.Py[i], s.Pz[i]));
            DA.SetDataList(0, heads);

            // Connectivity: lines between neighbours (Flocking Connect = true).
            var connLines = new List<Line>(Sim.ConnectivityCount);
            for (int k = 0; k < Sim.ConnectivityCount; k++)
            {
                int a = Sim.ConnectivityI[k], b = Sim.ConnectivityJ[k];
                if (a < n && b < n) connLines.Add(new Line(heads[a], heads[b]));
            }

            if (DisplayMode == 1)
            {
                // Geometry mode: trail history as points, one branch per creeper (oldest -> newest ->
                // current head), so a downstream PolyLine/Interpolate/Nurbs builds the agent curve.
                var tree = new GH_Structure<GH_Point>();
                if (Sim.TrailsStored)
                {
                    TrailBuffer t = Sim.Trails;
                    for (int i = 0; i < n; i++)
                    {
                        var path = new GH_Path(i);
                        for (int k = 0; k < t.Length(i); k++)
                        {
                            t.Get(i, k, out double x, out double y, out double z);
                            tree.Append(new GH_Point(new Point3d(x, y, z)), path);
                        }
                        tree.Append(new GH_Point(heads[i]), path);
                    }
                }
                else
                {
                    AddRuntimeMessage(GH_RuntimeMessageLevel.Remark,
                        "Connect Trail Data (Trail = true) to Visual Settings to record trails.");
                }
                DA.SetDataTree(1, tree);
                DA.SetDataList(2, connLines.ConvertAll(l => new GH_Line(l)));
                _drawHeads = Array.Empty<Point3d>();
                _drawLines.Clear();
                _clip = BoundingBox.Empty;
            }
            else
            {
                DA.SetDataTree(1, new GH_Structure<GH_Point>());
                DA.SetDataList(2, new List<GH_Line>());
                BuildGraphicBuffers(heads, connLines);
            }
        }

        private void BuildGraphicBuffers(List<Point3d> heads, List<Line> connLines)
        {
            _drawHeads = heads.ToArray();
            _drawLines.Clear();
            var clip = new BoundingBox(heads);

            if (Sim!.TrailsStored)
            {
                TrailBuffer t = Sim.Trails;
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
                    if (has) _drawLines.Add(new Line(prev, heads[i]));
                }
            }

            _drawLines.AddRange(connLines);
            _clip = clip;
        }

        public override void DrawViewportWires(IGH_PreviewArgs args)
        {
            if (Sim == null || DisplayMode != 0) return; // Graphic mode only
            if (Hidden || Locked) return;

            var colour = Attributes.Selected ? args.WireColour_Selected : args.WireColour;
            if (_drawLines.Count > 0) args.Display.DrawLines(_drawLines, colour);
            if (_drawHeads.Length > 0) args.Display.DrawPoints(_drawHeads, PointStyle.RoundSimple, 3, colour);
        }

        public override BoundingBox ClippingBox => _clip;

        public override bool IsPreviewCapable => true;

        /// <summary>Release simulation and draw state (document close, lock, delete). Subclasses extend this.</summary>
        protected virtual void ClearEngineState()
        {
            Sim = null;
            _drawHeads = Array.Empty<Point3d>();
            _drawLines.Clear();
            _clip = BoundingBox.Empty;
        }

        public override void RemovedFromDocument(GH_Document document)
        {
            ClearEngineState();
            base.RemovedFromDocument(document);
        }
    }
}
