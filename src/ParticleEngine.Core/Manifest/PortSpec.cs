using System;

namespace ParticleEngine.Core.Manifest
{
    /// <summary>
    /// The Grasshopper parameter kinds Particle Engine uses. The string from
    /// <see cref="PortKindExtensions.DumpTypeName"/> is the Grasshopper TypeName, which is
    /// what the Culebra port dump records and what the conformance test compares against.
    /// </summary>
    public enum PortKind
    {
        Generic, // Param_GenericObject, TypeName "Generic Data" — carries the opaque settings Goo.
        Boolean,
        Integer,
        Number,
        Point,
        Box,
        Curve,
        Mesh,
        Circle,
    }

    public enum PortAccess
    {
        Item,
        List,
        Tree,
    }

    public enum PortDirection
    {
        Input,
        Output,
    }

    public static class PortKindExtensions
    {
        /// <summary>The Grasshopper parameter TypeName this kind maps to (matches the port dump).</summary>
        public static string DumpTypeName(this PortKind kind)
        {
            switch (kind)
            {
                case PortKind.Generic: return "Generic Data";
                case PortKind.Boolean: return "Boolean";
                case PortKind.Integer: return "Integer";
                case PortKind.Number: return "Number";
                case PortKind.Point: return "Point";
                case PortKind.Box: return "Box";
                case PortKind.Curve: return "Curve";
                case PortKind.Mesh: return "Mesh";
                case PortKind.Circle: return "Circle";
                default: throw new ArgumentOutOfRangeException(nameof(kind), kind, null);
            }
        }
    }

    /// <summary>
    /// One input or output port. Immutable. <see cref="Default"/> is the persistent default as a
    /// string (exactly as the Culebra dump records it) for the few concrete-typed scalar inputs that
    /// have one; null for geometry, generic and list ports. <see cref="Tooltip"/> is original
    /// Particle Engine text (clean-room — not copied from Culebra).
    /// </summary>
    public sealed class PortSpec
    {
        public PortSpec(string name, string nick, PortKind kind, PortAccess access, string? @default, string tooltip)
        {
            Name = name ?? throw new ArgumentNullException(nameof(name));
            Nick = nick ?? throw new ArgumentNullException(nameof(nick));
            Kind = kind;
            Access = access;
            Default = @default;
            Tooltip = tooltip ?? string.Empty;
        }

        public string Name { get; }
        public string Nick { get; }
        public PortKind Kind { get; }
        public PortAccess Access { get; }
        public string? Default { get; }
        public string Tooltip { get; }
    }
}
