# Particle Engine: ribbon repair and Claude handover

**Updated:** 31 October 2026. **Status:** installed, loaded and visible on the owner's Windows Rhino 8. The owner confirmed "shows up now" after the tab-placement adjustment.

## Read this before making further loading changes

The missing tab was caused by **ribbon overflow**, not failure to load the current plugin. The host had 65 tabs. Particle Engine was index 63 (tab 64); its clickable header occupied x=1764..1792 in a ribbon only 1650 pixels wide. Moving the existing tab to index 11, just after Rhino, brought it to x=308..336. Its six panels remained intact, and the user confirmed it appeared.

The repositioning is a **session-only workaround**. It is not built into the GHA, does not save a custom layout, and can reset when the ribbon rebuilds or Rhino restarts. A persistent, user-controlled placement option is possible future work, not a completed fix. Do not solve overflow by changing target frameworks, generating duplicate tabs, or disabling other plugins without evidence and owner direction.

## Changes already applied to this repository

- `src/ParticleEngine.Grasshopper/ParticleEngineBranding.cs`: a public `GH_AssemblyPriority` registers the exact `Particle Engine` category, `PE` abbreviation, `P` symbol and 16x16 category icon before component discovery. Lazy bitmap loading clones images before resource streams are disposed.
- `Resources/ParticleEngine16.png` and `ParticleEngine24.png`: original embedded particle graphics. The 24x24 icon is shared by the assembly and all 20 components; distinct per-component icons are not yet implemented.
- `ParticleEngineInfo.cs` and `Components/ParticleComponentBase.cs`: use the branding constants and icons. All components retain primary exposure.
- `ParticleEngine.Grasshopper.csproj`: retains the pre-existing single `netstandard2.0` target and compile-only RhinoCommon/Grasshopper 7.38.24338.17001 references. A build-only `Microsoft.NETFramework.ReferenceAssemblies.net48` reference supplies the System.Drawing identity required by this API. No Rhino, GH or drawing DLLs are installed alongside the plugin. Removed the unsupported comment claiming net7 load isolation necessarily prevents ribbon tabs.
- `scripts/install-ribbon.ps1`: checks for running Rhino/Compute processes, rejects duplicate ParticleEngine.gha installations under Libraries, backs up replaced files outside Libraries and verifies copied hashes. It does not recursively delete plugin folders.
- `.gitignore`: excludes local installation backups and diagnostic output.

GUIDs, component ports, defaults and simulation logic were unchanged. These changes do not implement M2 or establish Culebra behavioural parity. The current rebuild is still the M1 component scaffold documented in PROGRESS.md.

## Installer follow-up

The original one-time apply script in the Codex workspace initially rejected its own already-applied files when rerun. It was corrected to accept matching new hashes while rejecting unrelated source changes. Its launcher also keeps errors visible; a closing PowerShell window was not proof of success.

For continued development in this repository, use **`Install Particle Engine.cmd`** at the repository root, or `powershell -NoProfile -File scripts/install-ribbon.ps1 -Configuration Release`. The launcher calls the local installer and leaves its result visible. Build first, and save/close Rhino and any Rhino Compute process. Nothing here terminates those processes. The old workspace apply script is not needed to install future repository builds.

Installed location: `%APPDATA%\Grasshopper\Libraries\ParticleEngine-dev`. Install only `ParticleEngine.gha` and `ParticleEngine.Core.dll` together.

Owner-confirmed last installation backup: `D:\CLAUDE\Particle Engine\.install-backups\56c6bc8cefd540e5bf9431231c89012c`.

## Verification and evidence

Windows build/test results from this repair:

- Release solution build: zero warnings and errors.
- Core tests: 31/31 on .NET 8, 31/31 on .NET Framework 4.8.
- Port conformance: 5/5 on .NET 8.
- Both PNG resources decoded directly from the GHA at 16x16 and 24x24.
- A separate Windows harness instantiated all 20 component classes and decoded their icons. This is not a simulation test. Initializing the global Grasshopper component server outside Rhino was abandoned because it scans other plugins; do not treat that attempt as a successful host test.

Actual owner-host checks, Rhino 8 Windows, runtime **.NET 10.0.11**:

- Both installed assemblies loaded from ParticleEngine-dev; exactly one Particle plugin discovered.
- All 20 expected GUIDs registered under library GUID `3f2a6c10-7b4e-4d2a-9c1f-0a5b6e2d41a0`.
- All 20 proxies: SDK compliant, primary exposure, not obsolete.
- Category icon 16x16, short name PE, symbol P present.
- Default and live ribbon both contained Particle Engine and six panels.
- No recorded Particle Engine loading error. Other plugins produced errors; no evidence linked those errors to this tab problem.
- Moving the existing tab into the visible strip worked; the owner confirmed it appeared.

