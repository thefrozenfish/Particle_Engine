namespace ParticleEngine.Core
{
    /// <summary>
    /// Closest-point query on a mesh surface, used by Mesh Crawl (spec 2.3, 7.5). Implemented in the
    /// Grasshopper layer with <c>Mesh.ClosestMeshPoint</c>, so Core stays free of RhinoCommon.
    /// </summary>
    public interface IMeshSurface
    {
        /// <summary>
        /// Closest point on the surface to (x,y,z) with its unit normal. Returns false (and leaves the
        /// outputs undefined) when nothing is found within <paramref name="maxDistance"/>; pass a
        /// non-positive maxDistance for an unbounded search.
        /// </summary>
        bool Closest(double x, double y, double z, double maxDistance,
            out double cx, out double cy, out double cz,
            out double nx, out double ny, out double nz);
    }
}
