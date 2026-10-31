using System;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using Grasshopper.GUI.Canvas;
using Grasshopper.GUI.Ribbon;
using Grasshopper.Kernel;

namespace ParticleEngine.Grasshopper
{
    /// <summary>
    /// OPT-IN, OFF BY DEFAULT. On hosts with heavy ribbon overflow (many plugins), the Particle
    /// Engine tab can be pushed off the visible strip even though it loaded fine. When the user has
    /// enabled it (persistent Grasshopper setting), this moves ONLY our tab just after the "Rhino"
    /// tab on each session, and restores the order if anything goes wrong — other plugins' tabs are
    /// preserved. Windows only (the ribbon is a WinForms control); does nothing on macOS.
    ///
    /// This is a placement convenience, not a load fix: the plugin and its 20 components work
    /// regardless. Toggle it with scripts/particle-engine-pin-tab.py / ...-unpin-tab.py.
    /// </summary>
    public sealed class RibbonPlacementPriority : GH_AssemblyPriority
    {
        internal const string EnabledKey = "ParticleEngine.PinTab.Enabled";

        public override GH_LoadingInstruction PriorityLoad()
        {
            try
            {
                if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                    return GH_LoadingInstruction.Proceed;
                if (!global::Grasshopper.Instances.Settings.GetValue(EnabledKey, false))
                    return GH_LoadingInstruction.Proceed;

                global::Grasshopper.Instances.CanvasCreated += OnCanvasCreated;
            }
            catch
            {
                // A convenience must never block the plugin from loading.
            }
            return GH_LoadingInstruction.Proceed;
        }

        private static void OnCanvasCreated(GH_Canvas canvas)
        {
            var editor = global::Grasshopper.Instances.DocumentEditor;
            if (editor == null) return;
            // Defer until the ribbon has finished its initial layout.
            try { editor.BeginInvoke((Action)(() => RibbonPlacement.TryPinOurTab())); }
            catch { }
        }
    }

    internal static class RibbonPlacement
    {
        private const string Us = "Particle Engine";
        private const string Anchor = "Rhino";

        /// <summary>Move our tab just after the Rhino tab, preserving every other tab. No-op on failure.</summary>
        internal static void TryPinOurTab()
        {
            try
            {
                var editor = global::Grasshopper.Instances.DocumentEditor;
                if (editor == null) return;

                GH_Ribbon? ribbon = FindRibbon(editor);
                if (ribbon == null) return;

                GH_RibbonTab? tab = FindTab(ribbon, Us);
                if (tab == null) return;

                int oldIndex = ribbon.Tabs.IndexOf(tab);
                int countBefore = ribbon.Tabs.Count;

                ribbon.SuspendLayout();
                try
                {
                    ribbon.Tabs.Remove(tab);

                    int dest = 1;
                    for (int i = 0; i < ribbon.Tabs.Count; i++)
                    {
                        if (string.Equals(Convert.ToString(ribbon.Tabs[i].NameFull), Anchor, StringComparison.Ordinal))
                        {
                            dest = i + 1;
                            break;
                        }
                    }
                    if (dest > ribbon.Tabs.Count) dest = ribbon.Tabs.Count;

                    ribbon.Tabs.Insert(dest, tab);
                    if (ribbon.Tabs.Count != countBefore)
                        throw new InvalidOperationException("ribbon tab count changed");

                    ribbon.LayoutRibbon();
                    ribbon.ActiveTabIndex = dest;
                }
                catch
                {
                    // Put our tab back where it was; never leave the ribbon in a half-moved state.
                    if (!ribbon.Tabs.Contains(tab))
                        ribbon.Tabs.Insert(Math.Min(Math.Max(oldIndex, 0), ribbon.Tabs.Count), tab);
                    ribbon.LayoutRibbon();
                }
                finally
                {
                    ribbon.ResumeLayout(true);
                }

                ribbon.Refresh();
            }
            catch
            {
                // Optional convenience: swallow anything rather than disrupt the host.
            }
        }

        private static GH_Ribbon? FindRibbon(Control control)
        {
            if (control is GH_Ribbon ribbon) return ribbon;
            foreach (Control child in control.Controls)
            {
                GH_Ribbon? found = FindRibbon(child);
                if (found != null) return found;
            }
            return null;
        }

        private static GH_RibbonTab? FindTab(GH_Ribbon ribbon, string nameFull)
        {
            foreach (GH_RibbonTab tab in ribbon.Tabs)
            {
                if (string.Equals(Convert.ToString(tab.NameFull), nameFull, StringComparison.Ordinal))
                    return tab;
            }
            return null;
        }
    }
}
