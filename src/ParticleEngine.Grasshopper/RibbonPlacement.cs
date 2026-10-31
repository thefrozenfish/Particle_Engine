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
    /// ON BY DEFAULT (Windows). Many Grasshopper installs have more ribbon tabs than fit across the
    /// window, so a newly added tab lands off-screen even though it loaded fine. Each session this
    /// moves ONLY the Particle Engine tab to just after the "Rhino" tab, restoring the order if
    /// anything goes wrong — other plugins' tabs are preserved. macOS is a no-op (the ribbon is a
    /// WinForms control).
    ///
    /// Opt out by creating %APPDATA%\ParticleEngine\pin-tab.disabled
    /// (scripts/particle-engine-unpin-tab.py); re-enable with particle-engine-pin-tab.py. The move is
    /// retried on a short timer because Grasshopper builds the ribbon incrementally after the canvas
    /// is created. This is placement only; the plugin's 20 components work regardless.
    /// </summary>
    public sealed class RibbonPlacementPriority : GH_AssemblyPriority
    {
        private static bool _subscribed;

        public override GH_LoadingInstruction PriorityLoad()
        {
            try
            {
                // Grasshopper may call PriorityLoad more than once; subscribe only once so we never
                // start duplicate timers.
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows) && !_subscribed)
                {
                    global::Grasshopper.Instances.CanvasCreated += OnCanvasCreated;
                    _subscribed = true;
                }
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
                if (RibbonPlacement.PinDisabled()) return; // user opted out
                // CanvasCreated is on the UI thread, but the editor window handle may not exist yet
                // (so BeginInvoke would throw). A WinForms Timer needs no handle; start it directly
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
        private const long MaxLogBytes = 100_000;

        private static Timer? _timer;
        private static int _attempts;
        private static int _stable;
        private static bool _pinnedThisSession;

        private static string Dir =>
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "ParticleEngine");

        /// <summary>%APPDATA%\ParticleEngine\pin-tab.disabled — present means the user opted OUT.</summary>
        internal static string DisableFlagPath => Path.Combine(Dir, "pin-tab.disabled");

        private static string LogPath => Path.Combine(Dir, "pin-log.txt");

        internal static bool PinDisabled()
        {
            try { return File.Exists(DisableFlagPath); }
            catch { return false; }
        }

        internal static void Log(string message)
        {
            try
            {
                if (!Directory.Exists(Dir)) Directory.CreateDirectory(Dir);
                try
                {
                    var info = new FileInfo(LogPath);
                    if (info.Exists && info.Length > MaxLogBytes) File.Delete(LogPath);
                }
                catch { }
                File.AppendAllText(LogPath,
                    DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture) + "  " + message + Environment.NewLine);
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
                _pinnedThisSession = false;
                if (_timer == null)
                {
                    _timer = new Timer { Interval = IntervalMs };
                    _timer.Tick += OnTick;
                }
                _timer.Start();
            }
            catch (Exception ex)
            {
                Log("StartPinTimer error: " + ex.Message);
            }
        }

        private static void OnTick(object? sender, EventArgs e)
        {
            _attempts++;
            int result = TryPinOnce(); // 1 = already at target, 0 = moved there, -1 = not found / failed

            if (result == 0) _pinnedThisSession = true;
            if (result == 1) _stable++;
            else _stable = 0;

            if (_stable >= StableTicksToStop || _attempts >= MaxAttempts)
            {
                _timer?.Stop();
                if (!_pinnedThisSession && _stable == 0)
                    Log("gave up after " + _attempts + " attempts (tab not found or move failed)");
                // Success is logged once in TryPinOnce when the move happens.
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
                if (tab == null) return -1; // ribbon not fully populated yet; the timer retries

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
                    Log("move failed, restored");
                    return -1;
                }
                finally
                {
                    ribbon.ResumeLayout(true);
                }

                ribbon.Refresh();
                Log("pinned Particle Engine to index " + dest + " of " + ribbon.Tabs.Count + " (attempt " + _attempts + ")");
                return 0;
            }
            catch (Exception ex)
            {
                Log("pin error: " + ex.Message);
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
