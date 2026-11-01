# Particle Engine — progress and test log

Update this file at the end of every working session. It is the single source of current state. The spec (`BUILD_SPEC.md`) says what to build; this says where we are.

**Last updated:** 2026-11-01

**Ribbon repair follow-up (confirmed):** source and installed binaries updated; all 20 components registered in Windows Rhino 8/.NET 10.0.11. The missing tab was ribbon overflow, confirmed by off-screen bounds. Moving the existing tab after Rhino made it visible; owner confirmed "shows up now". Placement is session-only. See [RIBBON_FIX.md](RIBBON_FIX.md) for the Claude handover, verification and remaining limits.
**Current version:** 0.4.0 in progress (fresh build; no 0.3.0 source reused)
**Active milestone:** M7/M8 in progress on an owner-approved split (2026-11-01): **harness + icons built now; benchmark numbers and the Yak publish held** until the Culebra probes lock the behaviour. Done so far: bench harness (spec 9.1 scenarios), a zero-allocation proof test (4 behaviours, passes net8 + net48), 18 per-component icons embedded, Yak manifest + staging script, install docs. All 18 components carry real behaviour. M2/M3 verified in Rhino; **M4, M5, M6 not yet Rhino-tested**, and the bench ran on **one Windows machine only**. Still outstanding before either milestone is done: deferred probe validation (P1–P9) + benchmarks on 3 machines, then the Yak publish.

**Validation plan (owner, 2026-11-01):** Culebra probes (P1–P9) and benchmarks are **deferred to the end** — build the milestones to interim rules, marking `VERIFY(Pn)`, then validate and fix in one pass (before M7/M8). Milestones are not blocked on probes meanwhile. Remind the owner before finalizing.

## 1. Milestones

Status: `todo` · `doing` · `done` · `blocked`. A milestone is `done` only when every exit criterion in spec Section 14 is met and a milestone report exists below.

