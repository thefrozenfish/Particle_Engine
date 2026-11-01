using System;
using ParticleEngine.Core.Settings;

namespace ParticleEngine.Core
{
    /// <summary>
    /// The creeper simulation (M2 scope: spawn, straight-line motion from Move Settings, boundary
    /// handling, trails; no behaviours yet — those arrive M3+). Struct-of-arrays state, own PRNG,
    /// double-buffered step (spec 7.1). One instance per engine component; never static.
    ///
    /// Several rules are interim pending Culebra probes and are tagged VERIFY(Pn); see docs/PROGRESS.md.
    /// </summary>
    public sealed class CreeperSimulation
    {
        private readonly ParticleState _state = new ParticleState();
        private readonly TrailBuffer _trails = new TrailBuffer();
        private Pcg32 _rng;

        private InitSettings? _init;
        private MoveSettings? _move;
        private TrailDataSettings? _trail;
        private ulong _seed;

        private int _dimension;  // 0 = 2D, 1 = 3D
        private int _boundary;   // 0 = Bounce, 1 = Respawn, 2 = Do Nothing
        private double _x0, _y0, _z0, _x1, _y1, _z1; // box bounds (sorted)
        private bool _storeTrails;
        private int _trailStep = 1;
        private int _maxTrail = 100;

        private readonly UniformGrid _grid = new UniformGrid();
        private bool _needGrid;

        // Neighbour pairs recorded when a Flocking behaviour has Connect = true (spec 7.2). Capped.
        private const int MaxConnections = 200000;
        private int[] _connI = Array.Empty<int>();
        private int[] _connJ = Array.Empty<int>();
        private int _connCount;
        private bool _recordConn;

        /// <summary>Number of neighbour-connection pairs recorded this frame (Flocking Connect = true).</summary>
        public int ConnectivityCount => _connCount;
        public int[] ConnectivityI => _connI;
        public int[] ConnectivityJ => _connJ;

        // Trail-point grid for Stigmergy (spec 7.3): all creepers' trail points, flattened each frame
        // with their owner, so an agent can steer by *other* creepers' trails.
        private readonly UniformGrid _trailGrid = new UniformGrid();
        private double[] _tpx = Array.Empty<double>();
        private double[] _tpy = Array.Empty<double>();
        private double[] _tpz = Array.Empty<double>();
        private int[] _tpOwner = Array.Empty<int>();
        private int _tpCount;

        private bool _hasStigmergy;
        private bool _hasPath;
        private bool _hasMeshCrawl;
        private MeshCrawlSpec? _meshSpec;
        private bool _trailsActive; // trails stored when Trail Data is on OR a Stigmergy behaviour is present (spec 7.8)

        /// <summary>Behaviour/force stack from the Controller. May change between frames (spec 4.4);
        /// the engine sets it each solve. Null or empty = straight-line motion (M2).</summary>
        public BehaviorData? Behaviors { get; set; }

        public int Frame { get; private set; }
        public int Count => _state.Count;
        public ParticleState State => _state;
        public TrailBuffer Trails => _trails;
        public bool TrailsStored => _trailsActive;

        /// <summary>Build frame 0 from the settings. Default seed is fixed, so identical inputs reproduce
        /// identical runs (spec 7.10).</summary>
        public void Initialize(InitSettings init, MoveSettings move, TrailDataSettings? trail,
            ulong seed = 0x9E3779B97F4A7C15UL, BehaviorData? behaviors = null)
        {
            _init = init ?? throw new ArgumentNullException(nameof(init));
            _move = move ?? throw new ArgumentNullException(nameof(move));
            _trail = trail;
            _seed = seed;
            Behaviors = behaviors;
            _rng = new Pcg32(seed);

            _dimension = init.Dimension == 0 ? 0 : 1;
            _boundary = init.Boundary;

            SpawnSettings sp = init.Spawn ?? throw new InvalidOperationException("Init Settings has no spawn settings.");
            SortBounds(sp);

            // Trail storage runs when Trail = true OR a Stigmergy behaviour is present (spec 7.8).
            _storeTrails = trail != null && trail.Trail;
            _trailStep = Math.Max(1, trail?.TrailStep ?? 1);
            _maxTrail = Math.Max(2, trail?.MaxTrailSize ?? 100);
            UpdateBehaviorFlags();

            Spawn(sp, move);
            _trails.Reset(_state.Count, _maxTrail);
            if (_trailsActive)
                for (int i = 0; i < _state.Count; i++)
                    _trails.Push(i, _state.Px[i], _state.Py[i], _state.Pz[i]);

            Frame = 0;
        }

