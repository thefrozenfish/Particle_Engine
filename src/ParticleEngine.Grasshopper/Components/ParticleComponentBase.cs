using System;
using System.Globalization;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Parameters;
using Grasshopper.Kernel.Types;
using ParticleEngine.Core.Manifest;

namespace ParticleEngine.Grasshopper.Components
{
    /// <summary>
    /// Base for every Particle Engine component. It registers its ports from the shared
    /// <see cref="ComponentSpec"/> in Core, so port names/nicknames/order/types/defaults cannot
    /// drift from the manifest the conformance test checks (spec 11.1).
    ///
    /// M1 is a scaffold: <see cref="SolveInstance"/> does nothing yet. Real behaviour arrives in
    /// later milestones.
    /// </summary>
    public abstract class ParticleComponentBase : GH_Component
    {
        protected ParticleComponentBase(ComponentSpec spec)
            : base(spec.Name, spec.Nick, spec.Description, ParticleEngineBranding.Category, spec.Subcategory)
        {
            // IMPORTANT: GH_Component's constructor has ALREADY called RegisterInputParams /
            // RegisterOutputParams (via PostConstructor) by the time this body runs. So nothing the
            // base needs for registration may depend on a field set here — Spec is resolved from the
            // runtime type instead (valid even mid-construction).
        }

        // Resolved from the concrete type name (e.g. BoxSpawnComponent -> "BoxSpawn"), so it is
        // available while the base constructor registers ports, before any derived field is set.
        protected ComponentSpec Spec
        {
            get
            {
                string n = GetType().Name;
                const string suffix = "Component";
                string key = n.EndsWith(suffix, StringComparison.Ordinal)
                    ? n.Substring(0, n.Length - suffix.Length)
                    : n;
                return ComponentCatalog.ByKey(key);
            }
        }

        public override Guid ComponentGuid => Spec.Guid;

        public override GH_Exposure Exposure => GH_Exposure.primary;

        protected override System.Drawing.Bitmap Icon => ParticleEngineBranding.ComponentIconFor(Spec.Key);

        protected override void RegisterInputParams(GH_InputParamManager pManager)
        {
            foreach (var port in Spec.Inputs)
            {
                IGH_Param param = CreateParam(port);
                // Generic inputs carry opaque settings (or are reserved, like Color Data); we default
                // or validate them in code, so mark them optional — an unconnected one must not block
                // the whole component. Concrete/geometry inputs stay required (or hold a default value).
                if (port.Kind == PortKind.Generic) param.Optional = true;
                pManager.AddParameter(param);
            }
        }

        protected override void RegisterOutputParams(GH_OutputParamManager pManager)
        {
            foreach (var port in Spec.Outputs)
                pManager.AddParameter(CreateParam(port));
        }

        // M1 scaffold: no simulation logic yet. M2+ components override this.
        protected override void SolveInstance(IGH_DataAccess DA)
        {
        }

        /// <summary>Add a warning (exposed so shared solver helpers can report on behalf of a component).</summary>
        internal void Warn(string message) => AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, message);

        /// <summary>
        /// Build a fully-configured Grasshopper parameter from a <see cref="PortSpec"/>. Persistent
        /// defaults are written exactly once here (spec rule 7 / R3), only for concrete scalar inputs.
        /// </summary>
        internal static IGH_Param CreateParam(PortSpec port)
        {
            IGH_Param param = NewParam(port.Kind);
            param.Name = port.Name;
            param.NickName = port.Nick;
            param.Description = port.Tooltip;
            param.Access = MapAccess(port.Access);

            if (port.Default != null)
                ApplyDefault(param, port.Kind, port.Default);

            return param;
        }

        private static IGH_Param NewParam(PortKind kind)
        {
            switch (kind)
            {
                case PortKind.Generic: return new Param_GenericObject();
                case PortKind.Boolean: return new Param_Boolean();
                case PortKind.Integer: return new Param_Integer();
                case PortKind.Number: return new Param_Number();
                case PortKind.Point: return new Param_Point();
                case PortKind.Box: return new Param_Box();
                case PortKind.Curve: return new Param_Curve();
                case PortKind.Mesh: return new Param_Mesh();
                case PortKind.Circle: return new Param_Circle();
                default: throw new ArgumentOutOfRangeException(nameof(kind), kind, null);
            }
        }

        private static GH_ParamAccess MapAccess(PortAccess access)
        {
            switch (access)
            {
                case PortAccess.Item: return GH_ParamAccess.item;
                case PortAccess.List: return GH_ParamAccess.list;
                case PortAccess.Tree: return GH_ParamAccess.tree;
                default: throw new ArgumentOutOfRangeException(nameof(access), access, null);
            }
        }

        private static void ApplyDefault(IGH_Param param, PortKind kind, string value)
        {
            // SetPersistentData writes a single stored value; never called more than once per input.
            switch (kind)
            {
                case PortKind.Boolean:
                    ((Param_Boolean)param).SetPersistentData(new GH_Boolean(bool.Parse(value)));
                    break;
                case PortKind.Integer:
                    ((Param_Integer)param).SetPersistentData(new GH_Integer(int.Parse(value, CultureInfo.InvariantCulture)));
                    break;
                case PortKind.Number:
                    ((Param_Number)param).SetPersistentData(new GH_Number(double.Parse(value, CultureInfo.InvariantCulture)));
                    break;
                // Generic / geometry ports carry no persistent default.
            }
        }
    }
}
