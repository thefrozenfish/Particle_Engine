using System;
using System.Collections.Generic;
using System.Drawing;
using Grasshopper.Kernel;

namespace ParticleEngine.Grasshopper
{
    internal static class ParticleEngineBranding
    {
        internal const string Category = "Particle Engine";
        private static readonly Lazy<Bitmap> Tab = new Lazy<Bitmap>(() => Load("ParticleEngine16.png"));
        private static readonly Lazy<Bitmap> Component = new Lazy<Bitmap>(() => Load("ParticleEngine24.png"));

        // Per-component icons, cached by ComponentSpec.Key. Missing icon -> shared fallback.
        private static readonly Dictionary<string, Bitmap> IconCache = new Dictionary<string, Bitmap>();

        internal static Bitmap TabIcon => Tab.Value;
        internal static Bitmap ComponentIcon => Component.Value;

        /// <summary>The icon for one component key (e.g. "Flocking"), or the shared Particle Engine icon
        /// if that component has no dedicated bitmap embedded.</summary>
        internal static Bitmap ComponentIconFor(string key)
        {
            lock (IconCache)
            {
                if (IconCache.TryGetValue(key, out Bitmap cached)) return cached;
                Bitmap icon = TryLoad("icons." + key + ".png") ?? Component.Value;
                IconCache[key] = icon;
                return icon;
            }
        }

        private static Bitmap Load(string name)
        {
            return TryLoad(name) ?? throw new InvalidOperationException("Missing Particle Engine icon: " + name);
        }

        private static Bitmap? TryLoad(string name)
        {
            using (var stream = typeof(ParticleEngineBranding).Assembly.GetManifestResourceStream(
                "ParticleEngine.Grasshopper.Resources." + name))
            {
                if (stream == null) return null;
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
