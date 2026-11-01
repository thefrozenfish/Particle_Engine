using ParticleEngine.Core;
using Rhino.Geometry;

namespace ParticleEngine.Grasshopper
{
    /// <summary>
    /// Grasshopper-side <see cref="IMeshSurface"/> for Mesh Crawl: closest-point + normal via
    /// <c>Mesh.ClosestMeshPoint</c>, so the Core simulation can query a mesh without referencing RhinoCommon.
    /// </summary>
    internal sealed class MeshSurface : IMeshSurface
    {
        private readonly Mesh _mesh;

        internal MeshSurface(Mesh mesh) => _mesh = mesh;

        public bool Closest(double x, double y, double z, double maxDistance,
            out double cx, out double cy, out double cz, out double nx, out double ny, out double nz)
        {
            cx = cy = cz = 0; nx = 0; ny = 0; nz = 1;

            MeshPoint mp = _mesh.ClosestMeshPoint(new Point3d(x, y, z), maxDistance > 0 ? maxDistance : 0.0);
            if (mp == null) return false;

            Point3d c = mp.Point;
            cx = c.X; cy = c.Y; cz = c.Z;

            Vector3d n = _mesh.NormalAt(mp);
            if (n.IsValid && n.Unitize()) { nx = n.X; ny = n.Y; nz = n.Z; }
            return true;
        }
    }
}