        /// <summary>Re-initialise to frame 0 from the same settings/seed (Reset = true).</summary>
        public void Reset()
        {
            if (_init != null && _move != null)
                Initialize(_init, _move, _trail, _seed, Behaviors);
        }

        /// <summary>Advance exactly one frame. Reads only the front buffers, so the per-particle update
        /// is order-independent (spec 7.1).</summary>
        public void Step()
        {
            int n = _state.Count;
            if (n == 0) return;

            double maxSpeed = _move!.MaxSpeed;
            double maxForce = _move.MaxForce;
            double vm = _move.VelocityMultiplier;

            PrepareBehaviors();

            for (int i = 0; i < n; i++)
            {
                // Steering acceleration from the behaviour/force stack (spec 7.1), clamped to Max Force.
                double ax = 0, ay = 0, az = 0;
                AccumulateSteering(i, maxSpeed, maxForce, ref ax, ref ay, ref az);
                VectorMath.ClampLength(ref ax, ref ay, ref az, maxForce);

                double vx = _state.Vx[i] + ax;
                double vy = _state.Vy[i] + ay;
                double vz = _state.Vz[i] + az;
                VectorMath.ClampLength(ref vx, ref vy, ref vz, maxSpeed);
                if (_dimension == 0) vz = 0.0;

                // VERIFY(P1): whether VM scales velocity or displacement — interim: displacement.
                double px = _state.Px[i] + vx * vm;
                double py = _state.Py[i] + vy * vm;
                double pz = _dimension == 0 ? _state.Pz[i] : _state.Pz[i] + vz * vm;

                ApplyBoundary(i, ref px, ref py, ref pz, ref vx, ref vy, ref vz);

                // Mesh Crawl snaps the position to the surface and flattens the velocity (spec 7.5).
                if (_hasMeshCrawl && _meshSpec?.Surface != null)
                    ProjectToMesh(_meshSpec, ref px, ref py, ref pz, ref vx, ref vy, ref vz);

                _state.Qx[i] = px; _state.Qy[i] = py; _state.Qz[i] = pz;
                _state.Ux[i] = vx; _state.Uy[i] = vy; _state.Uz[i] = vz;
            }

            _state.SwapBuffers();
            Frame++;

            // VERIFY(P11): exactly when a trail point is stored / whether the head is included.
            if (_trailsActive && Frame % _trailStep == 0)
                for (int i = 0; i < _state.Count; i++)
                    _trails.Push(i, _state.Px[i], _state.Py[i], _state.Pz[i]);
        }

        // ---- behaviours (spec 7.1-7.4) ----

        /// <summary>Per-frame setup: build the neighbour grid once if the stack contains Flocking.</summary>
        private void PrepareBehaviors()
        {
            _needGrid = false;
            _recordConn = false;
            _connCount = 0;
            UpdateBehaviorFlags();
            if (Behaviors == null) return;

            double maxSearch = 0, maxTrailRange = 0;
            BehaviorSpec[] specs = Behaviors.Specs;
            for (int si = 0; si < specs.Length; si++)
            {
                BehaviorSpec spec = specs[si];
                if (spec is FlockingSpec f)
                {
                    _needGrid = true;
                    if (f.SearchRadius > maxSearch) maxSearch = f.SearchRadius;
                    if (f.Connect) _recordConn = true;
                }
                else if (spec is StigmergySpec s)
                {
                    double r = Math.Max(s.CohesionRange, s.SeparationRange);
                    if (r > maxTrailRange) maxTrailRange = r;
                }
            }

            if (_needGrid)
                _grid.Build(_state.Px, _state.Py, _state.Pz, _state.Count, maxSearch > 1e-9 ? maxSearch : 1.0, _dimension == 0);

            if (_hasStigmergy)
                BuildTrailGrid(maxTrailRange > 1e-9 ? maxTrailRange : 1.0);
        }

