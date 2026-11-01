using Grasshopper.Kernel;
using Grasshopper.Kernel.Types;
using ParticleEngine.Core.Manifest;
using ParticleEngine.Core.Settings;
using ParticleEngine.Grasshopper.Goo;

namespace ParticleEngine.Grasshopper.Components
{
    /// <summary>Init Settings — spawn + dimension + boundary for an engine.</summary>
    public sealed class InitSettingsComponent : ParticleComponentBase
    {
        public InitSettingsComponent() : base(ComponentCatalog.ByKey("InitSettings")) { }

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            // Spawn Settings is a list-access input in Culebra; read it as a list and use the first.
            SpawnSettings? spawn = GhInput.GetSettingsFromList<SpawnSettings>(DA, 0);
            if (spawn == null)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "Spawn Settings is required (connect Box or Point Spawn).");
                return;
            }

            int dimension = 1; // 0 = 2D, 1 = 3D
            int boundary = 0;  // 0 = Bounce, 1 = Respawn, 2 = Do Nothing
            GhInput.GetInt(DA, 1, ref dimension);
            GhInput.GetInt(DA, 2, ref boundary);

            if (dimension != 0 && dimension != 1)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "Dimension must be 0 (2D) or 1 (3D); using 3D.");
                dimension = 1;
            }
            if (boundary < 0 || boundary > 2)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "Boundary must be 0 (Bounce), 1 (Respawn) or 2 (Do Nothing); using Bounce.");
                boundary = 0;
            }

            DA.SetData(0, new ParticleGoo<InitSettings>(new InitSettings(spawn, dimension, boundary)));
        }
    }

    /// <summary>Move Settings — motion limits (feeds the engine directly, not the Controller).</summary>
    public sealed class MoveSettingsComponent : ParticleComponentBase
    {
        public MoveSettingsComponent() : base(ComponentCatalog.ByKey("MoveSettings")) { }

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            // Initial Speed||Vector: a vector, or a scalar speed (random direction, VERIFY P1).
            bool isVector = false;
            double speed = 1.0;
            Rhino.Geometry.Vector3d vec = Rhino.Geometry.Vector3d.Zero;
            GhInput.GetVectorOrNumber(DA, 0, out isVector, out vec, out speed);

            double maxSpeed = 5.0, maxForce = 0.5, vm = 1.0;
            GhInput.GetDouble(DA, 1, ref maxSpeed);
            GhInput.GetDouble(DA, 2, ref maxForce);
            GhInput.GetDouble(DA, 3, ref vm);

            var move = new MoveSettings(isVector, speed, vec.X, vec.Y, vec.Z, maxSpeed, maxForce, vm);
            DA.SetData(0, new ParticleGoo<MoveSettings>(move));
        }
    }

    /// <summary>Trail Data — trail visibility, step and capacity for Visual Settings.</summary>
    public sealed class TrailDataComponent : ParticleComponentBase
    {
        public TrailDataComponent() : base(ComponentCatalog.ByKey("TrailData")) { }

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            bool trail = false;
            int step = 0, maxSize = 5000;
            GhInput.GetBool(DA, 0, ref trail);
            GhInput.GetInt(DA, 1, ref step);
            GhInput.GetInt(DA, 2, ref maxSize);

            if (step < 1)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Remark, "Trail Step clamped to 1.");
                step = 1;
            }
            if (maxSize < 2)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Remark, "Max Trail Size clamped to 2.");
                maxSize = 2;
            }

            DA.SetData(0, new ParticleGoo<TrailDataSettings>(new TrailDataSettings(trail, step, maxSize)));
        }
    }

    /// <summary>Visual Settings — Graphic/Geometry mode + trail data (colour/texture reserved, ignored).</summary>
    public sealed class VisualSettingsComponent : ParticleComponentBase
    {
        public VisualSettingsComponent() : base(ComponentCatalog.ByKey("VisualSettings")) { }

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            TrailDataSettings? trail = GhInput.GetSettings<TrailDataSettings>(DA, 0);

            IGH_Goo? colour = null;
            if (DA.GetData(1, ref colour) && colour != null)
                AddRuntimeMessage(GH_RuntimeMessageLevel.Remark, "Color Data is not supported and is ignored.");

            bool applyTexture = false;
            GhInput.GetBool(DA, 2, ref applyTexture); // reserved, ignored

            int displayMode = 0; // 0 = Graphic, 1 = Geometry
            GhInput.GetInt(DA, 3, ref displayMode);
            if (displayMode != 0 && displayMode != 1)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "Display Mode must be 0 (Graphic) or 1 (Geometry); using Graphic.");
                displayMode = 0;
            }

            DA.SetData(0, new ParticleGoo<VisualSettings>(new VisualSettings(trail, displayMode)));
        }
    }
}
