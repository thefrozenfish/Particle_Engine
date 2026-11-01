using System;
using ParticleEngine.Core.Settings;

namespace ParticleEngine.Core
{
    /// <summary>
    /// Curve-network self-organisation ("bundling", spec 7.11). Each cycle moves every non-pinned point
    /// toward the centroid of nearby points on OTHER curves, by at most Ratio, so separate curves draw
    /// together into bundles. Double-buffered so a cycle reads only the previous cycle's positions.
    /// Shared Core implementation behind both bundling engines (spec 6.5). Interim rules tagged VERIFY(P9).
    /// </summary>
    public sealed class BundlingSimulation
    {
        private double[] _px = Array.Empty<double>();
        private double[] _py = Array.Empty<double>();
        private double[] _pz = Array.Empty<double>();
        private double[] _qx = Array.Empty<double>();
        private double[] _qy = Array.Empty<double>();
        private double[] _qz = Array.Empty<double>();
        private int[] _curveOf = Array.Empty<int>();     // curve index per point
        private int[] _curveStart = Array.Empty<int>();   // CSR ranges: curve c owns [start[c], start[c+1])
        private bool[] _pinned = Array.Empty<bool>();

        private int _pointCount;
        private int _curveCount;
        private double _threshold = 1;
        private double _ratio = 0.1;

        private readonly UniformGrid _grid = new UniformGrid();

        private double[][]? _initialCurves;
        private BundlingSpec? _spec;

        public int Cycle { get; private set; }
        public int CurveCount => _curveCount;
        public int PointCount => _pointCount;

        /// <summary>Set up from curves already sampled to point arrays (flat x,y,z per curve). Weld pins the
        /// first and last WeldCount points of each curve.</summary>
        public void Initialize(double[][] curves, BundlingSpec spec)
        {
            _initialCurves = curves ?? Array.Empty<double[]>();
            _spec = spec;
            _threshold = spec.Threshold;
            _ratio = spec.Ratio;
            _curveCount = _initialCurves.Length;

            int total = 0;
            for (int c = 0; c < _curveCount; c++) total += _initialCurves[c].Length / 3;

            EnsureCapacity(total, _curveCount);

            int idx = 0;
            _curveStart[0] = 0;
            for (int c = 0; c < _curveCount; c++)
            {
                double[] pts = _initialCurves[c];
                int m = pts.Length / 3;
                for (int k = 0; k < m; k++)
                {
                    _px[idx] = pts[k * 3]; _py[idx] = pts[k * 3 + 1]; _pz[idx] = pts[k * 3 + 2];
                    _curveOf[idx] = c;
                    idx++;
                }
                _curveStart[c + 1] = idx;
            }
            _pointCount = idx;

            int wc = Math.Max(0, spec.WeldCount);
            for (int c = 0; c < _curveCount; c++)
            {
                int s = _curveStart[c], e = _curveStart[c + 1], cnt = e - s;
                for (int k = 0; k < cnt; k++)
                    _pinned[s + k] = k < wc || k >= cnt - wc; // first/last WeldCount points are pinned
            }

            Cycle = 0;
        }

        public void Reset()
        {
            if (_initialCurves != null && _spec != null) Initialize(_initialCurves, _spec);
        }

        /// <summary>One bundling cycle.</summary>
        public void Step()
        {
            if (_pointCount == 0) return;

            _grid.Build(_px, _py, _pz, _pointCount, _threshold > 1e-9 ? _threshold : 1.0, false);
            double thr2 = _threshold * _threshold;

            for (int i = 0; i < _pointCount; i++)
            {
                if (_pinned[i]) { _qx[i] = _px[i]; _qy[i] = _py[i]; _qz[i] = _pz[i]; continue; }

                double cxSum = 0, cySum = 0, czSum = 0; int count = 0;
                int myCurve = _curveOf[i];
                double pix = _px[i], piy = _py[i], piz = _pz[i];

                _grid.CellCoords(pix, piy, piz, out int gcx, out int gcy, out int gcz);
                for (int gz = gcz - 1; gz <= gcz + 1; gz++)
                    for (int gy = gcy - 1; gy <= gcy + 1; gy++)
                        for (int gx = gcx - 1; gx <= gcx + 1; gx++)
                        {
                            int c = _grid.CellIndexOf(gx, gy, gz);
                            if (c < 0) continue;
                            for (int kk = _grid.CellStart[c]; kk < _grid.CellStart[c + 1]; kk++)
                            {
                                int j = _grid.Items[kk];
                                if (_curveOf[j] == myCurve) continue; // only OTHER curves
                                double dx = _px[j] - pix, dy = _py[j] - piy, dz = _pz[j] - piz;
                                if (dx * dx + dy * dy + dz * dz >= thr2) continue;
                                cxSum += _px[j]; cySum += _py[j]; czSum += _pz[j]; count++;
                            }
                        }

                if (count == 0) { _qx[i] = pix; _qy[i] = piy; _qz[i] = piz; continue; }

                double tx = cxSum / count - pix, ty = cySum / count - piy, tz = czSum / count - piz;
                double dist = Math.Sqrt(tx * tx + ty * ty + tz * tz);
                if (dist <= 1e-12) { _qx[i] = pix; _qy[i] = piy; _qz[i] = piz; continue; }

                double move = Math.Min(_ratio, dist); // VERIFY(P9): Ratio absolute vs fraction
                double s = move / dist;
                _qx[i] = pix + tx * s; _qy[i] = piy + ty * s; _qz[i] = piz + tz * s;
            }

            Swap(ref _px, ref _qx); Swap(ref _py, ref _qy); Swap(ref _pz, ref _qz);
            Cycle++;
        }

        /// <summary>Current points grouped back into one flat x,y,z array per curve (for rebuilding geometry).</summary>
        public double[][] ToCurves()
        {
            var result = new double[_curveCount][];
            for (int c = 0; c < _curveCount; c++)
            {
                int s = _curveStart[c], e = _curveStart[c + 1], cnt = e - s;
                var arr = new double[cnt * 3];
                for (int k = 0; k < cnt; k++)
                {
                    arr[k * 3] = _px[s + k]; arr[k * 3 + 1] = _py[s + k]; arr[k * 3 + 2] = _pz[s + k];
                }
                result[c] = arr;
            }
            return result;
        }

        private void EnsureCapacity(int points, int curves)
        {
            if (_px.Length < points)
            {
                _px = new double[points]; _py = new double[points]; _pz = new double[points];
                _qx = new double[points]; _qy = new double[points]; _qz = new double[points];
                _curveOf = new int[points]; _pinned = new bool[points];
            }
            if (_curveStart.Length < curves + 1) _curveStart = new int[curves + 1];
        }

        private static void Swap(ref double[] a, ref double[] b) { var t = a; a = b; b = t; }
    }
}