        private void UpdateBehaviorFlags()
        {
            _hasStigmergy = _hasPath = _hasMeshCrawl = false;
            _meshSpec = null;
            if (Behaviors != null)
            {
                BehaviorSpec[] specs = Behaviors.Specs;
                for (int si = 0; si < specs.Length; si++)
                {
                    BehaviorSpec spec = specs[si];
                    if (spec is StigmergySpec) _hasStigmergy = true;
                    else if (spec is PathTrackingSpec) _hasPath = true;
                    else if (spec is MeshCrawlSpec m) { _hasMeshCrawl = true; if (_meshSpec == null) _meshSpec = m; }
                }
            }
            _trailsActive = _storeTrails || _hasStigmergy;
        }

        /// <summary>Flatten every creeper's trail points (with owner) and build a grid over them (spec 7.3).</summary>
        private void BuildTrailGrid(double cell)
        {
            int total = 0;
            for (int i = 0; i < _state.Count; i++) total += _trails.Length(i);

            if (_tpx.Length < total)
            {
                _tpx = new double[total]; _tpy = new double[total]; _tpz = new double[total];
                _tpOwner = new int[total];
            }

            int idx = 0;
            for (int i = 0; i < _state.Count; i++)
                for (int k = 0; k < _trails.Length(i); k++)
                {
                    _trails.Get(i, k, out double x, out double y, out double z);
                    _tpx[idx] = x; _tpy[idx] = y; _tpz[idx] = z; _tpOwner[idx] = i;
                    idx++;
                }

            _tpCount = idx;
            _trailGrid.Build(_tpx, _tpy, _tpz, _tpCount, cell, _dimension == 0);
        }

        private void ProjectToMesh(MeshCrawlSpec m, ref double px, ref double py, ref double pz,
            ref double vx, ref double vy, ref double vz)
        {
            if (!m.Surface!.Closest(px, py, pz, 0, out double cx, out double cy, out double cz,
                    out double nx, out double ny, out double nz))
                return;

            px = cx; py = cy; pz = cz; // snap to the surface (VERIFY P7)
            double vdotn = vx * nx + vy * ny + vz * nz; // remove velocity along the normal
            vx -= vdotn * nx; vy -= vdotn * ny; vz -= vdotn * nz;
        }

        private void AccumulateSteering(int i, double maxSpeed, double maxForce,
            ref double ax, ref double ay, ref double az)
        {
            if (Behaviors == null) return;
            BehaviorSpec[] specs = Behaviors.Specs;
            for (int si = 0; si < specs.Length; si++)
            {
                BehaviorSpec spec = specs[si];
                if (spec is FlockingSpec flock) AddFlocking(i, flock, maxSpeed, maxForce, ref ax, ref ay, ref az);
                else if (spec is ForceSpec force) AddForce(i, force, ref ax, ref ay, ref az);
                else if (spec is StigmergySpec stig) AddStigmergy(i, stig, maxSpeed, maxForce, ref ax, ref ay, ref az);
                else if (spec is PathTrackingSpec path) AddPathTracking(i, path, maxSpeed, maxForce, ref ax, ref ay, ref az);
                else if (spec is MeshCrawlSpec mesh) AddMeshCrawl(i, mesh, maxSpeed, maxForce, ref ax, ref ay, ref az);
                // BundlingSpec: M6.
            }
        }

