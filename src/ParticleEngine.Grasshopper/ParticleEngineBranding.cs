using System;
using System.Drawing;
using Grasshopper.Kernel;

namespace ParticleEngine.Grasshopper
{
    internal static class ParticleEngineBranding
    {
        internal const string Category = "Particle Engine";
        private static readonly Lazy<Bitmap> Tab = new Lazy<Bitmap>(() => Load("ParticleEngine16.png"));
        private static readonly Lazy<Bitmap> Component = new Lazy<Bitmap>(() => Load("ParticleEngine24.png"));

        internal static Bitmap TabIcon => Tab.Value;
        internal static Bitmap ComponentIcon => Component.Value;

        private static Bitmap Load(string name)
        {
            using (var stream = typeof(ParticleEngineBranding).Assembly.GetManifestResourceStream(
                "ParticleEngine.Grasshopper.Resources." + name))
            {
                if (stream == null)
                    throw new InvalidOperationException("Missing Particle Engine icon: " + name);
                using (var source = new Bitmap(stream))
                    return new Bitmap(source); // Detach from the disposed resource stream.
            }
        }
    }

    /// <summary>Register branding before Grasshopper builds the component ribbon.</summary>
    public sealed class ParticleEnginePriority : GH_AssemblyPriority
    {
        public override GH_LoadingInstruction PriorityLoad()
        {
            var server = global::Grasshopper.Instances.ComponentServer;
            server.AddCategoryIcon(ParticleEngineBranding.Category, ParticleEngineBranding.TabIcon);
            server.AddCategoryShortName(ParticleEngineBranding.Category, "PE");
            server.AddCategorySymbolName(ParticleEngineBranding.Category, 'P');
            return GH_LoadingInstruction.Proceed;
        }
    }
}
