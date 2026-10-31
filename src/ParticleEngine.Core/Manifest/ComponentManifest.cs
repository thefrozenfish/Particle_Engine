using System.Collections.Generic;

namespace ParticleEngine.Core.Manifest
{
    /// <summary>
    /// The component manifest the conformance test reads (spec 11.1). It is produced from the same
    /// static <see cref="ComponentCatalog"/> the Grasshopper components register their ports from, so
    /// the two cannot drift, and it needs no Rhino or Grasshopper to generate.
    /// </summary>
    public static class ComponentManifest
    {
        public static IReadOnlyList<ComponentSpec> Generate() => ComponentCatalog.All;
    }
}
