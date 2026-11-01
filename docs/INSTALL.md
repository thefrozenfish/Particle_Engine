# Installing and using Particle Engine

Particle Engine is a Grasshopper plugin for Rhino 8 (Windows and macOS; Rhino 7 best-effort). It ships
two files that must stay together:

- `ParticleEngine.gha` — the components
- `ParticleEngine.Core.dll` — the simulation engine

> The plugin is a single `netstandard2.0` build. Rhino 8 loads it on **both** the .NET Framework and
> modern-.NET runtimes, and Rhino 7 loads it too, so there is only one set of files to install.

## Option A — Yak (once published)

> **Not published yet.** The Yak package is prepared (`packaging/manifest.yml`, `scripts/build-yak.ps1`)
> but publishing is held until the simulation behaviour is validated against the reference. When it is
> live, install from Rhino's package manager:

1. In Rhino, run `_PackageManager`.
2. Search for **ParticleEngine**, select it, click **Install**.
3. Restart Rhino. The **Particle Engine** tab appears in Grasshopper.

## Option B — Manual install (works today)

1. Build: `dotnet build ParticleEngine.sln -c Release`
   (or double-click `Install Particle Engine.cmd`, which builds and copies for you).
2. In Grasshopper: **File > Special Folders > Components Folder**.
3. **Close Rhino** (and Rhino.Compute if running).
4. Copy **both** `ParticleEngine.gha` and `ParticleEngine.Core.dll` from
   `src/ParticleEngine.Grasshopper/bin/Release/netstandard2.0/` into that Components folder.
   Use **one install location only** — a second copy elsewhere will cause load conflicts.
5. On Windows, if the files were downloaded, right-click each > **Properties** > **Unblock**.
   On macOS, if Rhino reports a blocked load, clear the quarantine attribute on the extracted files.
6. Start Rhino, open Grasshopper. The components appear under the **Particle Engine** tab in six
   groups: `01 | Spawn Types`, `02 | Initialize`, `03 | Behaviors`, `04 | Forces`, `05 | Engine`,
   `06 | Display`.

If the tab does not appear, see [RIBBON_FIX.md](RIBBON_FIX.md) — on a very full ribbon the new tab can
land off-screen; the placement helper in `scripts/` moves it into view.

## Example setups

Binary `.gh` example files are authored in a Rhino session (they cannot be written from the build here);
they will be added under `examples/` once produced. The minimal wirings below reproduce each milestone
and deliberately avoid the duplicate-default pitfall (R3) — every scalar input keeps its single built-in
default unless you change it.

**1. Moving creepers (M2).**
`Box Spawn` (Box, Count) → `Init Settings` ← set Dimension and Boundary → feed `Init Settings` and a
`Move Settings` into `Creeper_Engine`. Add a Grasshopper **Timer** on the engine. Attach a `Trail Data`
(Trail = true) → `Visual Settings` (Display Mode = 1, Geometry) → engine. Wire the engine's **Trails**
output into a **PolyLine** to draw one agent curve per creeper.

**2. Flocking / forces (M3).**
Build a behaviour with `Flocking` (or `Attraction Force` / `Repulsion Force`), pass it through the
`Controller`, and feed the Controller's output into the engine's **Behavioral Settings**. The
`Attraction`/`Repulsion` **Threshold Viz** outputs draw the influence circles.

**3. Trail chasing, paths, mesh (M4).**
`Stigmergy`, `Multi Path Tracking` (feed polylines) or `Mesh Crawl` (feed a mesh) → `Controller` →
engine. Stigmergy records trails automatically.

**4. Batch run (M5).**
Use `Creeper_ZombieEngine` instead of `Creeper_Engine`: set **Iterations**, toggle **Run** true — it
runs the whole simulation in one solve and outputs the final state (no Timer).

**5. Curve bundling (M6).**
`Bundling` (Threshold, Ratio, Rebuild, Point Count, Weld Count) → `Bundling_Engine` with a **Timer** and
your input **Curves**; each cycle draws neighbouring curves together. `Bundling_ZombieEngine` runs a
fixed number of cycles in one go.
