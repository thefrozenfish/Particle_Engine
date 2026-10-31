# Particle Engine — progress and test log

Update this file at the end of every working session. It is the single source of current state. The spec (`BUILD_SPEC.md`) says what to build; this says where we are.

**Last updated:** 2026-10-31
**Current version:** 0.4.0 in progress (fresh build; no 0.3.0 source reused)
**Active milestone:** M1 **done** (conformance green; all 20 components visible in Rhino 8 Win modern .NET). Mac load not verified. M2 next — needs behavioural probes first.

## 1. Milestones

Status: `todo` · `doing` · `done` · `blocked`. A milestone is `done` only when every exit criterion in spec Section 14 is met and a milestone report exists below.

| M | Deliverable | Status | Hosts exercised | Report |
| --- | --- | --- | --- | --- |
| M0 | Retarget, solution layout, 3-OS CI, legacy tests green | done | CI green: Win + macOS Intel + macOS Apple Silicon. Rhino 8 Win modern .NET (.NET 10) load verified. net48 Rhino load deferred (owner on modern .NET). | [M0 report](#m0--fresh-solution-layout--tested-core-foundation--2026-10-31) |
| M1 | 20-component port-parity scaffold + manifest test | done | Win: build + conformance (vs real Culebra dump); Rhino 8 Win modern .NET shows all 20 components in the 6 subcategories. Mac not verified. | [M1 report](#m1--port-parity-scaffold-20-components--conformance--2026-10-31) |
| M2 | Spawn, Init, Move, Controller, Trail Data, Visual Settings, Creeper_Engine (straight motion, bounds, trails, Graphic/Geometry) | todo | | |
| M3 | Spatial grid, Flocking, Attraction, Repulsion, Connectivity | todo | | |
| M4 | Stigmergy, Multi Path Tracking I/II, Mesh Crawl I/II, children | todo | | |
| M5 | Creeper_ZombieEngine (background task, cancellation) | todo | | |
| M6 | Bundling, Bundling_Engine, Bundling_ZombieEngine | todo | | |
| M7 | Performance pass, zero-allocation proof, review checklist report | todo | | |
| M8 | Icons, Yak packages (rh8-any, rh7-any), install docs, examples | todo | | |

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
