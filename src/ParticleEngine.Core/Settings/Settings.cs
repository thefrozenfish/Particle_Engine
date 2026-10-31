using System;
using System.Collections.Generic;
using System.Globalization;

namespace ParticleEngine.Core.Settings
{
    /// <summary>
    /// Marker for the immutable settings objects that cross the Grasshopper/Core boundary as Goo
    /// (spec 4.2). Each provides a descriptive type name and a readable ToString for the panel.
    /// M1 defines the data contracts and their fields (the component ports); the engines populate
    /// and consume them from M2 on.
    /// </summary>
    public interface IParticleSettings
    {
        string SettingsTypeName { get; }
    }

    public enum SpawnMode
    {
        Box,
        Points,
    }

    /// <summary>Spawn description produced by Box Spawn / Point Spawn. Geometry is reduced to plain
    /// doubles in the Grasshopper layer so Core stays Rhino-free.</summary>
    public sealed class SpawnSettings : IParticleSettings
    {
        public SpawnSettings(SpawnMode mode, double[] boxMin, double[] boxMax, int spawnLocation, int count, double[] points)
        {
            Mode = mode;
            BoxMin = boxMin ?? new double[3];
            BoxMax = boxMax ?? new double[3];
            SpawnLocation = spawnLocation;
            Count = count;
            Points = points ?? Array.Empty<double>(); // flat x,y,z triples
        }

        public SpawnMode Mode { get; }
        public double[] BoxMin { get; }
        public double[] BoxMax { get; }
        public int SpawnLocation { get; }
        public int Count { get; }
        public double[] Points { get; }

        public string SettingsTypeName => "Spawn Settings";
        public override string ToString() =>
            Mode == SpawnMode.Box ? $"Spawn Settings (Box, {Count})" : $"Spawn Settings ({Points.Length / 3} points)";
    }

    /// <summary>Init Settings: spawn + dimension + boundary mode.</summary>
    public sealed class InitSettings : IParticleSettings
    {
        public InitSettings(SpawnSettings spawn, int dimension, int boundary)
        {
            Spawn = spawn;
            Dimension = dimension;
            Boundary = boundary;
        }

        public SpawnSettings Spawn { get; }
        public int Dimension { get; } // 0 = 2D, 1 = 3D
        public int Boundary { get; }  // 0 = Bounce, 1 = Respawn, 2 = Do Nothing

        public string SettingsTypeName => "Init Settings";
        public override string ToString() => $"Init Settings (dim {Dimension}, boundary {Boundary})";
    }

    /// <summary>Move Settings: motion limits. Initial speed is either a scalar or a vector.</summary>
    public sealed class MoveSettings : IParticleSettings
    {
        public MoveSettings(bool initialIsVector, double initialSpeed, double ivx, double ivy, double ivz,
            double maxSpeed, double maxForce, double velocityMultiplier)
        {
            InitialIsVector = initialIsVector;
            InitialSpeed = initialSpeed;
            Ivx = ivx; Ivy = ivy; Ivz = ivz;
            MaxSpeed = maxSpeed;
            MaxForce = maxForce;
            VelocityMultiplier = velocityMultiplier;
        }

        public bool InitialIsVector { get; }
        public double InitialSpeed { get; }
        public double Ivx { get; }
        public double Ivy { get; }
        public double Ivz { get; }
        public double MaxSpeed { get; }
        public double MaxForce { get; }
        public double VelocityMultiplier { get; }

        public string SettingsTypeName => "Move Settings";
        public override string ToString() => $"Move Settings (maxSpeed {MaxSpeed.ToString(CultureInfo.InvariantCulture)})";
    }

    /// <summary>Base for the behaviour and force specs that stack in the Controller.</summary>
    public abstract class BehaviorSpec : IParticleSettings
    {
        public abstract string SettingsTypeName { get; }
        public override string ToString() => SettingsTypeName;
    }

    public sealed class FlockingSpec : BehaviorSpec
    {
        public FlockingSpec(bool connect, double viewAngle, double searchRadius,
            double alignValue, double separationValue, double cohesionValue)
        {
            Connect = connect;
            ViewAngle = viewAngle;
            SearchRadius = searchRadius;
            AlignValue = alignValue;
            SeparationValue = separationValue;
            CohesionValue = cohesionValue;
        }

        public bool Connect { get; }
        public double ViewAngle { get; }
        public double SearchRadius { get; }
        public double AlignValue { get; }
        public double SeparationValue { get; }
        public double CohesionValue { get; }

        public override string SettingsTypeName => "Flocking Behavior";
    }

    public sealed class StigmergySpec : BehaviorSpec
    {
        public StigmergySpec(double viewAngle, double cohesionMag, double cohesionRange,
            double separationMag, double separationRange)
        {
            ViewAngle = viewAngle;
            CohesionMag = cohesionMag;
            CohesionRange = cohesionRange;
            SeparationMag = separationMag;
            SeparationRange = separationRange;
        }