        /// <summary>Reynolds flocking (spec 7.2): separation, alignment, cohesion over neighbours in the
        /// search radius and inside the view cone. VERIFY(P4): View Angle full vs half field of view.</summary>
        private void AddFlocking(int i, FlockingSpec f, double maxSpeed, double maxForce,
            ref double ax, ref double ay, ref double az)
        {
            double pix = _state.Px[i], piy = _state.Py[i], piz = _state.Pz[i];
            double vix = _state.Vx[i], viy = _state.Vy[i], viz = _state.Vz[i];

            double sr2 = f.SearchRadius * f.SearchRadius;
            double cosHalf = Math.Cos(f.ViewAngle * 0.5 * Math.PI / 180.0); // precomputed per particle, not per neighbour
            double viLen = Math.Sqrt(vix * vix + viy * viy + viz * viz);
            bool hasVel = viLen > 1e-12;

            double sepX = 0, sepY = 0, sepZ = 0;
            double aliX = 0, aliY = 0, aliZ = 0;
            double cohX = 0, cohY = 0, cohZ = 0;
            int count = 0;

            _grid.CellCoords(pix, piy, piz, out int cx, out int cy, out int cz);
            int zlo = _dimension == 0 ? cz : cz - 1;
            int zhi = _dimension == 0 ? cz : cz + 1;

            for (int gz = zlo; gz <= zhi; gz++)
                for (int gy = cy - 1; gy <= cy + 1; gy++)
                    for (int gx = cx - 1; gx <= cx + 1; gx++)
                    {
                        int c = _grid.CellIndexOf(gx, gy, gz);
                        if (c < 0) continue;
                        int start = _grid.CellStart[c], end = _grid.CellStart[c + 1];
                        for (int k = start; k < end; k++)
                        {
                            int j = _grid.Items[k];
                            if (j == i) continue;

                            double dx = _state.Px[j] - pix, dy = _state.Py[j] - piy, dz = _state.Pz[j] - piz;
                            double d2 = dx * dx + dy * dy + dz * dz;
                            if (d2 >= sr2 || d2 <= 0) continue; // squared compare; sqrt only for survivors

                            if (hasVel)
                            {
                                double d = Math.Sqrt(d2);
                                double dot = vix * dx + viy * dy + viz * dz;
                                if (dot < cosHalf * viLen * d) continue; // outside the view cone
                            }

                            sepX -= dx / d2; sepY -= dy / d2; sepZ -= dz / d2; // away from neighbour, weighted 1/d^2
                            aliX += _state.Vx[j]; aliY += _state.Vy[j]; aliZ += _state.Vz[j];
                            cohX += _state.Px[j]; cohY += _state.Py[j]; cohZ += _state.Pz[j];
                            count++;

                            if (_recordConn && f.Connect && j > i && _connCount < MaxConnections)
                            {
                                if (_connI.Length <= _connCount)
                                {
                                    int cap = _connI.Length == 0 ? 256 : _connI.Length * 2;
                                    Array.Resize(ref _connI, cap);
                                    Array.Resize(ref _connJ, cap);
                                }
                                _connI[_connCount] = i; _connJ[_connCount] = j; _connCount++;
                            }
                        }
                    }

            if (count == 0) return;
            double inv = 1.0 / count;

            // Separation: steer along the averaged away-vector.
            Steer(sepX * inv, sepY * inv, sepZ * inv, vix, viy, viz, maxSpeed, maxForce,
                out double ssx, out double ssy, out double ssz);
            // Alignment: steer toward the average neighbour velocity.
            Steer(aliX * inv, aliY * inv, aliZ * inv, vix, viy, viz, maxSpeed, maxForce,
                out double sax, out double say, out double saz);
            // Cohesion: seek the average neighbour position.
            Steer(cohX * inv - pix, cohY * inv - piy, cohZ * inv - piz, vix, viy, viz, maxSpeed, maxForce,
                out double scx, out double scy, out double scz);

            ax += f.SeparationValue * ssx + f.AlignValue * sax + f.CohesionValue * scx;
            ay += f.SeparationValue * ssy + f.AlignValue * say + f.CohesionValue * scy;
            az += f.SeparationValue * ssz + f.AlignValue * saz + f.CohesionValue * scz;
        }

        /// <summary>Attraction/Repulsion toward/away from targets within a threshold (spec 6.4, VERIFY P2/P3):
        /// magnitude min(Value·(1 − d/threshold), Max); zero outside the threshold.</summary>
        private void AddForce(int i, ForceSpec force, ref double ax, ref double ay, ref double az)
        {
            double[] targets = force.Targets;
            int nt = targets.Length / 3;
            if (nt == 0) return;

            double pix = _state.Px[i], piy = _state.Py[i], piz = _state.Pz[i];
            double sign = force.Repel ? -1.0 : 1.0;

            for (int t = 0; t < nt; t++)
            {
                double dx = targets[t * 3] - pix, dy = targets[t * 3 + 1] - piy, dz = targets[t * 3 + 2] - piz;
                double d2 = dx * dx + dy * dy + dz * dz;
                if (d2 <= 0) continue;

                double threshold = ThresholdFor(force.Thresholds, t);
                if (d2 >= threshold * threshold) continue;

                double d = Math.Sqrt(d2);
                double mag = force.Value * (1.0 - d / threshold);
                if (mag > force.Max) mag = force.Max;
                if (mag <= 0) continue;

                double s = sign * mag / d;
                ax += dx * s; ay += dy * s; az += dz * s;
            }
        }

