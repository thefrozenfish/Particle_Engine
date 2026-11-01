using ParticleEngine.Core.Manifest;

namespace ParticleEngine.Grasshopper.Components
{
    // Components still at the M1 port-parity scaffold (no SolveInstance logic yet). They gain behaviour
    // in later milestones: behaviours/forces in M3-M4, the Zombie/Bundling engines in M5-M6.
    // The M2 components (spawn, init, move, trail, visual, Controller, Creeper_Engine) have real logic
    // and live in their own files.

    // 03 | Behaviors all have real behaviour now (BehaviorComponents.cs, TrackingComponents.cs).

    // ----- 04 | Forces (Attraction/Repulsion have real behaviour in ForceComponents.cs; Bundling is M6) -----
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
