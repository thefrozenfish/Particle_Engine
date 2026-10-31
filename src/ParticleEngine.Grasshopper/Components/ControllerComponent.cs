using Grasshopper.Kernel;
using Grasshopper.Kernel.Parameters;
using ParticleEngine.Core.Manifest;

namespace ParticleEngine.Grasshopper.Components
{
    /// <summary>
    /// Behaviour-merging Controller. Culebra exposes no fixed inputs; users add and reorder behaviour
    /// inputs via the zoomable +/- UI (spec 6.3). Starts with zero inputs to match the dump; each
    /// added input is a generic port that accepts any behaviour, force or Move Settings Goo.
    /// </summary>
    public sealed class ControllerComponent : ParticleComponentBase, IGH_VariableParameterComponent
    {
        public ControllerComponent() : base(ComponentCatalog.ByKey("Controller")) { }

        // Inputs may be inserted/removed on the input side only.
        public bool CanInsertParameter(GH_ParameterSide side, int index) => side == GH_ParameterSide.Input;

        public bool CanRemoveParameter(GH_ParameterSide side, int index) => side == GH_ParameterSide.Input;

        public IGH_Param CreateParameter(GH_ParameterSide side, int index)
        {
            return new Param_GenericObject
            {
                Access = GH_ParamAccess.list,
                Optional = true,
                MutableNickName = true,
            };
        }

        public bool DestroyParameter(GH_ParameterSide side, int index) => true;

        public void VariableParameterMaintenance()
        {
            for (int i = 0; i < Params.Input.Count; i++)
            {
                IGH_Param p = Params.Input[i];
                p.Name = $"Behavior {i + 1}";
                if (string.IsNullOrEmpty(p.NickName) || p.NickName.StartsWith("B"))
                    p.NickName = $"B{i + 1}";
                p.Description = "A behaviour, force or Move Settings to add to the execution stack.";
                p.Access = GH_ParamAccess.list;
                p.Optional = true;
            }
        }
    }
}