        private static double ThresholdFor(double[] thresholds, int index)
        {
            if (thresholds == null || thresholds.Length == 0) return 10.0; // spec fallback
            if (thresholds.Length == 1) return thresholds[0];               // one value applies to all
            return thresholds[index < thresholds.Length ? index : thresholds.Length - 1];
        }

        /// <summary>Stigmergy (spec 7.3): steer toward other creepers' trail points within Cohesion Range
        /// (weight CM) and away from those within Separation Range (weight SM), inside the view cone.</summary>
        private void AddStigmergy(int i, StigmergySpec s, double maxSpeed, double maxForce,
            ref double ax, ref double ay, ref double az)
        {
            if (_tpCount == 0) return;

            double pix = _state.Px[i], piy = _state.Py[i], piz = _state.Pz[i];
            double vix = _state.Vx[i], viy = _state.Vy[i], viz = _state.Vz[i];

            double cr2 = s.CohesionRange * s.CohesionRange;
            double sr2 = s.SeparationRange * s.SeparationRange;
            double max2 = Math.Max(cr2, sr2);
            double cosHalf = Math.Cos(s.ViewAngle * 0.5 * Math.PI / 180.0);
            double viLen = Math.Sqrt(vix * vix + viy * viy + viz * viz);
            bool hasVel = viLen > 1e-12;

            double cohX = 0, cohY = 0, cohZ = 0; int cohN = 0;
            double sepX = 0, sepY = 0, sepZ = 0; int sepN = 0;

            _trailGrid.CellCoords(pix, piy, piz, out int cx, out int cy, out int cz);
            int zlo = _dimension == 0 ? cz : cz - 1;
            int zhi = _dimension == 0 ? cz : cz + 1;

            for (int gz = zlo; gz <= zhi; gz++)
                for (int gy = cy - 1; gy <= cy + 1; gy++)
                    for (int gx = cx - 1; gx <= cx + 1; gx++)
                    {
                        int c = _trailGrid.CellIndexOf(gx, gy, gz);
                        if (c < 0) continue;
                        for (int k = _trailGrid.CellStart[c]; k < _trailGrid.CellStart[c + 1]; k++)
                        {
                            int tp = _trailGrid.Items[k];
                            if (_tpOwner[tp] == i) continue; // ignore own trail

                            double dx = _tpx[tp] - pix, dy = _tpy[tp] - piy, dz = _tpz[tp] - piz;
                            double d2 = dx * dx + dy * dy + dz * dz;
                            if (d2 >= max2 || d2 <= 0) continue;

                            if (hasVel)
                            {
                                double d = Math.Sqrt(d2);
                                double dot = vix * dx + viy * dy + viz * dz;
                                if (dot < cosHalf * viLen * d) continue;
                            }

                            if (d2 < cr2) { cohX += _tpx[tp]; cohY += _tpy[tp]; cohZ += _tpz[tp]; cohN++; }
                            if (d2 < sr2) { sepX += _tpx[tp]; sepY += _tpy[tp]; sepZ += _tpz[tp]; sepN++; }
                        }
                    }

            if (cohN > 0)
            {
                Steer(cohX / cohN - pix, cohY / cohN - piy, cohZ / cohN - piz, vix, viy, viz, maxSpeed, maxForce,
                    out double sx, out double sy, out double sz);
                ax += s.CohesionMag * sx; ay += s.CohesionMag * sy; az += s.CohesionMag * sz;
            }
            if (sepN > 0)
            {
                Steer(pix - sepX / sepN, piy - sepY / sepN, piz - sepZ / sepN, vix, viy, viz, maxSpeed, maxForce,
                    out double sx, out double sy, out double sz);
                ax += s.SeparationMag * sx; ay += s.SeparationMag * sy; az += s.SeparationMag * sz;
            }
        }

