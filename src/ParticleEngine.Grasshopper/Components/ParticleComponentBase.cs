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
            : base(spec.Name, spec.Nick, spec.Description, "Particle Engine", spec.Subcategory)
        {
            Spec = spec;
        }

        protected ComponentSpec Spec { get; }

        public override Guid ComponentGuid => Spec.Guid;

        public override GH_Exposure Exposure => GH_Exposure.primary;

        protected override void RegisterInputParams(GH_InputParamManager pManager)
        {
            foreach (var port in Spec.Inputs)
                pManager.AddParameter(CreateParam(port));
        }

        protected override void RegisterOutputParams(GH_OutputParamManager pManager)
        {
            foreach (var port in Spec.Outputs)
                pManager.AddParameter(CreateParam(port));
        }

        // M1 scaffold: no simulation logic yet.
        protected override void SolveInstance(IGH_DataAccess DA)
        {
        }

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
