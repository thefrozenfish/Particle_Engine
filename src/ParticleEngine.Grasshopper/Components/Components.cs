using ParticleEngine.Core.Manifest;

namespace ParticleEngine.Grasshopper.Components
{
    // M1 port-parity scaffold: each component only identifies its ComponentSpec; the base class
    // registers the ports and (for now) does nothing in SolveInstance. One class per component so
    // Grasshopper can instantiate each by its own parameterless constructor and GUID.
    // The Controller is the one exception (variable inputs) and lives in ControllerComponent.cs.

    // ----- 01 | Spawn Types -----
    public sealed class BoxSpawnComponent : ParticleComponentBase
    {
        public BoxSpawnComponent() : base(ComponentCatalog.ByKey("BoxSpawn")) { }
    }

    public sealed class PointSpawnComponent : ParticleComponentBase
    {
        public PointSpawnComponent() : base(ComponentCatalog.ByKey("PointSpawn")) { }
    }

    // ----- 02 | Initialize -----
    public sealed class InitSettingsComponent : ParticleComponentBase
    {
        public InitSettingsComponent() : base(ComponentCatalog.ByKey("InitSettings")) { }
    }

    public sealed class MoveSettingsComponent : ParticleComponentBase
    {
        public MoveSettingsComponent() : base(ComponentCatalog.ByKey("MoveSettings")) { }
    }

    // ----- 03 | Behaviors -----
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

    // ----- 04 | Forces -----
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

    // ----- 05 | Engine -----
    public sealed class CreeperEngineComponent : ParticleComponentBase
    {
        public CreeperEngineComponent() : base(ComponentCatalog.ByKey("CreeperEngine")) { }
    }

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

    // ----- 06 | Display -----
    public sealed class TrailDataComponent : ParticleComponentBase
    {
        public TrailDataComponent() : base(ComponentCatalog.ByKey("TrailData")) { }
    }

    public sealed class VisualSettingsComponent : ParticleComponentBase
    {
        public VisualSettingsComponent() : base(ComponentCatalog.ByKey("VisualSettings")) { }
    }
}
