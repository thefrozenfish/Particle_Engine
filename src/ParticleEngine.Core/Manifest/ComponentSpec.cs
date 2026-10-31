using System;
using System.Collections.Generic;

namespace ParticleEngine.Core.Manifest
{
    /// <summary>
    /// The full definition of one component's identity and ports. Immutable, with no Grasshopper
    /// dependency, so both the Grasshopper components (which read it to register their ports) and the
    /// conformance test (spec 11.1, runs without Rhino) use the same source of truth.
    /// </summary>
    public sealed class ComponentSpec
    {
        public ComponentSpec(
            string key,
            string name,
            string nick,
            string description,
            string subcategory,
            Guid guid,
            IReadOnlyList<PortSpec> inputs,
            IReadOnlyList<PortSpec> outputs,
            bool variableInputs = false,
            bool inCulebra = true)
        {
            Key = key;
            Name = name;
            Nick = nick;
            Description = description;
            Subcategory = subcategory;
            Guid = guid;
            Inputs = inputs ?? Array.Empty<PortSpec>();
            Outputs = outputs ?? Array.Empty<PortSpec>();
            VariableInputs = variableInputs;
            InCulebra = inCulebra;
        }

        /// <summary>Stable short key used to look this spec up from the component class.</summary>
        public string Key { get; }

        public string Name { get; }
        public string Nick { get; }
        public string Description { get; }
        public string Subcategory { get; }
        public Guid Guid { get; }
        public IReadOnlyList<PortSpec> Inputs { get; }
        public IReadOnlyList<PortSpec> Outputs { get; }

        /// <summary>True for the Controller: inputs are added/removed at run time (IGH_VariableParameterComponent).</summary>
        public bool VariableInputs { get; }

        /// <summary>False for the one new component (Bundling_ZombieEngine) that Culebra has no equivalent of.</summary>
        public bool InCulebra { get; }
    }
}