See `verification/ribbon-host-summary.json` for the focused evidence. All modified plugin source files, both embedded icons, the installer and the two built binaries were hash-checked against the verified repair before preparing this handover.

**Not verified:** Mac loading/drawing, Rhino 7, Rhino 8 .NET Framework host loading, simulation behaviour in the rebuild, or persistence of the placement adjustment. .NET Framework unit-test success is not a Rhino host test. Earlier milestone claims about other machines remain historical claims, not tests rerun during this repair.

## Scripts transferred for continuing diagnosis

Run these with Rhino's `_-RunPythonScript` command while Grasshopper is open. Supply the quoted full path to the chosen file under this repository's `scripts` folder.

| Script | Purpose |
| --- | --- |
| `particle-engine-diagnose.py` | Inspect loaded assemblies, registered components, discovered files, category metadata and recorded load errors. |
| `particle-engine-refresh-ribbon.py` | Record compatibility flags and tabs, refresh the ribbon, and try selecting the existing Particle Engine tab. This alone did not fix overflow. |
| `particle-engine-show-tab.py` | Move only the existing Particle Engine tab just after Rhino, select it, and check its clickable region fits inside the ribbon. Session-only. |

Scripts save reports in the repository's ignored `diagnostics` directory. Their output paths were adapted for repository use; the original versions were run in Rhino, and the relocated versions were syntax-checked. No plugin or model changes are made by the diagnostic scripts. The placement script reorders one existing entry in the SDK's exposed tab list and calls LayoutRibbon; no public move method is available in the inspected SDK. It keeps all tab/panel objects and attempts to restore the old order if repositioning fails. Test this workaround on another host before incorporating it into product code.

Two diagnostic pitfalls:

1. The ribbon report's `Visible=false` appeared for all inactive tabs before selection. It did not prove that those categories were disabled. Inspect clickable bounds and the selected tab instead.
2. The unqualified `Box Spawn` lookup matched the installed **Culebra_GH** component. Check Particle Engine's category and GUIDs when testing search; both plugins use overlapping component names.

## Next work for Claude

1. Preserve the now-working build and registration; do not restart the loader investigation without new evidence.
2. If the owner wants persistent positioning, design an opt-in solution that preserves other tabs and user preferences; validate after restart and on Mac. Do not report the current script as permanent.
3. Resume M2 only under the existing project specification/probe workflow. Ribbon visibility does not mean simulation behaviours are implemented.
4. Review and commit the local repair/handover changes as appropriate; do not commit bin/obj, local backup DLLs or diagnostic logs.

## Automatic tab placement (ON BY DEFAULT, Windows) — added 31 Oct 2026, host-validated

Placement is now built into the plugin and **on by default** (owner's choice): no flag or script is
needed. macOS is a no-op (the ribbon is a WinForms control).

- `src/ParticleEngine.Grasshopper/RibbonPlacement.cs`: a `GH_AssemblyPriority` subscribes once to
  `Instances.CanvasCreated` and, each session, moves **only** the Particle Engine tab to just after
  the Rhino tab, restoring the order if anything fails — other plugins' tabs are preserved. The move
  is **retried on a 500ms timer** (max ~10s) because Grasshopper builds the ribbon incrementally after
  the canvas is created (the host's ribbon had 0 tabs for ~5s, then 64). Windows-guarded; never blocks
  loading. Not a component — conformance remains exactly 20. A small capped log is written to
  `%APPDATA%\ParticleEngine\pin-log.txt`.
- **Opt out:** `scripts/particle-engine-unpin-tab.py` creates `%APPDATA%\ParticleEngine\pin-tab.disabled`;
  while present the plugin stops repositioning. **Re-enable / move now:** `particle-engine-pin-tab.py`
  removes that flag and moves the tab immediately.
- csproj: build-only Windows `System.Drawing` and `System.Windows.Forms` references (Rhino supplies
  them at runtime).

**Validated (Windows Rhino 8 / .NET 10, 2026-10-31):** the tab auto-pins just after Rhino on a fresh
Rhino restart; owner confirmed, and the log showed the retry catching the ribbon once it was
populated (tick 12: "moved PE to index 11 of 64"). Two bugs fixed en route: an early
`GH_AssemblyPriority` setting read was unreliable (switched to a file check at canvas time), and
`BeginInvoke` at `CanvasCreated` threw because the editor window handle didn't exist yet (the timer is
now started directly on the UI thread).

**Still unverified:** opt-out + restart returning the tab cleanly; that other plugins' tabs and order
stay undisturbed over many sessions; and macOS (an explicit no-op there).
