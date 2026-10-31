using System.Drawing;
using System.Drawing.Drawing2D;
using Grasshopper.Kernel;

namespace ParticleEngine.Grasshopper
{
    /// <summary>
    /// Runs early in Grasshopper's load sequence (before the ribbon is built) to register the
    /// "Particle Engine" category with a tab symbol and icon, so its ribbon tab appears and is easy
    /// to spot. Without this, a Rhino 8 (.NET) plugin's components register and are findable by name,
    /// but the ribbon shows only a bare single-letter tab. Full per-component icons come in M8; this
    /// is just the category tab icon.
    /// </summary>
    public sealed class ParticleEnginePriority : GH_AssemblyPriority
    {
        public override GH_LoadingInstruction PriorityLoad()
        {
            var server = global::Grasshopper.Instances.ComponentServer;
            server.AddCategorySymbolName("Particle Engine", 'P');
            server.AddCategoryIcon("Particle Engine", CreateCategoryIcon());
            return GH_LoadingInstruction.Proceed;
        }

        // A simple, distinctive 24x24 tab icon: a dark disc with a few "particles" and a short trail.
        // Basic GDI only, so it also works on Rhino 8 Mac's System.Drawing shim.
        private static Bitmap CreateCategoryIcon()
        {
            var bmp = new Bitmap(24, 24);
            using (var g = Graphics.FromImage(bmp))
            {
                g.SmoothingMode = SmoothingMode.AntiAlias;
                g.Clear(Color.Transparent);

                using (var disc = new SolidBrush(Color.FromArgb(255, 54, 58, 68)))
                    g.FillEllipse(disc, 1, 1, 22, 22);

                using (var trail = new Pen(Color.FromArgb(160, 120, 200, 255), 1.6f))
                    g.DrawLine(trail, 6, 17, 16, 7);

                using (var dot = new SolidBrush(Color.White))
                {
                    g.FillEllipse(dot, 14, 5, 5, 5);   // head
                    g.FillEllipse(dot, 10, 9, 3, 3);
                    g.FillEllipse(dot, 7, 13, 3, 3);
                    g.FillEllipse(dot, 5, 16, 2, 2);   // tail
                }
            }
            return bmp;
        }
    }
}