        /// <summary>Multi Path Tracking (spec 7.4): steer toward the nearest path within Polyline Threshold
        /// when outside its corridor (Polyline Radius), seeking a point ahead along the path.</summary>
        private void AddPathTracking(int i, PathTrackingSpec p, double maxSpeed, double maxForce,
            ref double ax, ref double ay, ref double az)
        {
            if (p.Polylines.Length == 0) return;

            double pix = _state.Px[i], piy = _state.Py[i], piz = _state.Pz[i];
            double vix = _state.Vx[i], viy = _state.Vy[i], viz = _state.Vz[i];

            double hx = vix, hy = viy, hz = viz;
            if (!VectorMath.TryNormalize(ref hx, ref hy, ref hz)) { hx = 1; hy = 0; hz = 0; }
            double pfx = pix + hx * p.ProjectionDistance;
            double pfy = piy + hy * p.ProjectionDistance;
            double pfz = piz + hz * p.ProjectionDistance;

            double bestDist = double.MaxValue;
            double bcx = 0, bcy = 0, bcz = 0, bdx = 0, bdy = 0, bdz = 0;
            for (int path = 0; path < p.Polylines.Length; path++)
            {
                if (ClosestOnPath(p.Polylines[path], pfx, pfy, pfz,
                        out double cx, out double cy, out double cz,
                        out double ddx, out double ddy, out double ddz, out double dist) && dist < bestDist)
                {
                    bestDist = dist; bcx = cx; bcy = cy; bcz = cz; bdx = ddx; bdy = ddy; bdz = ddz;
                }
            }

            if (bestDist > p.Threshold) return; // no path in range
            if (bestDist <= p.Radius) return;    // already inside the corridor

            double tx = bcx + bdx * p.ProjectionDistance - pix;
            double ty = bcy + bdy * p.ProjectionDistance - piy;
            double tz = bcz + bdz * p.ProjectionDistance - piz;
            Steer(tx, ty, tz, vix, viy, viz, maxSpeed, maxForce, out double sx, out double sy, out double sz);
            ax += sx; ay += sy; az += sz;
        }

        /// <summary>Closest point on a flat x,y,z polyline to (px,py,pz), with the local unit direction and distance.</summary>
        private static bool ClosestOnPath(double[] pts, double px, double py, double pz,
            out double cx, out double cy, out double cz, out double dx, out double dy, out double dz, out double dist)
        {
            cx = cy = cz = 0; dx = 1; dy = 0; dz = 0; dist = 0;
            int n = pts.Length / 3;
            if (n == 0) { dist = double.MaxValue; return false; }
            if (n == 1)
            {
                cx = pts[0]; cy = pts[1]; cz = pts[2];
                dist = Math.Sqrt(VectorMath.DistanceSquared(px, py, pz, cx, cy, cz));
                return true;
            }

            double bestD2 = double.MaxValue;
            for (int s = 0; s < n - 1; s++)
            {
                double ax = pts[s * 3], ay = pts[s * 3 + 1], az = pts[s * 3 + 2];
                double ex = pts[(s + 1) * 3] - ax, ey = pts[(s + 1) * 3 + 1] - ay, ez = pts[(s + 1) * 3 + 2] - az;
                double len2 = ex * ex + ey * ey + ez * ez;
                double t = len2 > 0 ? ((px - ax) * ex + (py - ay) * ey + (pz - az) * ez) / len2 : 0;
                if (t < 0) t = 0; else if (t > 1) t = 1;
                double qx = ax + ex * t, qy = ay + ey * t, qz = az + ez * t;
                double d2 = VectorMath.DistanceSquared(px, py, pz, qx, qy, qz);
                if (d2 < bestD2)
                {
                    bestD2 = d2; cx = qx; cy = qy; cz = qz;
                    dx = ex; dy = ey; dz = ez; VectorMath.TryNormalize(ref dx, ref dy, ref dz);
                }
            }
            dist = Math.Sqrt(bestD2);
            return true;
        }

