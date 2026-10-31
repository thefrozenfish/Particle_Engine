using ParticleEngine.Core.Manifest;

namespace ParticleEngine.Grasshopper.Components
{
    // Components still at the M1 port-parity scaffold (no SolveInstance logic yet). They gain behaviour
    // in later milestones: behaviours/forces in M3-M4, the Zombie/Bundling engines in M5-M6.
    // The M2 components (spawn, init, move, trail, visual, Controller, Creeper_Engine) have real logic
    // and live in their own files.

    // ----- 03 | Behaviors (M3-M4) -----
    public sealed class FlockingComponent : ParticleComponentBase
    {
        public FlockingComponent() : base(ComponentCatalog.ByKey("Flocking")) { }
    }

    public sealed class StigmergyComponent : ParticleComponentBase
    {
        public StigmergyComponent() : base(ComponentCatalog.ByKey("Stigmergy")) { }
    }

    public sealed class MultiPathTrackingComponent : ParticleComponentBase
    {
        public MultiPathTrackingComponent() : base(ComponentCatalog.ByKey("MultiPathTracking")) { }
    }

    public sealed class MultiPathTrackingIIComponent : ParticleComponentBase
    {
        public MultiPathTrackingIIComponent() : base(ComponentCatalog.ByKey("MultiPathTrackingII")) { }
    }

    public sealed class MeshCrawlComponent : ParticleComponentBase
    {
        public MeshCrawlComponent() : base(ComponentCatalog.ByKey("MeshCrawl")) { }
    }

    public sealed class MeshCrawlIIComponent : ParticleComponentBase
    {
        public MeshCrawlIIComponent() : base(ComponentCatalog.ByKey("MeshCrawlII")) { }
    }

    // ----- 04 | Forces (M3-M6) -----
    public sealed class AttractionForceComponent : ParticleComponentBase
    {
        public AttractionForceComponent() : base(ComponentCatalog.ByKey("AttractionForce")) { }
    }

    public sealed class RepulsionForceComponent : ParticleComponentBase
    {
        public RepulsionForceComponent() : base(ComponentCatalog.ByKey("RepulsionForce")) { }
    }

    public sealed class BundlingComponent : ParticleComponentBase
    {
        public BundlingComponent() : base(ComponentCatalog.ByKey("Bundling")) { }
    }

    // ----- 05 | Engine (M5-M6) -----
    public sealed class CreeperZombieEngineComponent : ParticleComponentBase
    {
        public CreeperZombieEngineComponent() : base(ComponentCatalog.ByKey("CreeperZombieEngine")) { }
    }

    public sealed class BundlingEngineComponent : ParticleComponentBase
    {
        public BundlingEngineComponent() : base(ComponentCatalog.ByKey("BundlingEngine")) { }
    }

    public sealed class BundlingZombieEngineComponent : ParticleComponentBase
    {
        public BundlingZombieEngineComponent() : base(ComponentCatalog.ByKey("BundlingZombieEngine")) { }
    }
}
