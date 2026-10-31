# Particle Engine — progress and test log

Update this file at the end of every working session. It is the single source of current state. The spec (`BUILD_SPEC.md`) says what to build; this says where we are.

**Last updated:** (date)
**Current version:** 0.3.0 prototype (legacy) → 0.4.0 in progress
**Active milestone:** M0

## 1. Milestones

Status: `todo` · `doing` · `done` · `blocked`. A milestone is `done` only when every exit criterion in spec Section 14 is met and a milestone report exists below.

| M | Deliverable | Status | Hosts exercised | Report |
| --- | --- | --- | --- | --- |
| M0 | Retarget, solution layout, 3-OS CI, legacy tests green | todo | | |
| M1 | 20-component port-parity scaffold + manifest test | todo | | |
| M2 | Spawn, Init, Move, Controller, Trail Data, Visual Settings, Creeper_Engine (straight motion, bounds, trails, Graphic/Geometry) | todo | | |
| M3 | Spatial grid, Flocking, Attraction, Repulsion, Connectivity | todo | | |
| M4 | Stigmergy, Multi Path Tracking I/II, Mesh Crawl I/II, children | todo | | |
| M5 | Creeper_ZombieEngine (background task, cancellation) | todo | | |
| M6 | Bundling, Bundling_Engine, Bundling_ZombieEngine | todo | | |
| M7 | Performance pass, zero-allocation proof, review checklist report | todo | | |
| M8 | Icons, Yak packages (rh8-any, rh7-any), install docs, examples | todo | | |

## 2. Owner tasks (things only the owner can do)

- [ ] Put the 0.3.0 source in `legacy/` (see `legacy/README.md`).
- [ ] Confirm the interpretation calls in spec Section 1.3: I-1 both Multi Path Tracking versions · I-2 both Mesh Crawl versions · I-3 Rhino 7 best-effort alongside Rhino 8 · I-4 colour ports kept but ignored.
- [ ] Run the port-dump script (spec 12.1) in Rhino 7 with Culebra installed; save the JSON as `tests/ParticleEngine.Parity/culebra_ports.json` (raw) and tell Claude Code. **Do this before M1.**
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
| | Rhino 8 Win, modern .NET | | | | | | |
| | Rhino 8 Mac, Apple Silicon | | | | | | |
| | Rhino 8 Mac, Intel | | | | | | |
| | Rhino 7 Win | | | | | | |
| | Rhino 7 Mac | | | | | | |

## 5. CI and benchmarks

| Date | Commit | Windows | macOS Intel | macOS Apple Silicon | Notes |
| --- | --- | --- | --- | --- | --- |
| | | | | | |

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
| | | |

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
