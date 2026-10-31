using System;
using System.Globalization;
using System.IO;
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
    /// enabled it (a flag file), this moves ONLY our tab just after the "Rhino" tab at session start
    /// and restores the order if anything goes wrong — other plugins' tabs are preserved. Windows
    /// only (the ribbon is a WinForms control); does nothing on macOS.
    ///
    /// The move is retried on a short timer through the startup window, because Grasshopper populates
    /// the ribbon and other plugins' tabs incrementally — a single move at CanvasCreated is too early
    /// or gets overwritten. This is a placement convenience, not a load fix: the 20 components work
    /// regardless. Toggle with scripts/particle-engine-pin-tab.py / ...-unpin-tab.py.
    /// </summary>
    public sealed class RibbonPlacementPriority : GH_AssemblyPriority
    {
        public override GH_LoadingInstruction PriorityLoad()
        {
            try
            {
                RibbonPlacement.Log("PriorityLoad; windows=" +
                    RuntimeInformation.IsOSPlatform(OSPlatform.Windows));
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
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
            try
            {
                bool enabled = RibbonPlacement.IsEnabled();
                RibbonPlacement.Log("CanvasCreated; enabled=" + enabled);
                if (!enabled) return;

                // CanvasCreated is raised on the UI thread, but the editor window handle may not exist
                // yet (so BeginInvoke would throw). A WinForms Timer needs no handle; start it directly
                // and let its ticks wait for the ribbon to come up.
                RibbonPlacement.StartPinTimer();
            }
            catch (Exception ex)
            {
                RibbonPlacement.Log("OnCanvasCreated error: " + ex.Message);
            }
        }
    }

    internal static class RibbonPlacement
    {
        private const string Us = "Particle Engine";
        private const string Anchor = "Rhino";

        private const int IntervalMs = 500;
        private const int MaxAttempts = 20;   // ~10s startup window
        private const int StableTicksToStop = 3;

        private static Timer? _timer;
        private static int _attempts;
        private static int _stable;

        private static string Dir =>
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "ParticleEngine");

        /// <summary>%APPDATA%\ParticleEngine\pin-tab.flag — present means the user opted in.</summary>
        internal static string FlagPath => Path.Combine(Dir, "pin-tab.flag");

        private static string LogPath => Path.Combine(Dir, "pin-log.txt");

        internal static bool IsEnabled()
        {
            try { return File.Exists(FlagPath); }
            catch { return false; }
        }

        internal static void Log(string message)
        {
            try
            {
                if (!Directory.Exists(Dir)) Directory.CreateDirectory(Dir);
                File.AppendAllText(LogPath,
                    DateTime.Now.ToString("HH:mm:ss.fff", CultureInfo.InvariantCulture) + "  " + message + Environment.NewLine);
            }
            catch
            {
            }
        }

        internal static void StartPinTimer()
        {
            try
            {
                _attempts = 0;
                _stable = 0;
                if (_timer == null)
                {
                    _timer = new Timer { Interval = IntervalMs };
                    _timer.Tick += OnTick;
                }
                _timer.Start();
                Log("Pin timer started");
            }
            catch (Exception ex)
            {
                Log("StartPinTimer error: " + ex.Message);
            }
        }

        private static void OnTick(object? sender, EventArgs e)
        {
            _attempts++;
            int result = TryPinOnce(); // 1 = already at target, 0 = moved there, -1 = tab not found / failed

            if (result == 1) _stable++;
            else _stable = 0;

            if (_stable >= StableTicksToStop || _attempts >= MaxAttempts)
            {
                _timer?.Stop();
                Log("Pin timer stopped after " + _attempts + " attempts (stable=" + _stable + ")");
            }
        }

        /// <summary>Move our tab just after the Rhino tab. Returns 1 if already there, 0 if moved, -1 otherwise.</summary>
        private static int TryPinOnce()
        {
            try
            {
                var editor = global::Grasshopper.Instances.DocumentEditor;
                if (editor == null) return -1;

                GH_Ribbon? ribbon = FindRibbon(editor);
                if (ribbon == null) return -1;

                GH_RibbonTab? tab = FindTab(ribbon, Us);
                if (tab == null) { Log("tick " + _attempts + ": PE tab not in ribbon yet (" + ribbon.Tabs.Count + " tabs)"); return -1; }

                int dest = DesiredIndex(ribbon);
                int current = ribbon.Tabs.IndexOf(tab);
                if (current == dest) return 1; // already pinned

                int countBefore = ribbon.Tabs.Count;
                ribbon.SuspendLayout();
                try
                {
                    ribbon.Tabs.Remove(tab);
                    dest = DesiredIndex(ribbon); // recompute after removal
                    if (dest > ribbon.Tabs.Count) dest = ribbon.Tabs.Count;
                    ribbon.Tabs.Insert(dest, tab);
                    if (ribbon.Tabs.Count != countBefore)
                        throw new InvalidOperationException("ribbon tab count changed");
                    ribbon.LayoutRibbon();
                    ribbon.ActiveTabIndex = dest;
                }
                catch
                {
                    if (!ribbon.Tabs.Contains(tab))
                        ribbon.Tabs.Insert(Math.Min(Math.Max(current, 0), ribbon.Tabs.Count), tab);
                    ribbon.LayoutRibbon();
                    Log("tick " + _attempts + ": move failed, restored");
                    return -1;
                }
                finally
                {
                    ribbon.ResumeLayout(true);
                }

                ribbon.Refresh();
                Log("tick " + _attempts + ": moved PE to index " + dest + " of " + ribbon.Tabs.Count);
                return 0;
            }
            catch (Exception ex)
            {
                Log("tick " + _attempts + " error: " + ex.Message);
                return -1;
            }
        }

        private static int DesiredIndex(GH_Ribbon ribbon)
        {
            for (int i = 0; i < ribbon.Tabs.Count; i++)
            {
                if (string.Equals(Convert.ToString(ribbon.Tabs[i].NameFull), Anchor, StringComparison.Ordinal))
                    return Math.Min(i + 1, ribbon.Tabs.Count);
            }
            return Math.Min(1, ribbon.Tabs.Count);
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
