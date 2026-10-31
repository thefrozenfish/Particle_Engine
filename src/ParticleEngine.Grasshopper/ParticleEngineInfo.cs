using System;
using Grasshopper.Kernel;

namespace ParticleEngine.Grasshopper
{
    /// <summary>
    /// Assembly metadata Grasshopper reads when it loads the .gha. No components are
    /// registered in M0; this only makes the plugin a recognised, loadable library so
    /// host load can be verified before any component exists.
    /// </summary>
    public sealed class ParticleEngineInfo : GH_AssemblyInfo
    {
        public override string Name => "Particle Engine";

        // Icons are added in the final milestone (spec 2.5); the base Icon returns null meanwhile.

        public override string Description =>
            "Agent and particle simulation for Grasshopper. A clean-room reimplementation " +
            "of a Culebra-style workflow subset.";

        public override Guid Id => new Guid("3f2a6c10-7b4e-4d2a-9c1f-0a5b6e2d41a0");

        public override string AuthorName => "Abhinav Chaudhary";

        public override string AuthorContact => "";

        public override string AssemblyVersion => "0.4.0.0";
    }
}
