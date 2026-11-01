using System;
using System.Collections.Generic;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Types;
using ParticleEngine.Core;
using ParticleEngine.Core.Manifest;
using ParticleEngine.Core.Settings;
using Rhino.Display;
using Rhino.Geometry;

namespace ParticleEngine.Grasshopper.Components
{
    /// <summary>
    /// Shared machinery for the bundling engines (Bundling_Engine, Bundling_ZombieEngine): holds the
    /// bundling simulation, reads the curves + Bundling behaviour, rebuilds the self-organised curves
    /// for the Geometry output, and draws them in Graphic mode. Subclasses differ only in how they drive
    /// the sim (Timer cycle vs run-N-in-one-go).
    /// </summary>
    public abstract class BundlingEngineBase : ParticleComponentBase
    {
        protected BundlingSimulation? Bundle;
        protected int DisplayMode; // 0 = Graphic, 1 = Geometry

        private readonly List<Line> _drawLines = new List<Line>();
        private BoundingBox _clip = BoundingBox.Empty;

        protected BundlingEngineBase(ComponentSpec spec) : base(spec) { }

        /// <summary>Find the Bundling behaviour either inside a Controller stack or supplied directly.</summary>
        protected static BundlingSpec? ReadBundlingSpec(IGH_DataAccess DA, int index)
        {
            BehaviorData? data = GhInput.GetSettings<BehaviorData>(DA, index);
            if (data != null)
            {
                foreach (BehaviorSpec b in data.Stack)
                    if (b is BundlingSpec bs) return bs;
                return null;
            }
            return GhInput.GetSettings<BundlingSpec>(DA, index);
        }

        /// <summary>Read the input curves and sample them to flat x,y,z point arrays: resampled to
        /// PointCount points when Rebuild is on (spec 7.11), otherwise the curve's own polyline points.</summary>
        protected static double[][] ReadCurves(IGH_DataAccess DA, int index, BundlingSpec spec)
        {
            var curves = new List<Curve>();
            if (!DA.GetDataList(index, curves) || curves.Count == 0) return Array.Empty<double[]>();

            var arrays = new List<double[]>(curves.Count);
            foreach (Curve c in curves)
            {
                if (c == null) continue;
                arrays.Add(spec.Rebuild ? Resample(c, spec.PointCount) : GhInput.CurveToFlat(c));
            }
            return arrays.ToArray();
        }

        private static double[] Resample(Curve curve, int pointCount)
        {
            int pc = Math.Max(2, pointCount);
            double[] ts = curve.DivideByCount(pc - 1, true); // pc points, endpoints included
            var flat = new double[pc * 3];
            if (ts == null || ts.Length < pc)
            {
                // Degenerate curve: fall back to its endpoints.
                Point3d a = curve.PointAtStart, b = curve.PointAtEnd;
                for (int k = 0; k < pc; k++)
                {
                    double u = pc == 1 ? 0 : (double)k / (pc - 1);
                    Point3d p = a + (b - a) * u;
                    flat[k * 3] = p.X; flat[k * 3 + 1] = p.Y; flat[k * 3 + 2] = p.Z;
                }
                return flat;
            }
            for (int k = 0; k < pc; k++)
            {
                Point3d p = curve.PointAt(ts[k]);
                flat[k * 3] = p.X; flat[k * 3 + 1] = p.Y; flat[k * 3 + 2] = p.Z;
            }
            return flat;
        }

        /// <summary>Rebuild the current point arrays into polylines, emit them on the Geometry output and
        /// build the Graphic-mode draw buffers. VERIFY(P9): output curve type (polyline vs degree-3).</summary>
        protected void EmitGeometry(IGH_DataAccess DA)
        {
            double[][] arrays = Bundle!.ToCurves();
            var curves = new List<GH_Curve>(arrays.Length);
            _drawLines.Clear();
            var clip = BoundingBox.Empty;

            foreach (double[] arr in arrays)
            {
                int m = arr.Length / 3;
                if (m < 2) continue;
                var pts = new Point3d[m];
                for (int k = 0; k < m; k++) pts[k] = new Point3d(arr[k * 3], arr[k * 3 + 1], arr[k * 3 + 2]);

                var pl = new Polyline(pts);
                curves.Add(new GH_Curve(new PolylineCurve(pts)));

                for (int k = 1; k < m; k++) _drawLines.Add(new Line(pts[k - 1], pts[k]));
                clip.Union(pl.BoundingBox);
            }

            DA.SetDataList(0, curves);
            _clip = clip;
        }

        public override void DrawViewportWires(IGH_PreviewArgs args)
        {
            if (Bundle == null || DisplayMode != 0) return; // Graphic mode only
            if (Hidden || Locked) return;
            if (_drawLines.Count == 0) return;

            var colour = Attributes.Selected ? args.WireColour_Selected : args.WireColour;
            args.Display.DrawLines(_drawLines, colour);
        }

        public override BoundingBox ClippingBox => _clip;

        public override bool IsPreviewCapable => true;

        protected virtual void ClearEngineState()
        {
            Bundle = null;
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