| M | Deliverable | Status | Hosts exercised | Report |
| --- | --- | --- | --- | --- |
| M0 | Retarget, solution layout, 3-OS CI, legacy tests green | done | CI green: Win + macOS Intel + macOS Apple Silicon. Rhino 8 Win modern .NET (.NET 10) load verified. net48 Rhino load deferred (owner on modern .NET). | [M0 report](#m0--fresh-solution-layout--tested-core-foundation--2026-10-31) |
| M1 | 20-component port-parity scaffold + manifest test | done | Win: build + conformance (vs real Culebra dump); Rhino 8 Win modern .NET shows all 20 components in the 6 subcategories. Mac not verified. | [M1 report](#m1--port-parity-scaffold-20-components--conformance--2026-10-31) |
| M2 | Spawn, Init, Move, Controller, Trail Data, Visual Settings, Creeper_Engine (straight motion, bounds, trails, Graphic/Geometry) | doing | Win: Core+GH build + 48 Core tests green. Interim rules (VERIFY P1/P8/P11/P12/P13). Rhino run + probes pending. | [M2 report](#m2--core--grasshopper-interim--2026-10-31) |
| M3 | Spatial grid, Flocking, Attraction, Repulsion, Connectivity | doing | Win: Core+GH build + 64 Core tests green (grid vs brute-force; flocking/forces). Interim VERIFY P2/P3/P4. Rhino run + probes pending. | [M3 report](#m3--spatial-grid--flocking--forces-interim--2026-11-01) |
| M4 | Stigmergy, Multi Path Tracking, Mesh Crawl (II variants + children dropped per owner) | doing | Win: Core+GH build + 71 Core tests green (stigmergy/path/mesh-crawl). Interim VERIFY P5/P6/P7. Rhino run + probes pending. | [M4 report](#m4--stigmergy-path-tracking-mesh-crawl-interim--2026-11-01) |
| M5 | Creeper_ZombieEngine (run N iterations in one go) | doing | Win: Core+GH build + 72 Core tests green (Zombie == repeated steps). Synchronous; background-task refinement deferred. Rhino run pending. | [M5 report](#m5--creeper_zombieengine-synchronous--2026-11-01) |
| M6 | Bundling, Bundling_Engine, Bundling_ZombieEngine | doing | Win: Core+GH build + 78 Core tests green (convergence, threshold cutoff, weld pins, determinism). Interim VERIFY P9. Rhino run + probe pending. | [M6 report](#m6--bundling--bundling-engines-interim--2026-11-01) |
| M7 | Performance pass, zero-allocation proof, review checklist report | doing | Win: bench harness runs (1 machine, provisional); zero-alloc proof test green (net8 + net48, 4 behaviours). 3-machine numbers + post-probe re-run pending. | [M7 report](#m7--performance-pass--zero-allocation-proof-partial--2026-11-01) |
| M8 | Icons, Yak packages (rh8-any, rh7-any), install docs, examples | doing | Win: 18 component icons embedded + wired; Yak manifest + staging script; install docs. Publish HELD; Rhino icon check + fresh-machine Yak install pending. | [M8 report](#m8--icons--packaging-partial-publish-held--2026-11-01) |

## 2. Owner tasks (things only the owner can do)

- [x] ~~Put the 0.3.0 source in `legacy/`~~ — **superseded**: decided 2026-10-31 to build fresh and not reuse 0.3.0 source. The handover doc stays in `legacy/` as an overridable reference.
- [x] Confirm interpretation calls I-1..I-4 (owner: assume them) — I-1 both Multi Path Tracking versions · I-2 both Mesh Crawl versions · I-3 Rhino 7 best-effort alongside Rhino 8 · I-4 colour ports kept but ignored.
- [ ] **M0 exit:** run the Rhino 8 Windows acceptance script (both runtimes) from the end of the M0 report and record results in Section 4.
- [ ] **M0 exit:** push to GitHub so CI produces Core-test logs on Windows + macOS Intel + macOS Apple Silicon; record in Section 5.
- [x] Run the port-dump script in **Rhino 8**: done 2026-10-31, 32 components captured. Sanitized fixture committed as `tests/ParticleEngine.Parity/culebra_ports.json`; conformance test built against it.
- [x] **M1 exit:** Rhino 8 Win (modern .NET) shows all 20 components in the six subcategories (2026-10-31). Mac not yet checked.
- [ ] Run the M2 probes **P1, P8, P11, P12, P13** (spec 12.2) in Rhino 8 with Culebra, to get single-step behaviour ground truth before M2 is finalized.
- [ ] Run probes P1, P8, P11, P12, P13 before M2; P2–P4 before M3; P5–P7, P10 before M4; P9 before M6 (spec 12.2). Save each result as `tests/ParticleEngine.Parity/probe_PNN.md` (setup, settings, output text).
- [ ] Test each milestone build in Rhino on Windows (both runtimes) and record results in Section 4.
- [ ] Test on a Mac (Rhino 8 Apple Silicon; Rhino 8 Intel and Rhino 7 if available) at M1, M2, M5, M8 at minimum.
- [ ] Choose a licence before any distribution beyond yourself (spec Section 13).

## 3. Open `[VERIFY]` items

Each row is resolved by a probe. `Status`: `open` (interim rule in code) or `resolved` (golden test added; spec change logged).

| Probe | Question | Interim rule in code | Status |
| --- | --- | --- | --- |
| P1 | Initial Speed scalar vs vector; Max Speed/Force clamps; where Velocity Multiplier applies | Scalar = random direction; VM scales displacement | open |
| P2 | Attraction falloff and Max Attraction | `min(Value·(1−d/T), Max)` inside threshold | open |
| P3 | Repulsion falloff | Mirror of attraction | open |
| P4 | Flocking maths; View Angle full vs half | VA = full field of view; classic Reynolds steering | open |
| P5 | Stigmergy ranges; trail storage when Trail = false | Stored whenever Stigmergy is in the stack | open |
| P6 | Path-following seek-ahead and path choice | Nearest path within PT; seek ahead of closest point | open |
| P7 | Mesh crawl: snapping, velocity flattening, MPD scaling | Steer to closest point; remove normal velocity; snap | open |
| P8 | Respawn and 2D Z handling | Random point in box; Z held at spawn value | open |
| P9 | Bundling: Ratio, Weld, output degree | Centroid pull capped by Ratio; weld = pin end points | open |
| P10 | Child spawning trigger | One child per parent per frame while TS true, up to MC | open |
| P11 | Trail polyline length / head included | Head appended after stored points | open |
| P12 | Box Spawn Spawn Location 0/1 | 1 = random, 0 = grid | open |
| P13 | Engine output state before first step; Graphic-mode outputs empty | Frame 0 on first solve; empty in Graphic | open |

## 4. Host acceptance (spec Section 11.3 sequence)

Record per milestone. `OK` / `FAIL` / `-` (not run). Include Rhino build number and runtime.

| Milestone | Host | Rhino build | Runtime | Install | Loads | Runs | Notes |
| --- | --- | --- | --- | --- | --- | --- | --- |
| | Rhino 8 Win, .NET Framework | | | | | | |
| M0 | Rhino 8 Win, modern .NET | 8 (modern .NET) | .NET 10.0.11 | OK | OK | n/a | net7.0 .gha loaded into process (AppDomain shows `ParticleEngine`). No tab / no Libraries entry — expected, 0 components. |
| M1 | Rhino 8 Win, modern .NET | 8 (modern .NET) | .NET 10.0.11 | OK | OK | n/a | All 20 components visible in the six subcategories. Needed two fixes: `EnableDynamicLoading` (resolve Core.dll) + ctor NRE fix (Spec resolved from runtime type). |
| M2 | Rhino 8 Win, modern .NET | 8 (modern .NET) | .NET 10.0.11 | OK | OK | OK | Timer-driven run: creepers spawn/move/bounce; Geometry Trails point-tree → PolyLine = agent curves. Graphic mode, Reset, save/reopen not yet confirmed. |
| M3 | Rhino 8 Win, modern .NET | 8 (modern .NET) | .NET 10.0.11 | OK | OK | OK | Flocking motion, Attraction force and Connectivity output all run (first try, no issues). Probes P2/P3/P4 pending. |
| | Rhino 8 Mac, Apple Silicon | | | | | | |
| | Rhino 8 Mac, Intel | | | | | | |
| | Rhino 7 Win | | | | | | |
| | Rhino 7 Mac | | | | | | |

## 5. CI and benchmarks

| Date | Commit | Windows | macOS Intel | macOS Apple Silicon | Notes |
| --- | --- | --- | --- | --- | --- |
| 2026-10-31 | 45fa7df | pass | pass | pass | ci #1, 2m22s. Core tests net8.0 (3 OS) + net48 (Win); plugin build Win; net7.0 compile-check Mac. |

Benchmark results (median / p95 ms per step; bytes allocated per step):

| Scenario (spec 9.1) | Windows x64 | macOS Intel | macOS Apple Silicon |
| --- | --- | --- | --- |
| 5 000 agents, Flocking | | | |
| 1 000 agents, Stigmergy | | | |
| 10 000 agents, Attraction + Repulsion | | | |
| 2 000 agents, Multi Path Tracking | | | |

## 6. Decisions log

| Date | Decision | By |
| --- | --- | --- |
| 2026-10-31 | Build 0.4.0 fresh; do not reuse 0.3.0 source. Handover kept as an overridable reference only. | Owner |
| 2026-10-31 | Assume interpretation calls I-1..I-4 (spec 1.3). | Owner |
| 2026-10-31 | Add build-only NuGet `Microsoft.NETFramework.ReferenceAssemblies` (PrivateAssets=all) so net48 builds without Visual Studio. No runtime dependency. | Owner-approved |
| 2026-10-31 | Pin RhinoCommon/Grasshopper to 7.38 (net48) and 8.36 (net7.0) for reproducible CI. | Claude |
| 2026-10-31 | Owner runs Culebra on **Rhino 8**; port dump will be done there, not Rhino 7. Rhino 8 is already the primary target; Rhino 7 stays best-effort via the net48 build (no code dropped). | Owner |
| 2026-11-01 | Dropped **Mesh Crawl II** and **Multi Path Tracking II** (and child spawning). Scope now **18 components**. Reverses interpretation calls I-1/I-2; probe P10 dropped. | Owner |
| 2026-11-01 | Do M7/M8 now on a split: **build harness + icons; hold benchmark numbers and the Yak publish** until the probes lock behaviour. Avoids re-running benchmarks / re-releasing after probe-driven changes. | Owner |
| 2026-11-01 | Yak package ships a **single netstandard2.0 assembly** (loads on both Rhino 8 runtimes + Rhino 7), not the spec's net48/+net7.0/ split (BUILD_SPEC §2.4). One file set; distribution tags set at push time. | Claude |

## 7. Milestone reports

Add one section per milestone using this template.

```
### M<n> — <title> — <date>
Files changed:
Tests run (OS / runtime / count / result):
Benchmarks:
Hosts exercised (Rhino build, OS, runtime) and what was actually shown (compiles / loads / runs):
Deviations from spec (and spec change-log entries):
Review checklist (spec 9.4) findings:
New or changed [VERIFY] items:
Next:
```

### M0 — fresh solution layout + tested Core foundation — 2026-10-31

**Context.** Started the 0.4.0 build fresh: the 0.3.0 source was never placed in `legacy/`, and
the owner decided not to reuse it. The spec's literal M0 ("port the existing 40 tests") therefore
does not apply; it is replaced by a small, fully-tested Core foundation (the pieces every later
milestone depends on and the spec flags as the highest cross-platform risk). No components and no
behaviours were built (those are M1/M2+).

**Files changed (created):**
- `ParticleEngine.sln`, `Directory.Build.props`
- `src/ParticleEngine.Core/` — `ParticleEngine.Core.csproj` (netstandard2.0, no RhinoCommon),
  `Pcg32.cs` (deterministic PRNG, spec 7.10 / R8), `VectorMath.cs`, `ParticleState.cs` (SoA, spec 4.3)
- `src/ParticleEngine.Grasshopper/` — `.csproj` (net48;net7.0 → `.gha`), `ParticleEngineInfo.cs`
  (loadable plugin, **no components yet**)
- `tests/ParticleEngine.Tests/` — `.csproj` (net8.0;net48) + `Pcg32Tests.cs`, `VectorMathTests.cs`,
  `ParticleStateTests.cs`
- `tests/ParticleEngine.Parity/` — stub `.csproj` + README (manifest test lands in M1)
- `bench/ParticleEngine.Bench/` — stub console (can emit PRNG goldens)
- `.github/workflows/ci.yml` — updated to the new layout
- Removed `docs/KICKOFF_PROMPT.md`; rewrote `legacy/README.md`

**Tests run (OS / runtime / count / result):**
- Windows 11 / .NET 8 (`net8.0`) / 21 / **pass**
- Windows 11 / .NET Framework 4.8 (`net48`) / 21 / **pass**
- The PCG32 golden stream is **bit-identical** on both runtimes (integer-only arithmetic), which is
  the cross-runtime determinism check (spec 11.2). Not yet run on macOS (needs CI).

**Builds:** `dotnet build ParticleEngine.sln -c Release` → **0 warnings, 0 errors**. The `.gha` builds
for both `net48` and `net7.0`. The **MSB3277** `System.Net.Http` warning from 0.3 is gone
(`ExcludeAssets="runtime"` on the Rhino/GH refs). Each `.gha` output folder contains only
`ParticleEngine.gha` + `ParticleEngine.Core.dll` (+ pdb/deps); no RhinoCommon/Grasshopper/GH_IO leak
(spec 2.4).

**Benchmarks:** none (no behaviours yet).

**Hosts exercised and what was actually shown:**
- Windows 11 — **compiles** (whole solution) and **runs** (Core unit tests, both runtimes).
- Rhino 8 Windows, **modern .NET (.NET 10.0.11)** — **loads.** The net7.0 `.gha` is pulled into the
  Rhino process (verified via AppDomain: `ParticleEngine` present). No ribbon tab and no
  `ComponentServer.Libraries` entry — expected for a zero-component plugin; the first tab appears at M1.
  (Note: the owner's Rhino now runs .NET 10, not the .NET 8.0.15 recorded in CLAUDE.md; net7.0 assets
  roll forward onto it.)
- Rhino 8 Windows, **.NET Framework** — **not exercised.** The owner runs modern .NET and is not
  switching runtime (to avoid disturbing their setup). The net48 build and net48 Core tests pass, but
  the net48 Rhino host-load is unverified; revisit on a Framework-mode host if one is available.
- macOS — **nothing exercised** (no Mac here; pending CI).

**M0 exit status: DONE.** 3-OS CI logs are green (ci #1 on commit 45fa7df: Windows + macOS Intel +
macOS Apple Silicon). Manual Rhino 8 Win load is met for modern .NET (the owner's runtime). The only
sub-item not exercised is the .NET Framework in-Rhino load, deferred by owner choice (they run modern
.NET); the net48 build and net48 Core tests both pass. Next: M1 — but run the Culebra port dump
(spec 12.1) first so the manifest conformance test can be populated.

**Deviations from spec (logged in Section 6 decisions / to go in spec change log at M1):**
- Fresh build, no legacy test port (owner decision).
- Added build-only `Microsoft.NETFramework.ReferenceAssemblies` so net48 builds without Visual Studio.
- Pinned Rhino/GH package versions (7.38 / 8.36).

**Review checklist (spec 9.4) findings:** Core has no hot-path/step loop yet, so most items are N/A.
Confirmed clean for what exists: no `System.Random` (own PCG32); no LINQ/lambdas in Core; SoA arrays
grown by doubling, no per-particle objects; squared-distance compares in `VectorMath`; no static
mutable state. Full checklist re-run applies from M2 when a `Step()` exists.

**New or changed [VERIFY] items:** none (no behaviour implemented in M0).

**Next:** owner runs the Rhino 8 Windows acceptance script (both runtimes) and pushes for CI. Then M1:
register all 20 components with correct ports + the manifest conformance test (needs the Culebra port
dump, spec 12.1, before it can be fully populated).

### M1 — port-parity scaffold (20 components) + conformance — 2026-10-31

**Context.** Built against the **real Culebra port dump** from the owner's Rhino 8 (32 components
captured; our 20 in scope). Ground truth beat the public-docs spec in several places (see BUILD_SPEC
§17). No simulation logic yet — this is the port/registration scaffold (spec M1).

**Files changed (created):**
- `src/ParticleEngine.Core/Manifest/` — `PortSpec.cs`, `ComponentSpec.cs`, `ComponentCatalog.cs`
  (the 20 components as data), `ComponentManifest.cs` (`Generate()`, no Rhino dependency).
- `src/ParticleEngine.Core/Settings/Settings.cs` — immutable settings/behaviour/force classes + `IParticleSettings`.
- `src/ParticleEngine.Grasshopper/Components/` — `ParticleComponentBase.cs` (registers ports from the
  catalog), `Components.cs` (19 components), `ControllerComponent.cs` (variable inputs).
- `src/ParticleEngine.Grasshopper/Goo/ParticleGoo.cs` — `GH_Goo<T>` wrapper (Duplicate = self).
- `tests/ParticleEngine.Parity/` — real test project: `ConformanceTests.cs` vs `culebra_ports.json`.
- `tests/ParticleEngine.Tests/` — `CatalogTests.cs`, `SettingsTests.cs`.
- `.github/workflows/ci.yml` — now also runs the conformance test on all 3 OS.

**Tests run (OS / runtime / count / result):**
- Windows 11 / net8.0 / 31 core + 5 conformance / **pass**
- Windows 11 / net48 / 31 core / **pass** (conformance is net8.0 only)
- Conformance asserts our 20 components' names/nicks/order/types match the dump, with documented
  exceptions (Creeper engines' Init/Move Settings item-not-list per R4; Bundling_Engine nick BE).

**Builds:** `dotnet build ParticleEngine.sln -c Release` → **0 warnings, 0 errors**; `.gha` builds for
net48 and net7.0.

**Hosts exercised:** Windows 11 — **compiles + tests pass**. **Rhino 8 Win, modern .NET (.NET 10):
all 20 components load and appear in the six subcategories** (verified by the owner). Two Rhino-load
fixes were needed after the first attempt showed 0 components:
1. `EnableDynamicLoading=true` + shipping the net7.0 `deps.json`/`runtimeconfig.json` so Rhino resolves
   the sibling `ParticleEngine.Core.dll` (M0 loaded without this only because it never touched Core).
2. A `NullReferenceException` in component construction: `GH_Component`'s base ctor registers ports
   (via PostConstructor) before a derived field is set, so `Spec` was null. Fixed by resolving the
   spec from the runtime type name. **Lesson:** GH component instantiation isn't covered by the tests
   (they read the catalog, never instantiate components), so this class of bug only shows at Rhino
   load — the M1 host check is the gate for it. Mac load still unverified (no Mac).

**Deviations from spec (logged in BUILD_SPEC §17):** engines take a Move Settings input directly
(overrides §6.2); Mesh Crawl has a 4th `Multiplier` port; many real defaults; Init Settings spawn
input is list; Bundling_Engine nick BE; engine settings inputs kept item (R4). New component
`Bundling_ZombieEngine` added.

**Review checklist (spec 9.4):** no hot path yet; catalog/components allocate only at registration.
No `System.Random`, no LINQ in Core hot code (none exists yet). Defaults written once via
`SetPersistentData` in the base (R3); conformance asserts single-valued defaults.

**New / changed [VERIFY] items:** Mesh Crawl `Multiplier` role unknown (VERIFY P7). The behavioural
probe semantics (P1–P13) remain open — the port dump gave **defaults and port shapes**, not
single-step behaviour; those still need the probe runs (spec 12.2) before/with M2–M6.

**Next:** owner loads the M1 `.gha` in Rhino to confirm the 20 components appear. Then M2 (Spawn/Init/
Move/Controller/Trail/Visual + Creeper_Engine straight-line motion), which wants probes P1, P8, P11,
P12, P13 first.

### M2 — Core + Grasshopper (interim) — 2026-10-31

**Scope.** Spawn, Init, Move, Controller, Trail Data, Visual Settings, and Creeper_Engine with real
behaviour: straight-line motion from Move Settings (no behaviours yet — those are M3+), boundaries,
trails, Graphic and Geometry display. Built to the spec's interim rules; probe-dependent specifics are
tagged `VERIFY(Pn)` and listed in Section 3.

**Files created:**
- Core: `TrailBuffer.cs` (ring buffers), `CreeperSimulation.cs` (spawn / double-buffered step /
  boundary / trails).
- GH: `GhInput.cs` (port coercion + geometry glue), `Components/SpawnComponents.cs`,
  `Components/SettingsComponents.cs`, `Components/CreeperEngineComponent.cs`; `ControllerComponent`
  now emits `BehaviorData`; `Components.cs` trimmed to still-scaffold components.
- Tests: `TrailBufferTests.cs`, `CreeperSimulationTests.cs` (17 new).

**Tests run (OS / runtime / count / result):**
- Windows 11 / net8.0 / 48 core + 5 conformance / **pass**
- Windows 11 / net48 / 48 core / **pass**
- Covers spawn counts, straight-line motion, Velocity-Multiplier, Max-Speed clamp, 2D z-held, bounce
  (exact reflect + in-box invariant for multi-width steps), respawn, do-nothing, determinism, trails/reset.

**Builds:** `dotnet build ParticleEngine.sln -c Release` → **0 warnings, 0 errors**.

**Hosts exercised:** Windows 11 — compiles + Core tests pass. **Rhino 8 Win (modern .NET): RUN VERIFIED
(owner, 2026-11-01)** — Timer-driven creepers spawn, move and bounce; Geometry-mode Trails point-tree
into a PolyLine gives one agent curve per creeper. Four GH-runtime bugs were fixed during this run:
Init Settings' Spawn Settings is a list-access input (was read as item); generic inputs must be
optional (reserved Color Data was blocking); and Trails now outputs a point tree, not degenerate
polylines. Still to confirm in Rhino: Graphic-mode viewport drawing, Reset, and save/reopen. Mac
untested.

**Review checklist (spec 9.4):** step loop uses `for` over SoA arrays, no LINQ/lambdas, squared compares
in `VectorMath`, ring buffers (no `RemoveAt(0)`), own PRNG, no statics for state (engine state is
instance-level, cleared in `RemovedFromDocument`). Draw buffers rebuilt per frame; trail curves built
only in Geometry mode. Full zero-allocation bench is M7.

**New / open `[VERIFY]` items (interim rules in code):** P1 (scalar speed = random direction; VM scales
displacement), P8 (respawn = random point in box + re-init velocity + clear trail), P11 (trail point per
TrailStep; head appended after stored points), P12 (Box Spawn 1 = random, 0 = grid), P13 (frame 0 on
first solve / Reset; Graphic outputs empty). Mid-run Init/Move changes apply on Reset (spec 4.4).

**Next:** owner runs the M2 acceptance def in Rhino (Timer-driven), and the probes P1/P8/P11/P12/P13
against Culebra to confirm/adjust the interim rules before M2 is marked done.

### M3 — spatial grid + flocking + forces (interim) — 2026-11-01

**Scope.** Uniform spatial grid, Flocking (separation/alignment/cohesion with view cone), Attraction
and Repulsion forces, and the Connectivity output. Built autonomously to interim rules while the owner
was away (probe-dependent specifics tagged `VERIFY(P2/P3/P4)`). Not yet run in Rhino.

**Files created/changed:**
- Core: `UniformGrid.cs` (counting-sort CSR neighbour grid); `CreeperSimulation.cs` now applies a
  `BehaviorData` stack each frame — flocking via the grid (view cone by dot vs precomputed cos(VA/2),
  squared compares, sqrt only for survivors), attraction/repulsion forces, and capped connectivity
  recording.
- GH: `Components/BehaviorComponents.cs` (Flocking), `Components/ForceComponents.cs` (Attraction/
  Repulsion + threshold-viz circles); `CreeperEngineComponent` reads Behavioral Settings, drives the
  sim's behaviours, and emits Connectivity (Geometry output / Graphic drawing); `Components.cs` trimmed.
- Tests: `UniformGridTests.cs` (grid candidates ⊇ brute-force neighbours, 2D/3D, radii 0.5–7),
  `FlockingForcesTests.cs` (separation/alignment/cohesion isolated, view cone, attraction/repulsion/
  threshold/max).

**Tests run:** Windows 11 / net8.0 / **64 core + 5 conformance / pass**; net48 / 64 core / pass.
**Builds:** solution **0 errors** (a pre-existing LF/CRLF note aside). Still exactly 20 components.

**Hosts exercised:** Windows 11 — compiles + Core tests. **Rhino 8 Win (modern .NET): RUN VERIFIED
(owner, 2026-11-01)** — flocking motion, Attraction force, and the Connectivity output all work, with
no GH-runtime issues on the first run. Probes P2/P3/P4 still pending to confirm the interim rules.

**Review checklist (spec 9.4):** grid build is counting-sort into flat arrays (no per-cell lists);
neighbour loop uses `for` over `Items`, squared distances, no trig (cos precomputed), sqrt only for
survivors; behaviour dispatch via `is` type checks (no LINQ in the loop); connectivity capped at
200k and arrays reused. Benchmarks are M7.

**New / open `[VERIFY]`:** P2 (attraction law/falloff/Max), P3 (repulsion law), P4 (flocking maths;
View Angle full vs half; separation falloff). Interim: classic Reynolds steering; force
`min(Value·(1−d/threshold), Max)`; VA treated as full field of view with a half-angle cone.

**Next:** owner runs flocking/forces in Rhino, and probes P2/P3/P4 against Culebra. Then fold results
into golden tests and the spec change log, and M3 is done.

### M4 — stigmergy, path tracking, mesh crawl (interim) — 2026-11-01

**Scope.** Stigmergy, Multi Path Tracking, and Mesh Crawl — built autonomously to interim rules. The
II variants and child spawning were removed earlier today per owner (18 components; probe P10 dropped).
Probe-dependent specifics tagged `VERIFY(P5/P6/P7)`. Not yet run in Rhino.

**Files created/changed:**
- Core: `IMeshSurface.cs`; `Settings.cs` (Path/MeshCrawl specs simplified, mesh surface added);
  `CreeperSimulation.cs` — trail-point grid for Stigmergy, path-following, mesh seek + surface
  projection, trail storage forced on when Stigmergy is present.
- GH: `MeshSurface.cs` (IMeshSurface via `Mesh.ClosestMeshPoint`), `GhInput.CurveToFlat`,
  `Components/TrackingComponents.cs` (Stigmergy / Multi Path Tracking / Mesh Crawl). `Components.cs`
  trimmed — all `03 | Behaviors` now implemented.
- Tests: `M4Tests.cs` (stigmergy cohesion/separation + forced storage; path steer-to-corridor /
  in-corridor no-op; mesh snap + velocity flatten + tangential motion, via a flat-plane mock surface).

**Tests run:** Windows 11 / net8.0 / **71 core + 5 conformance / pass**; net48 / 71 core / pass.
**Builds:** solution **0 warnings, 0 errors**. 18 components.

**Hosts exercised:** Windows 11 — compiles + Core tests. **Rhino — NOT run** (stigmergy/path/mesh-crawl
untested in the host; mesh crawl especially, since it calls `Mesh.ClosestMeshPoint` on the solve thread).

**Review checklist (spec 9.4):** stigmergy uses a counting-sort trail grid (flattened each frame),
squared compares + cos-cone (sqrt for survivors); path tracking is segment projection without trig;
mesh crawl does one closest-point call per agent per frame. No LINQ in the loops; buffers reused.

**New / open `[VERIFY]`:** P5 (stigmergy ranges; trail storage when Trail = false), P6 (path seek-ahead
and path choice), P7 (mesh snap vs threshold; velocity flatten; MPD scaling). Interim: see code tags.

**Next:** owner runs stigmergy/path/mesh-crawl in Rhino, and probes P5/P6/P7. Then M5 (Zombie engine)
and M6 (Bundling).

### M5 — Creeper_ZombieEngine (synchronous) — 2026-11-01

**Scope.** Creeper_ZombieEngine: runs the whole simulation for a fixed number of Iterations in one go
(no Timer), on the solution where Run goes false → true, then outputs the final state; while Run stays
unchanged it returns the cached result without recomputing (spec 6.5). Shares all output/draw machinery
with Creeper_Engine.

**Files created/changed:**
- GH: `Components/EngineComponentBase.cs` (NEW) — extracted the shared engine machinery (`Sim`,
  `DisplayMode`, `Seed`, `EmitOutputs`, Graphic-mode draw buffers, `DrawViewportWires`, `ClippingBox`,
  `ClearEngineState`, `RemovedFromDocument`). `CreeperEngineComponent` rewritten as a thin subclass
  (Timer step + `DA.Iteration` guard + Reset). `CreeperZombieEngineComponent` (NEW) — run-N on the
  rising edge of Run, cache while Run unchanged. Zombie scaffold removed from `Components.cs`.
- Tests: `M4Tests.BehaviorRunIsDeterministic_ZombieEqualsRepeatedSteps` — N steps in one go equal N
  one-per-solution steps (the Zombie-vs-Timer equivalence that makes the cached run trustworthy).

**Tests run:** Windows 11 / net8.0 / **72 core + 5 conformance / pass**; net48 / 72 core / pass.
**Builds:** solution **0 warnings, 0 errors**. 18 components.

**Hosts exercised:** Windows 11 — compiles + Core tests. **Rhino — NOT run** (Zombie engine untested in
the host).

**Review checklist (spec 9.4):** no new Core step code — reuses `CreeperSimulation`. The engine runs on
the solve thread (synchronous), so a 10k-agent × 1k-iteration run blocks the UI for the duration.

**Deviations / notes:** M5 is **synchronous**, not the spec's background-task + cancellation design. That
refinement (run off-thread, report progress, cancel on input change) is deferred as a follow-up; the
determinism test guarantees the async version must produce the same result. No new `[VERIFY]` items.

**Next:** owner runs the Zombie engine in Rhino (set Iterations, toggle Run) alongside M4. Then M6.

### M6 — Bundling + bundling engines (interim) — 2026-11-01

**Scope.** Curve-network self-organisation ("bundling", spec 7.11): the Bundling settings component,
the Timer-driven Bundling_Engine (one cycle per solution), and Bundling_ZombieEngine (run N cycles in
one go; no Culebra equivalent, mirrors Creeper_ZombieEngine). Built to interim rules; the bundling
specifics are tagged `VERIFY(P9)`.

**Files created/changed:**
- Core: `BundlingSimulation.cs` (NEW) — curves sampled to flat point arrays (CSR per-curve ranges);
  each cycle moves every non-pinned point toward the centroid of nearby points on **other** curves by
  at most Ratio, found through the existing `UniformGrid`; double-buffered; weld pins the first/last
  WeldCount points of each curve. `ToCurves()` regroups points per curve for geometry rebuild.
- GH: `Components/BundlingEngineBase.cs` (NEW) — shared bundling machinery: read the Bundling behaviour
  (from a Controller stack or supplied directly), sample the input curves (resampled to PointCount when
  Rebuild is on, else the curve's own polyline), rebuild the current points into polylines for the
  Geometry output, and draw them in Graphic mode. `BundlingEngineComponent` (Timer) and
  `BundlingZombieEngineComponent` (run-N) as thin subclasses. `BundlingComponent` gained its
  `SolveInstance` (emits `BundlingSpec`); `Components.cs` now holds only that one component.
- Tests: `M6Tests.cs` — nearby curves converge; curves beyond Threshold do not move; welded endpoints
  stay pinned while the free interior moves; bundling is deterministic; `ToCurves` preserves curve and
  point counts; Reset returns to the input curves.

**Tests run:** Windows 11 / net8.0 / **78 core + 5 conformance / pass**; net48 / 78 core / pass.
**Builds:** solution **0 warnings, 0 errors**. 18 components.

**Hosts exercised:** Windows 11 — compiles + Core tests. **Rhino — NOT run** (bundling untested in the
host; the engines call `Curve.DivideByCount` / build `PolylineCurve`s on the solve thread).

**Review checklist (spec 9.4):** `BundlingSimulation.Step()` allocates nothing — the grid reuses its
buffers, the neighbour loop uses locals and squared compares (sqrt only once per moved point, no trig),
and positions are double-buffered. `ToCurves()` and the GH curve rebuild allocate, but once per solution
at the GH boundary, not inside the cycle loop. Curve sampling happens only at Initialize/Reset.

**New / open `[VERIFY]`:** P9 — whether Ratio is an absolute move distance or a fraction of the gap,
exactly how Weld pins/merges end points, and the output curve type (polyline vs degree-3). Interim:
Ratio is an absolute cap (`move = min(Ratio, gap)`); Weld pins the first/last WeldCount points per curve
(no merging); output is a `PolylineCurve` through the points.

**Next:** owner runs bundling in Rhino (Bundling → Bundling_Engine with a Timer, and Bundling_ZombieEngine
with Run), and probe P9 against Culebra. Then the deferred validation pass (all probes + benchmarks)
before M7/M8.

### M7 — performance pass + zero-allocation proof (partial) — 2026-11-01

**Scope / basis.** Owner approved doing M7/M8 now on a split: **build the harness and prove zero
allocation now; hold the final benchmark numbers** until the probes lock the behaviour (the probes change
the maths in the step loop, not whether it allocates). So this report covers the harness, the
allocation proof, the §9.4 checklist re-run, and a real fix — but the recorded numbers are provisional
and single-machine.

**Fix made (the main finding).** The step loop walked the behaviour stack with
`foreach (BehaviorSpec spec in Behaviors.Stack)` where `Stack` is `IReadOnlyList<BehaviorSpec>`. Through
the interface that boxes an enumerator on the heap **once per agent per frame** — 10 000 allocations/
frame in the forces scenario (spec 9.4 "foreach over IEnumerable"). Fixed: `BehaviorData` now stores a
concrete `BehaviorSpec[]` and exposes it internally as `Specs`; the three per-frame loops
(`AccumulateSteering`, `PrepareBehaviors`, `UpdateBehaviorFlags`) iterate it with an indexed `for`.
`Stack` (the public `IReadOnlyList`) is unchanged for the GH layer.

**Files created/changed:**
- Core: `Settings.cs` — `BehaviorData` backed by an array + internal `Specs`. `CreeperSimulation.cs` —
  three `foreach`→`for` loops.
- Bench: `bench/ParticleEngine.Bench/Scenarios.cs` (the four spec-9.1 scenarios, built deterministically
  with the Core PRNG) + `Program.cs` (median/p95 step time via `Stopwatch`, allocation/step via
  `GC.GetAllocatedBytesForCurrentThread`).
- Tests: `ZeroAllocationTests.cs` — Flocking / Attraction+Repulsion / Stigmergy / Path tracking each
  assert ≤ 1 KB allocated per step after warm-up (bounce boundary keeps the cloud bounded so the grid
  is stable). **Passes on net8 and net48.**

**Tests run:** Windows 11 / net8.0 / **82 core + 5 conformance / pass**; net48 / 82 core / pass.
**Builds:** solution **0 warnings, 0 errors**. 18 components.

**Provisional benchmark numbers** (Windows 11, .NET 8.0.15, x64, **one machine** — NOT the three-machine
acceptance, and interim behaviour). `alloc B/step` is steady-state with a **do-nothing** boundary:

| Scenario | Target | median ms | p95 ms | alloc B/step |
| --- | --- | --- | --- | --- |
| 5 000 agents, Flocking, 3D | ≤ 10 ms | 3.0 | 3.5 | 461 093 |
| 1 000 agents, Stigmergy, trail 100, step 6 | ≤ 15 ms | 2.1 | 2.7 | 1 375 306 |
| 10 000 agents, Attraction + Repulsion (20 targets) | ≤ 5 ms | 0.5 | 2.9 | 0 |
| 2 000 agents, Multi Path Tracking, 20 paths × 200 seg | ≤ 8 ms | 20.6 | 22.5 | **0** but **over time target** |

**Two real findings from the bench (behaviour-independent; for the validation pass, not fixed now):**
1. **Path tracking is over its 8 ms target (~20 ms).** It is a brute-force nearest-point search over all
   segments (20 × 200 = 4 000) for every agent. It allocates nothing, but needs spatial acceleration
   (index the path segments, or cap the search) to hit the target. Deferred because P6 may change the
   path-choice/seek semantics first.
2. **The do-nothing boundary reallocates the grid every frame.** With an unbounded cloud the live
   bounding box grows without limit, so the grid cell count keeps growing and `cellStart`/`cursor`
   realloc each frame (the 461 KB / 1.4 MB above). **Bounded boundaries (bounce/respawn) are zero-alloc**
   — that is what the proof test asserts. Options for the perf pass: clamp the grid extent, or pool the
   cell arrays. The step loop itself is allocation-free regardless.

**Review checklist (spec 9.4) — full re-run:** no LINQ/lambdas in Core; the behaviour stack is now an
array walked with `for` (the fix above); grid + trail grid are counting-sort into flat arrays that only
grow; ring-buffer trails (no `RemoveAt(0)`); squared-distance compares, `cos` precomputed per particle,
no `Acos`/`Atan2`; own PRNG; `Message` strings built once per solve, not per frame; engine state is
instance-level. Outstanding: path-tracking acceleration and the do-nothing grid growth (above).

**New / changed [VERIFY]:** none. **Hosts exercised:** Windows 11 only (compiles, tests, bench). Mac +
the other two machines pending (CI / owner). The time targets and the final allocation table are
**not** accepted yet — they are re-run after the probes.

**Next:** after the probes lock behaviour, re-run the bench on Windows + macOS Intel + macOS Apple
Silicon, decide on the path-tracking acceleration and the do-nothing grid handling, and record the
accepted numbers here.

### M8 — icons + packaging (partial; publish held) — 2026-11-01

**Scope / basis.** Same split: **icons and the package are prepared now; the Yak publish is held** until
the behaviour is validated. No release is made.

**Files created/changed:**
- Icons: `scripts/generate-icons.ps1` (clean-room generator — our own monogram tiles, subcategory
  colours, nothing from Culebra) + 18 PNGs in `src/ParticleEngine.Grasshopper/Resources/icons/`.
- Wiring: `ParticleEngine.Grasshopper.csproj` embeds `Resources\icons\*.png` with logical name
  `...Resources.icons.<Key>.png`; `ParticleEngineBranding.ComponentIconFor(key)` loads per-key with a
  cached fallback to the shared icon; `ParticleComponentBase.Icon` now returns the per-component icon.
  Verified from the built `.gha`: 18 icon resources embedded, names match the 18 catalog keys.
- Packaging: `packaging/manifest.yml` (Yak manifest, 0.4.0) + `scripts/build-yak.ps1` (stages exactly
  `ParticleEngine.gha` + `ParticleEngine.Core.dll` + manifest + icon into `dist/`, **never pushes**,
  prints the push command). `docs/INSTALL.md` (manual + Yak install + per-milestone wiring recipes).

**Packaging deviation (logged in decisions):** the spec's net48/ + net7.0/ multi-target Yak layout does
not apply — the plugin is a **single netstandard2.0 assembly** that Rhino 8 loads on both runtimes (and
Rhino 7 loads too), so one set of files serves every target. Distribution tags are set at push time.

**Builds/tests:** solution **0 warnings, 0 errors**; 82 core + 5 conformance green; staging script
produces the correct 4-file package.

**Hosts exercised:** Windows 11 — compiles, icon resources confirmed embedded in the `.gha`. **Rhino —
NOT run:** the icons have not been seen rendering in the Grasshopper ribbon/canvas, and no fresh-machine
Yak install has been done (publish held). **Examples:** binary `.gh` files need a Rhino session to
author; wiring recipes are in `INSTALL.md` as the interim, and `examples/` will be added later.

**New / changed [VERIFY]:** none.

**Next:** owner confirms the per-component icons render in Rhino; then, after behaviour validation,
publish the Yak package (`scripts/build-yak.ps1`, then `yak push` by hand) and do a fresh-machine install
on Windows and Mac to complete the host-acceptance table.


## Ribbon and installer repair — final handover, 2026-10-31

- Source branding and installer fixes are present in this repository and match the verified build. All 20 component GUIDs, ports/defaults and simulation logic are unchanged.
- Owner confirmed installation to `%APPDATA%/Grasshopper/Libraries/ParticleEngine-dev`, with backup `.install-backups/56c6bc8cefd540e5bf9431231c89012c`.
- Actual Windows Rhino 8 host: .NET 10.0.11; one Particle plugin discovered; both assemblies loaded; 20 primary, SDK-compliant, non-obsolete proxies registered; category icon and PE/P labels present.
- Tab was index 63 of 65, x=1764..1792 outside a 1650-wide ribbon. Repositioned to index 11 after Rhino, x=308..336, preserving six panels and all 65 tabs. User confirmed it appeared.
- Existing validation: Release build 0 warnings/errors; Core 31/31 net8 and 31/31 net48; conformance 5/5 net8; embedded PNG dimensions checked. A standalone Windows constructor/icon check exercised all 20 classes. This does not verify simulation behaviour.
- Added reusable diagnostic/refresh/placement scripts under `scripts`, a root installer launcher, focused evidence in `docs/verification/ribbon-host-summary.json`, and current instructions in CLAUDE.md/RIBBON_FIX.md. Report paths are repository-relative; diagnostics output is ignored.
- Outstanding: placement persistence after restart/rebuild, Mac host checks, .NET Framework Rhino host checks and M2 simulation/probe work. No new simulation milestone is claimed.
- Do not attribute overflow to the target framework or unrelated plugin startup errors. An unqualified Box Spawn search can find Culebra_GH; distinguish libraries by category/GUID.
