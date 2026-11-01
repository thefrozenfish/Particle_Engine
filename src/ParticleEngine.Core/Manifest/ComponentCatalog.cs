using System;
using System.Collections.Generic;

namespace ParticleEngine.Core.Manifest
{
    /// <summary>
    /// The single source of truth for every Particle Engine component's identity and ports.
    /// Port names, nicknames, order and types reproduce the owner's installed Culebra (captured by
    /// the port dump, spec 12.1) for wiring parity. Deviations from that dump are deliberate and
    /// marked DEVIATION below; they are also recorded in the spec change log (BUILD_SPEC §17):
    ///
    ///   - Creeper engines: Init Settings and Move Settings are item access here, not Culebra's list.
    ///     One engine drives one simulation; multi-value guarding is handled by the DA.Iteration rule
    ///     (spec R4), not by accepting lists of settings.
    ///   - Bundling_Engine nickname is "BE", not Culebra's placeholder "Nickname".
    ///   - Bundling_ZombieEngine is new (no Culebra equivalent); it mirrors Creeper_ZombieEngine.
    ///
    /// GUIDs are new to this rebuild (spec rule 5); none of Culebra's are reused.
    /// All defaults are the real values from the dump. Tooltips are original text (clean-room).
    /// </summary>
    public static class ComponentCatalog
    {
        // Subcategories, exact names and order (spec 5, confirmed by the dump).
        public const string SubSpawn = "01 | Spawn Types";
        public const string SubInit = "02 | Initialize";
        public const string SubBehaviors = "03 | Behaviors";
        public const string SubForces = "04 | Forces";
        public const string SubEngine = "05 | Engine";
        public const string SubDisplay = "06 | Display";

        private static PortSpec In(string name, string nick, PortKind kind, PortAccess access, string? def, string tip)
            => new PortSpec(name, nick, kind, access, def, tip);

        private static PortSpec Out(string name, string nick, PortKind kind, PortAccess access, string tip)
            => new PortSpec(name, nick, kind, access, null, tip);

        private static Guid G(string s) => new Guid(s);