        /// <summary>Mesh Crawl (spec 7.5): seek the closest mesh point to the look-ahead position when the
        /// agent is further than Mesh Threshold from the surface. Snap + velocity flatten happen in Step.</summary>
        private void AddMeshCrawl(int i, MeshCrawlSpec m, double maxSpeed, double maxForce,
            ref double ax, ref double ay, ref double az)
        {
            if (m.Surface == null) return;

            double pix = _state.Px[i], piy = _state.Py[i], piz = _state.Pz[i];
            double vix = _state.Vx[i], viy = _state.Vy[i], viz = _state.Vz[i];

            double hx = vix, hy = viy, hz = viz;
            if (!VectorMath.TryNormalize(ref hx, ref hy, ref hz)) { hx = 1; hy = 0; hz = 0; }
            double ppx = pix + hx * m.MeshProjectionDistance;
            double ppy = piy + hy * m.MeshProjectionDistance;
            double ppz = piz + hz * m.MeshProjectionDistance;

            if (!m.Surface.Closest(ppx, ppy, ppz, 0, out double cx, out double cy, out double cz,
                    out double nx, out double ny, out double nz))
                return;

            double dist = Math.Sqrt(VectorMath.DistanceSquared(ppx, ppy, ppz, cx, cy, cz));
            if (dist <= m.MeshThreshold) return;

            Steer(cx - pix, cy - piy, cz - piz, vix, viy, viz, maxSpeed, maxForce,
                out double sx, out double sy, out double sz);
            ax += m.Multiplier * sx; ay += m.Multiplier * sy; az += m.Multiplier * sz;
        }

        /// <summary>Reynolds steer: setLength(target, MaxSpeed) − velocity, clamped to Max Force (spec 7.2).</summary>
        private static void Steer(double tx, double ty, double tz, double vx, double vy, double vz,
            double maxSpeed, double maxForce, out double sx, out double sy, out double sz)
        {
            VectorMath.SetLength(ref tx, ref ty, ref tz, maxSpeed);
            sx = tx - vx; sy = ty - vy; sz = tz - vz;
            VectorMath.ClampLength(ref sx, ref sy, ref sz, maxForce);
        }

        // ---- boundary (spec 7.6) ----

        private void ApplyBoundary(int i, ref double px, ref double py, ref double pz,
            ref double vx, ref double vy, ref double vz)
        {
            switch (_boundary)
            {
                case 0: // Bounce — reflect position and velocity (exact for multi-width steps).
                    px = Fold(px, _x0, _x1, ref vx);
                    py = Fold(py, _y0, _y1, ref vy);
                    if (_dimension != 0) pz = Fold(pz, _z0, _z1, ref vz);
                    break;

                case 1: // Respawn — if it left the box, re-place it. VERIFY(P8): exact respawn rule.
                    if (Outside(px, py, pz))
                    {
                        RandomPointInBox(out px, out py, out pz);
                        InitialVelocity(out vx, out vy, out vz);
                        _trails.Clear(i); // no line drawn across the domain
                    }
                    break;

                default: // 2 = Do Nothing
                    break;
            }
        }

        /// <summary>Closed-form triangle-wave fold of <paramref name="p"/> into [a,b]; flips velocity on an
        /// odd number of reflections. Exact for steps that cross several box widths.</summary>
        private static double Fold(double p, double a, double b, ref double v)
        {
            double w = b - a;
            if (w <= 0.0) { v = 0.0; return a; }

            double t = p - a;
            double q = Math.Floor(t / w);
            double r = t - q * w;            // r in [0, w)
            bool odd = (((long)q) & 1L) != 0; // parity of the width index

            if (odd)
            {
                v = -v;
                return a + (w - r);
            }
            return a + r;
        }

        private bool Outside(double px, double py, double pz)
        {
            if (px < _x0 || px > _x1 || py < _y0 || py > _y1) return true;
            if (_dimension != 0 && (pz < _z0 || pz > _z1)) return true;
            return false;
        }

        // ---- spawn (spec 7.1, P12) ----

