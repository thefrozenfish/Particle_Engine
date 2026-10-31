using Grasshopper.Kernel.Types;
using ParticleEngine.Core.Settings;

namespace ParticleEngine.Grasshopper.Goo
{
    /// <summary>
    /// Grasshopper Goo wrapper for Particle Engine's immutable settings objects (spec 4.2). One
    /// generic wrapper suffices because the settings ports are generic parameters. Because the wrapped
    /// value is immutable, <see cref="Duplicate"/> returns this instance — safe for Grasshopper to
    /// share, cache and copy, and lets a running engine pick up a changed upstream object by reference.
    /// </summary>
    public sealed class ParticleGoo<T> : GH_Goo<T> where T : class, IParticleSettings
    {
        public ParticleGoo() { }

        public ParticleGoo(T value) { Value = value; }

        public override bool IsValid => Value != null;

        public override string TypeName => Value?.SettingsTypeName ?? "Particle Settings";

        public override string TypeDescription => "Particle Engine " + TypeName;

        public override IGH_Goo Duplicate() => this; // immutable

        public override string ToString() => Value?.ToString() ?? "Null " + TypeName;
    }
}