        private static readonly ComponentSpec[] _all =
        {
            // ---------------- 01 | Spawn Types ----------------
            new ComponentSpec(
                "BoxSpawn", "Box Spawn", "BS",
                "Spawns creepers inside a box region, in 2D or 3D.",
                SubSpawn, G("a7c3e100-0000-4a00-8b00-000000000001"),
                new[]
                {
                    In("Box", "B", PortKind.Box, PortAccess.Item, null, "Box that bounds the spawn region."),
                    In("Spawn Location", "S", PortKind.Integer, PortAccess.Item, "0", "Spawn distribution selector (0 or 1)."),
                    In("Spawn Count", "CC", PortKind.Integer, PortAccess.Item, "500", "Number of creepers to spawn."),
                },
                new[] { Out("Spawn Settings", "SS", PortKind.Generic, PortAccess.List, "Spawn settings for Init Settings.") }),

            new ComponentSpec(
                "PointSpawn", "Point Spawn", "PS",
                "Spawns one creeper at each supplied point, in 2D or 3D.",
                SubSpawn, G("a7c3e100-0000-4a00-8b00-000000000002"),
                new[]
                {
                    In("Points", "P", PortKind.Point, PortAccess.List, null, "Start position for each creeper."),
                    In("Box", "B", PortKind.Box, PortAccess.Item, null, "Bounding box used by the boundary mode."),
                },
                new[] { Out("Spawn Settings", "SS", PortKind.Generic, PortAccess.List, "Spawn settings for Init Settings.") }),

            // ---------------- 02 | Initialize ----------------
            new ComponentSpec(
                "InitSettings", "Init Settings", "IS",
                "Combines spawn settings with dimension and boundary mode for an engine.",
                SubInit, G("a7c3e100-0000-4a00-8b00-000000000003"),
                new[]
                {
                    In("Spawn Settings", "ST", PortKind.Generic, PortAccess.List, null, "Spawn settings from Box/Point Spawn."),
                    In("Dimension", "D", PortKind.Generic, PortAccess.Item, null, "0 = 2D, 1 = 3D."),
                    In("Boundary", "B", PortKind.Generic, PortAccess.Item, null, "0 = Bounce, 1 = Respawn, 2 = Do Nothing."),
                },
                new[] { Out("Init Settings", "IS", PortKind.Generic, PortAccess.List, "Initialization settings for an engine.") }),

            new ComponentSpec(
                "MoveSettings", "Move Settings", "MS",
                "Motion limits for the creepers: speed, force and velocity scaling.",
                SubInit, G("a7c3e100-0000-4a00-8b00-000000000004"),
                new[]
                {
                    In("Initial Speed||Vector", "IS", PortKind.Generic, PortAccess.Item, null, "Initial speed (scalar) or velocity (vector)."),
                    In("Max Speed", "MS", PortKind.Generic, PortAccess.Item, null, "Maximum speed."),
                    In("Max Force", "MF", PortKind.Generic, PortAccess.Item, null, "Maximum steering force."),
                    In("Velocity Multiplier", "VM", PortKind.Generic, PortAccess.Item, null, "Scales the per-step movement."),
                },
                new[] { Out("Move Settings", "MS", PortKind.Generic, PortAccess.List, "Movement settings for the engine.") }),

            // ---------------- 03 | Behaviors ----------------
            new ComponentSpec(
                "Controller", "Controller", "BC",
                "Merges behaviours, forces and move settings into one ordered execution stack.",
                SubBehaviors, G("a7c3e100-0000-4a00-8b00-000000000005"),
                Array.Empty<PortSpec>(), // variable inputs, added at run time
                new[] { Out("Behavior Data Object", "BDO", PortKind.Generic, PortAccess.Item, "The ordered behaviour stack.") },
                variableInputs: true),

            new ComponentSpec(
                "Flocking", "Flocking", "FL",
                "Reynolds flocking: separation, alignment and cohesion.",
                SubBehaviors, G("a7c3e100-0000-4a00-8b00-000000000006"),
                new[]
                {
                    In("Connect", "C", PortKind.Boolean, PortAccess.Item, "False", "Draw connections to neighbours (slow)."),
                    In("View Angle", "VA", PortKind.Number, PortAccess.Item, "180", "Field of view, in degrees."),
                    In("Search Radius", "SR", PortKind.Number, PortAccess.Item, "70", "Neighbour search distance."),
                    In("Align Value", "AV", PortKind.Number, PortAccess.Item, "0.24", "Alignment weight."),
                    In("Separation Value", "SV", PortKind.Number, PortAccess.Item, "0.09", "Separation weight."),
                    In("Cohesion Value", "CV", PortKind.Number, PortAccess.Item, "0.045", "Cohesion weight."),
                },
                new[] { Out("Flocking Behavior", "FB", PortKind.Generic, PortAccess.Item, "Flocking behaviour for the Controller.") }),

            new ComponentSpec(
                "Stigmergy", "Stigmergy", "ST",
                "Trail chasing: creepers steer by other creepers' stored trails.",
                SubBehaviors, G("a7c3e100-0000-4a00-8b00-000000000007"),
                new[]
                {
                    In("View Angle", "VA", PortKind.Number, PortAccess.Item, "60", "Field of view, in degrees."),
                    In("Cohesion Mag", "CM", PortKind.Number, PortAccess.Item, "1.5", "Steer-toward-trail weight."),
                    In("Cohesion Range", "CR", PortKind.Number, PortAccess.Item, "80", "Range enabling cohesion."),
                    In("Separation Mag", "SM", PortKind.Number, PortAccess.Item, "0", "Steer-away-from-trail weight."),
                    In("Separation Range", "SR", PortKind.Number, PortAccess.Item, "5", "Range enabling separation."),
                },
                new[] { Out("Stigmergy Behavior", "SB", PortKind.Generic, PortAccess.Item, "Stigmergy behaviour for the Controller.") }),

            new ComponentSpec(
                "MultiPathTracking", "Multi Path Tracking", "T",
                "Steers creepers to follow the nearest of several polyline paths.",
                SubBehaviors, G("a7c3e100-0000-4a00-8b00-000000000008"),
                new[]
                {
                    In("Polylines", "P", PortKind.Curve, PortAccess.List, null, "Paths to follow."),
                    In("Polyline Threshold", "PT", PortKind.Number, PortAccess.Item, "500", "Distance within which a path is seen."),
                    In("Projection Distance", "PD", PortKind.Number, PortAccess.Item, "50", "Look-ahead distance."),
                    In("Polyline Radius", "PR", PortKind.Number, PortAccess.Item, "15", "Path corridor radius."),
                },
                new[] { Out("Tracking Behavior", "TB", PortKind.Generic, PortAccess.Item, "Path-tracking behaviour for the Controller.") }),

            // Multi Path Tracking II removed per owner (2026-11-01): only the single-path/no-children
            // version is kept; see BUILD_SPEC §17 (reverses interpretation call I-1).

            new ComponentSpec(
                "MeshCrawl", "Mesh Crawl", "MC",
                "Keeps creepers moving along the surface of a mesh.",
                SubBehaviors, G("a7c3e100-0000-4a00-8b00-00000000000a"),
                new[]
                {
                    In("Mesh", "M", PortKind.Mesh, PortAccess.Item, null, "Mesh to crawl on."),
                    In("Mesh Threshold", "MT", PortKind.Number, PortAccess.Item, "1.5", "Distance before steering back to the mesh."),
                    In("Mesh Projection Distance", "MPD", PortKind.Number, PortAccess.Item, "2", "Look-ahead distance before the closest-point lookup."),
                    // VERIFY(P7): exact role of 'Multiplier' is unknown; port kept for parity, default 1.5.
                    In("Multiplier", "M", PortKind.Number, PortAccess.Item, "1.5", "Mesh-crawl steering multiplier."),
                },
                new[] { Out("Mesh Crawling Behavior", "MCB", PortKind.Generic, PortAccess.Item, "Mesh-crawl behaviour for the Controller.") }),

            // Mesh Crawl II removed per owner (2026-11-01): only the no-children version is kept;
            // see BUILD_SPEC §17 (reverses interpretation call I-2).

            // ---------------- 04 | Forces ----------------
            new ComponentSpec(
                "AttractionForce", "Attraction Force", "AF",
                "Attracts creepers toward a set of target points.",
                SubForces, G("a7c3e100-0000-4a00-8b00-00000000000c"),
                new[]
                {
                    In("Targets", "T", PortKind.Point, PortAccess.List, null, "Target points to attract toward."),
                    In("Thresholds", "TH", PortKind.Number, PortAccess.List, null, "Influence radius per target."),
                    In("Attraction Value", "AV", PortKind.Number, PortAccess.Item, "0.97", "Attraction magnitude."),
                    In("Max Attraction", "MA", PortKind.Number, PortAccess.Item, "0.06", "Upper limit on the attraction."),
                },
                new[]
                {
                    Out("Attraction Force", "AB", PortKind.Generic, PortAccess.Item, "Attraction force for the Controller."),
                    Out("Attraction Threshold Viz", "V", PortKind.Circle, PortAccess.List, "Threshold circle per target."),
                }),

            new ComponentSpec(
                "RepulsionForce", "Repulsion Force", "RF",
                "Repels creepers away from a set of target points.",
                SubForces, G("a7c3e100-0000-4a00-8b00-00000000000d"),
                new[]
                {
                    In("Targets", "T", PortKind.Point, PortAccess.List, null, "Target points to repel from."),
                    In("Thresholds", "TH", PortKind.Number, PortAccess.List, null, "Influence radius per target."),
                    In("Repel Value", "AV", PortKind.Number, PortAccess.Item, "1", "Repulsion magnitude."),
                    In("Max Repel", "MA", PortKind.Number, PortAccess.Item, "1.5", "Upper limit on the repulsion."),
                },
                new[]
                {
                    Out("Repulsion Force", "AB", PortKind.Generic, PortAccess.Item, "Repulsion force for the Controller."),
                    Out("Repulsion Threshold Viz", "V", PortKind.Circle, PortAccess.List, "Threshold circle per target."),
                }),

            new ComponentSpec(
                "Bundling", "Bundling", "B",
                "Settings for self-organisation (bundling) of curve networks.",
                SubForces, G("a7c3e100-0000-4a00-8b00-00000000000e"),
                new[]
                {
                    In("Threshold", "T", PortKind.Number, PortAccess.Item, "100", "Self-organisation distance threshold."),
                    In("Ratio", "R", PortKind.Number, PortAccess.Item, "0.5", "Move distance per cycle."),
                    In("Rebuild", "RB", PortKind.Boolean, PortAccess.Item, "True", "Rebuild input curves before bundling."),
                    In("Point Count", "PC", PortKind.Integer, PortAccess.Item, "40", "Point count of the rebuild."),
                    In("Weld Count", "WC", PortKind.Integer, PortAccess.Item, "2", "Points welded at each end."),
                },
                new[] { Out("Bundling Behavior", "BB", PortKind.Generic, PortAccess.Item, "Bundling behaviour for a bundling engine.") }),

            // ---------------- 05 | Engine ----------------
            // DEVIATION (R4): Init Settings / Move Settings are item access here, not Culebra's list.
            new ComponentSpec(
                "CreeperEngine", "Creeper_Engine", "CE",
                "Timer-driven creeper simulation engine. Advances one frame per solution.",
                SubEngine, G("a7c3e100-0000-4a00-8b00-00000000000f"),
                new[]
                {
                    In("Init Settings", "S", PortKind.Generic, PortAccess.Item, null, "Initialization settings (required)."),
                    In("Move Settings", "S", PortKind.Generic, PortAccess.Item, null, "Movement settings."),
                    In("Behavioral Settings", "BS", PortKind.Generic, PortAccess.Item, null, "Behaviour stack from the Controller."),
                    In("Visual Settings", "VS", PortKind.Generic, PortAccess.Item, null, "Visual settings (optional)."),
                    In("Reset", "R", PortKind.Generic, PortAccess.Item, null, "Reset to frame 0."),
                },
                new[]
                {
                    Out("Creepers", "C", PortKind.Point, PortAccess.List, "Head positions."),
                    Out("Trails", "T", PortKind.Generic, PortAccess.Tree, "Trail polylines (Geometry mode)."),
                    Out("Connectivity", "CN", PortKind.Generic, PortAccess.List, "Neighbour connection lines (Geometry mode)."),
                }),

            new ComponentSpec(
                "CreeperZombieEngine", "Creeper_ZombieEngine", "CZE",
                "Runs a creeper simulation for a fixed number of iterations in one go (no timer).",
                SubEngine, G("a7c3e100-0000-4a00-8b00-000000000010"),
                new[]
                {
                    In("Init Settings", "S", PortKind.Generic, PortAccess.Item, null, "Initialization settings (required)."),
                    In("Move Settings", "S", PortKind.Generic, PortAccess.Item, null, "Movement settings."),
                    In("Behavioral Settings", "BS", PortKind.Generic, PortAccess.Item, null, "Behaviour stack from the Controller."),
                    In("Visual Settings", "VS", PortKind.Generic, PortAccess.Item, null, "Visual settings (optional)."),
                    In("Iterations", "I", PortKind.Integer, PortAccess.Item, null, "Number of frames to run."),
                    In("Run", "R", PortKind.Generic, PortAccess.Item, null, "Run-and-reset trigger."),
                },
                new[]
                {
                    Out("Creepers", "C", PortKind.Point, PortAccess.List, "Head positions."),
                    Out("Trails", "T", PortKind.Generic, PortAccess.Tree, "Trail polylines (Geometry mode)."),
                    Out("Connectivity", "CN", PortKind.Generic, PortAccess.List, "Neighbour connection lines (Geometry mode)."),
                }),

            // DEVIATION: nickname is "BE" here, not Culebra's placeholder "Nickname".
            new ComponentSpec(
                "BundlingEngine", "Bundling_Engine", "BE",
                "Timer-driven curve-network bundling engine. One cycle per solution.",
                SubEngine, G("a7c3e100-0000-4a00-8b00-000000000011"),
                new[]
                {
                    In("Curves", "C", PortKind.Curve, PortAccess.List, null, "Curves to bundle."),
                    In("Behavioral Settings", "BS", PortKind.Generic, PortAccess.Item, null, "Must contain a Bundling behaviour."),
                    In("Visual Settings", "VS", PortKind.Generic, PortAccess.Item, null, "Display mode only."),
                    In("Reset", "R", PortKind.Generic, PortAccess.Item, null, "Reset to the input curves."),
                },
                new[] { Out("Geometry", "G", PortKind.Generic, PortAccess.List, "Self-organised curves.") }),

            // NEW: no Culebra equivalent. Mirrors Creeper_ZombieEngine for bundling.
            new ComponentSpec(
                "BundlingZombieEngine", "Bundling_ZombieEngine", "BZE",
                "Runs curve-network bundling for a fixed number of cycles in one go (no timer).",
                SubEngine, G("a7c3e100-0000-4a00-8b00-000000000012"),
                new[]
                {
                    In("Curves", "C", PortKind.Curve, PortAccess.List, null, "Curves to bundle."),
                    In("Behavioral Settings", "BS", PortKind.Generic, PortAccess.Item, null, "Must contain a Bundling behaviour."),
                    In("Visual Settings", "VS", PortKind.Generic, PortAccess.Item, null, "Display mode only."),
                    In("Iterations", "I", PortKind.Integer, PortAccess.Item, null, "Number of cycles to run."),
                    In("Run", "R", PortKind.Generic, PortAccess.Item, null, "Run-and-reset trigger."),
                },
                new[] { Out("Geometry", "G", PortKind.Generic, PortAccess.List, "Self-organised curves.") },
                inCulebra: false),

            // ---------------- 06 | Display ----------------
            new ComponentSpec(
                "TrailData", "Trail Data", "TD",
                "Controls trail storage and visibility for the Visual Settings component.",
                SubDisplay, G("a7c3e100-0000-4a00-8b00-000000000013"),
                new[]
                {
                    In("Trail", "T", PortKind.Generic, PortAccess.Item, null, "Trail visibility toggle."),
                    In("Trail Step", "TS", PortKind.Integer, PortAccess.Item, "0", "Steps between stored trail points."),
                    In("Max Trail Size", "MT", PortKind.Integer, PortAccess.Item, "5000", "Maximum trail points per creeper."),
                },
                new[] { Out("Trail Data", "TD", PortKind.Generic, PortAccess.Item, "Trail data for Visual Settings.") }),

            new ComponentSpec(
                "VisualSettings", "Visual Settings", "VS",
                "Controls the visual output of the engine (Graphic or Geometry mode).",
                SubDisplay, G("a7c3e100-0000-4a00-8b00-000000000014"),
                new[]
                {
                    In("Trail Data", "TD", PortKind.Generic, PortAccess.Item, null, "Trail data from Trail Data."),
                    In("Color Data", "CD", PortKind.Generic, PortAccess.Item, null, "Reserved; colour data is ignored."),
                    In("Apply Texture", "AT", PortKind.Boolean, PortAccess.Item, "False", "Reserved; ignored."),
                    In("Display Mode", "DM", PortKind.Generic, PortAccess.Item, null, "0 = Graphic, 1 = Geometry."),
                },
                new[] { Out("Visual Settings", "VS", PortKind.Generic, PortAccess.Item, "Visual settings for the engine.") }),
        };

        /// <summary>All 20 component specifications, in registration order.</summary>
        public static IReadOnlyList<ComponentSpec> All => _all;

        /// <summary>Look up one spec by its <see cref="ComponentSpec.Key"/>.</summary>
        public static ComponentSpec ByKey(string key)
        {
            foreach (var c in _all)
                if (c.Key == key)
                    return c;
            throw new KeyNotFoundException($"No component spec with key '{key}'.");
        }
    }
}