        public double ViewAngle { get; }
        public double CohesionMag { get; }
        public double CohesionRange { get; }
        public double SeparationMag { get; }
        public double SeparationRange { get; }

        public override string SettingsTypeName => "Stigmergy Behavior";
    }

    /// <summary>Multi Path Tracking (I and II). Polylines are flat x,y,z triples per path.</summary>
    public sealed class PathTrackingSpec : BehaviorSpec
    {
        public PathTrackingSpec(double[][] polylines, double threshold, double projectionDistance,
            double radius, bool triggerSpawn, int maxChildren)
        {
            Polylines = polylines ?? Array.Empty<double[]>();
            Threshold = threshold;
            ProjectionDistance = projectionDistance;
            Radius = radius;
            TriggerSpawn = triggerSpawn;
            MaxChildren = maxChildren;
        }

        public double[][] Polylines { get; }
        public double Threshold { get; }
        public double ProjectionDistance { get; }
        public double Radius { get; }
        public bool TriggerSpawn { get; }
        public int MaxChildren { get; }

        public override string SettingsTypeName => "Tracking Behavior";
    }

    /// <summary>Mesh Crawl (I and II). The mesh itself is supplied to the engine via IMeshSurface (M4);
    /// this spec carries the scalar parameters only.</summary>
    public sealed class MeshCrawlSpec : BehaviorSpec
    {
        public MeshCrawlSpec(double meshThreshold, double meshProjectionDistance, double multiplier,
            bool triggerSpawn, int maxChildren)
        {
            MeshThreshold = meshThreshold;
            MeshProjectionDistance = meshProjectionDistance;
            Multiplier = multiplier;
            TriggerSpawn = triggerSpawn;
            MaxChildren = maxChildren;
        }

        public double MeshThreshold { get; }
        public double MeshProjectionDistance { get; }
        public double Multiplier { get; }
        public bool TriggerSpawn { get; }
        public int MaxChildren { get; }

        public override string SettingsTypeName => "Mesh Crawling Behavior";
    }

    /// <summary>Attraction or repulsion to a set of targets. Targets are flat x,y,z triples.</summary>
    public sealed class ForceSpec : BehaviorSpec
    {
        public ForceSpec(bool repel, double[] targets, double[] thresholds, double value, double max)
        {
            Repel = repel;
            Targets = targets ?? Array.Empty<double>();
            Thresholds = thresholds ?? Array.Empty<double>();
            Value = value;
            Max = max;
        }

        public bool Repel { get; }
        public double[] Targets { get; }
        public double[] Thresholds { get; }
        public double Value { get; }
        public double Max { get; }

        public override string SettingsTypeName => Repel ? "Repulsion Force" : "Attraction Force";
    }

    public sealed class BundlingSpec : BehaviorSpec
    {
        public BundlingSpec(double threshold, double ratio, bool rebuild, int pointCount, int weldCount)
        {
            Threshold = threshold;
            Ratio = ratio;
            Rebuild = rebuild;
            PointCount = pointCount;
            WeldCount = weldCount;
        }

        public double Threshold { get; }
        public double Ratio { get; }
        public bool Rebuild { get; }
        public int PointCount { get; }
        public int WeldCount { get; }

        public override string SettingsTypeName => "Bundling Behavior";
    }

    /// <summary>The ordered behaviour/force stack produced by the Controller.</summary>
    public sealed class BehaviorData : IParticleSettings
    {
        public BehaviorData(IReadOnlyList<BehaviorSpec> stack)
        {
            Stack = stack ?? Array.Empty<BehaviorSpec>();
        }

        public IReadOnlyList<BehaviorSpec> Stack { get; }

        public string SettingsTypeName => "Behavior Data";
        public override string ToString() => $"Behavior Data ({Stack.Count} in stack)";
    }

    public sealed class TrailDataSettings : IParticleSettings
    {
        public TrailDataSettings(bool trail, int trailStep, int maxTrailSize)
        {
            Trail = trail;
            TrailStep = trailStep;
            MaxTrailSize = maxTrailSize;
        }

        public bool Trail { get; }
        public int TrailStep { get; }
        public int MaxTrailSize { get; }

        public string SettingsTypeName => "Trail Data";
        public override string ToString() => $"Trail Data (step {TrailStep}, max {MaxTrailSize})";
    }

    public sealed class VisualSettings : IParticleSettings
    {
        public VisualSettings(TrailDataSettings? trail, int displayMode)
        {
            Trail = trail;
            DisplayMode = displayMode;
        }

        public TrailDataSettings? Trail { get; }
        public int DisplayMode { get; } // 0 = Graphic, 1 = Geometry

        public string SettingsTypeName => "Visual Settings";
        public override string ToString() => DisplayMode == 1 ? "Visual Settings (Geometry)" : "Visual Settings (Graphic)";
    }
}
