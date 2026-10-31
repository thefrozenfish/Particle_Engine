using Grasshopper.Kernel;

namespace ParticleEngine.Grasshopper
{
    /// <summary>
    /// Runs early in Grasshopper's load sequence (before the ribbon is built) to register the
    /// "Particle Engine" category, so its tab reliably appears in the ribbon. Without this, a
    /// Rhino 8 (.NET) plugin's components can register — and be findable by name — while the ribbon
    /// tab is missing. A short symbol labels the tab until real icons arrive (M8).
    /// </summary>
    public sealed class ParticleEnginePriority : GH_AssemblyPriority
    {
        public override GH_LoadingInstruction PriorityLoad()
        {
            global::Grasshopper.Instances.ComponentServer.AddCategorySymbolName("Particle Engine", 'P');
            return GH_LoadingInstruction.Proceed;
        }
    }
}