        private void Spawn(SpawnSettings sp, MoveSettings move)
        {
            int n;
            if (sp.Mode == SpawnMode.Points)
                n = sp.Points.Length / 3;
            else
                n = Math.Max(0, sp.Count);

            _state.Reset(n);

            for (int i = 0; i < n; i++)
            {
                double px, py, pz;
                if (sp.Mode == SpawnMode.Points)
                {
                    // Each point keeps its own z; in 2D that spawn z is then held (velocity z = 0).
                    px = sp.Points[i * 3];
                    py = sp.Points[i * 3 + 1];
                    pz = sp.Points[i * 3 + 2];
                }
                else if (sp.SpawnLocation == 0)
                {
                    GridPoint(i, n, out px, out py, out pz); // VERIFY(P12): exact grid layout
                }
                else
                {
                    RandomPointInBox(out px, out py, out pz); // VERIFY(P12): interim 1 = random
                }

                _state.Px[i] = px; _state.Py[i] = py; _state.Pz[i] = pz;

                InitialVelocity(out double vx, out double vy, out double vz);
                _state.Vx[i] = vx; _state.Vy[i] = vy; _state.Vz[i] = vz;
            }
        }

        /// <summary>Initial velocity from Move Settings. Vector → that vector; scalar → a random direction
        /// of that length (VERIFY(P1)). Clamped to Max Speed. 2D keeps Z at 0.</summary>
        private void InitialVelocity(out double vx, out double vy, out double vz)
        {
            if (_move!.InitialIsVector)
            {
                vx = _move.Ivx; vy = _move.Ivy; vz = _dimension == 0 ? 0.0 : _move.Ivz;
            }
            else
            {
                double speed = _move.InitialSpeed;
                if (_dimension == 0)
                {
                    double ang = _rng.NextDouble(0.0, 2.0 * Math.PI);
                    vx = Math.Cos(ang) * speed; vy = Math.Sin(ang) * speed; vz = 0.0;
                }
                else
                {
                    // Uniform direction on the unit sphere.
                    double z = _rng.NextDouble(-1.0, 1.0);
                    double a = _rng.NextDouble(0.0, 2.0 * Math.PI);
                    double r = Math.Sqrt(Math.Max(0.0, 1.0 - z * z));
                    vx = Math.Cos(a) * r * speed; vy = Math.Sin(a) * r * speed; vz = z * speed;
                }
            }
            VectorMath.ClampLength(ref vx, ref vy, ref vz, _move.MaxSpeed);
        }

        private void RandomPointInBox(out double px, out double py, out double pz)
        {
            px = _rng.NextDouble(_x0, _x1);
            py = _rng.NextDouble(_y0, _y1);
            pz = _dimension == 0 ? _z0 : _rng.NextDouble(_z0, _z1);
        }

        private void GridPoint(int i, int n, out double px, out double py, out double pz)
        {
            // Regular grid fill: cells across the box (base rectangle in 2D), placed at cell centres.
            if (_dimension == 0)
            {
                int cols = (int)Math.Ceiling(Math.Sqrt(n));
                if (cols < 1) cols = 1;
                int rows = (int)Math.Ceiling(n / (double)cols);
                int cx = i % cols, cy = i / cols;
                px = CellCentre(_x0, _x1, cx, cols);
                py = CellCentre(_y0, _y1, cy, rows);
                pz = _z0;
            }
            else
            {
                int side = (int)Math.Ceiling(Math.Pow(n, 1.0 / 3.0)); // Math.Cbrt is not in netstandard2.0
                if (side < 1) side = 1;
                int cx = i % side;
                int cy = (i / side) % side;
                int cz = i / (side * side);
                px = CellCentre(_x0, _x1, cx, side);
                py = CellCentre(_y0, _y1, cy, side);
                pz = CellCentre(_z0, _z1, cz, side);
            }
        }

        private static double CellCentre(double a, double b, int cell, int cells)
        {
            if (cells <= 1) return (a + b) * 0.5;
            double w = (b - a) / cells;
            return a + w * (cell + 0.5);
        }

        private void SortBounds(SpawnSettings sp)
        {
            _x0 = Math.Min(sp.BoxMin[0], sp.BoxMax[0]); _x1 = Math.Max(sp.BoxMin[0], sp.BoxMax[0]);
            _y0 = Math.Min(sp.BoxMin[1], sp.BoxMax[1]); _y1 = Math.Max(sp.BoxMin[1], sp.BoxMax[1]);
            _z0 = Math.Min(sp.BoxMin[2], sp.BoxMax[2]); _z1 = Math.Max(sp.BoxMin[2], sp.BoxMax[2]);
        }
    }
}
