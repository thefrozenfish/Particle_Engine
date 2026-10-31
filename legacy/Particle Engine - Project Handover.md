# Particle Engine — project history and handover

**Updated:** 31 October 2026  
**Current packaged version:** 0.3.0 prototype  
**Status:** Working implementation with automated core tests; Windows host testing underway; macOS host testing outstanding.

## 1. Objective and decisions

The project began by reviewing [Culebra.NET](https://www.complicitmatter.com/culebra-net/), an agent-based simulation plugin for Grasshopper.

The agreed objective is a new plugin that offers comparable setup, behaviours and simulation workflows in **Rhino 8 and later on Windows and macOS**.

Decisions made:

- A new plugin with equivalent behaviours is acceptable; loading existing Culebra definitions unchanged is not required.
- The plugin is named **Particle Engine** throughout. Assemblies and namespaces use `ParticleEngine` without spaces.
- Use a native C# implementation rather than retaining Culebra's Java/IKVM dependency chain.
- Prioritise live simulation with Run, Pause, Reset and Step.
- Subsequently add seeded particle setup, bounce/wrap boundaries, separate flocking controls and interactions between particle groups.
- Retain the original frame-slider simulation component alongside the live engine.

## 2. Research findings

Culebra's published documentation describes a Grasshopper assembly and C# wrapper around a Java behaviour library converted through IKVM. It lists flocking, wandering, noise, tracking, mesh crawling, attraction/repulsion, stigmergy, spawning and multi-object interactions.

The published 2.1 release targets Rhino 7 and earlier and documents a .NET Framework 4.0 build and Windows installation paths. Its public repository identifies a GPL-3.0 licence. No original Culebra code, binaries, icons or branding were incorporated into Particle Engine.

We did **not** establish that original Culebra is supported on macOS. The documentation reviewed does not confirm Mac support, and a Mac compatibility question on its forum did not provide confirmation in the material checked. This means **unverified**, not proven impossible.

RhinoCommon supports cross-platform plugin development. That architectural capability does not establish that our particular plugin works on a Mac; actual host testing is still required.

References:

- [Culebra documentation](https://www.complicitmatter.com/culebra-net/)
- [Culebra source repository](https://github.com/elQuixote/Culebra)
- [Culebra discussion group](https://www.grasshopper3d.com/group/culebra?overrideMobileRedirect=1)
- [Rhino runtime guidance](https://developer.rhino3d.com/guides/rhinocommon/moving-to-dotnet-core/)
- [Grasshopper scheduling API](https://developer.rhino3d.com/api/grasshopper/html/M_Grasshopper_Kernel_GH_Document_ScheduleSolution_1.htm)

## 3. Version history

| Version | Main work | Automated checks reported |
| --- | --- | --- |
| 0.1.0 | Simulation core, eight Grasshopper components, deterministic frame replay, trails, mesh constraint and capped child spawning | 15 tests passed |
| 0.2.0 | Live Engine, Behaviour Controller, pause/resume/reset/step, live behaviour replacement, stop-frame and scheduling logic | 25 tests passed |
| 0.3.0 | Seeded setup, bounds, groups, separate flock forces, pursuit/avoidance and group selection; 19 components total | 40 tests passed |

The user reported the initial plugin was working on Windows. A screenshot confirmed Rhino was running **.NET 8.0.15**. This was suitable for continuing testing without changing the runtime.

Subsequent generated example files exposed a saved-input issue described in section 8. Full acceptance of version 0.3 has not been established.

## 4. Current implementation

### Architecture

- **ParticleEngine.Core:** simulation math and state, behaviours, spawning, bounds, groups and playback controller.
- **ParticleEngine.Grasshopper:** components, Grasshopper data conversion, mesh constraint, document scheduling and output geometry.
- **ParticleEngine.Tests:** console-based regression tests for core behaviour and playback logic.

The core and plugin target **.NET Standard 2.1, AnyCPU**, intended for Rhino's modern .NET runtimes. Development used the installed .NET SDK **8.0.408** and Rhino 8 SDK assemblies on Windows.

The distribution contains `ParticleEngine.gha` and `ParticleEngine.Core.dll`. Rhino supplies RhinoCommon and Grasshopper. There is no Java or IKVM dependency and no custom Windows-only UI.

The newer source appends optional Groups and Bounds ports to the engines while retaining existing component GUIDs and original port positions. Migration of saved definitions still needs host testing.

### The 19 components

| Component | Function |
| --- | --- |
| Simulate | Reconstruct a seeded simulation up to an absolute frame |
| Live Engine | Incremental Run/Pause/Reset/Step simulation |
| Settings | Speed, force, timestep, trails, XY mode, spawn interval and population cap |
| Behaviour Controller | Combine behaviour groups into one ordered list |
| Flock | Combined separation, alignment and cohesion |
| Wander | Seeded directional jitter |
| Noise | Smooth seeded value-noise vector field |
| Point Force | Attraction or repulsion with distance falloff |
| Follow Path | Predictive following of ordered polyline points |
| Follow Trails | Simple stigmergy using other particles' recent trails |
| Spawn Box | Seeded starting points, matching group IDs and a domain box |
| Boundaries | Bounce or wrap inside world-axis-aligned bounds |
| Assign Group | Attach one group ID to each point in a starting list |
| Separation | Independent separation radius, strength, view angle and group filters |
| Alignment | Independent alignment radius, strength, view angle and group filters |
| Cohesion | Independent cohesion radius, strength, view angle and group filters |
| Pursuit | Predictively chase the nearest eligible target-group particle |
| Avoidance | Predictively steer away from a target-group particle |
| Select Group | Extract current particles by group and return their list indices |

### Simulation semantics

- Forces are calculated from a shared snapshot of the previous frame, summed, and capped by the force and speed settings.
- The same inputs, ordered behaviours and seed reproduce the same sequence in the tested environment. Bit-for-bit agreement between operating systems is unverified.
- XY mode flattens motion to world Z = 0.
- Trails are bounded by the configured trail length.
- Original particles can spawn children at a configured interval up to a population cap. Children do not reproduce and inherit their parent's group ID.
- Wrapping preserves velocity but clears that particle's stored trail to avoid drawing a line across the domain. Earlier trail segments are not retained.
- Bounce reflects position and velocity, including steps that cross multiple box widths.
- Optional mesh crawling projects positions to the closest mesh point and velocities to the tangent plane. Bounds and mesh crawling cannot be combined in the same engine.

### Live controls

- **Run True:** advance continuously; **Run False:** preserve the current state.
- **Reset:** rebuild at frame zero from the current initial setup. Reset takes priority over Run and Step.
- **Step:** advance one frame per button press while paused.
- **Interval:** delay between updates, not a guaranteed frame rate.
- **Steps Per Tick:** frames advanced in each running update.
- **Stop Frame:** stop at an absolute frame without overshooting a batch.
- Behaviour changes affect future frames without replaying history.
- Changes to starting points, Groups, Bounds, Seed, Settings, initial velocities or Crawl Mesh require Reset.
- Switching documents stops scheduling but is intended to preserve in-memory state. Closing/reloading the definition discards the live state.
- Live state is not saved as a checkpoint; save with Run False and expect frame zero on reopening.

## 5. Testing completed and its limits

### Completed

- Windows Release builds succeeded.
- Forty automated core tests passed, covering integration, determinism, planar motion, force/speed caps, trails, spawning, paths, point forces, playback, bounds, groups and interactions.
- Rhino's static compatibility scanner completed for both assemblies without reporting incompatible APIs.
- The 19 component GUIDs were checked for uniqueness.
- The user reported the early Windows plugin working under Rhino .NET 8.0.15.

The build retains one **MSB3277 System.Net.Http reference-version warning** from the Rhino SDK/.NET Standard combination. Particle Engine does not use HTTP APIs.

### Not established

- Full Windows host acceptance of all 0.3 components, scheduling, document lifecycle and viewport behaviour.
- Actual mesh crawling through the Rhino geometry kernel in the test suite.
- Saved-definition upgrades after new ports were appended.
- macOS installation and execution, including Apple Silicon and Intel separately if required.
- Execution under the original Rhino 8 .NET 7 runtime or later Rhino hosts.
- Production-scale performance, collision avoidance guarantees or complete Culebra parity.
- Remote CI execution. A Windows/macOS core-test workflow was prepared but not published or run remotely.

## 6. Installation and test guidance already provided

1. In Grasshopper, locate **File > Special Folders > Components Folder**.
2. Save work and close all Rhino instances.
3. Extract the chosen prototype package; unblock the ZIP in Windows Properties if needed.
4. Replace both `ParticleEngine.gha` and `ParticleEngine.Core.dll` together. Do not leave duplicate installations in separate component folders.
5. Restart Rhino and Grasshopper.

The basic acceptance sequence is Reset → Step → Run → Pause → Resume → Reset, followed by a stop-frame test and behaviour edits while running.

## 7. Grasshopper example files created

Two Culebra-inspired workflows were created using Particle Engine 0.3:

1. **01 - Bounded Flock:** 30 XY particles; separation, alignment, cohesion and wander; bounce/wrap boundaries.
2. **02 - Chase and Avoid:** group 0 contains eight pursuers; group 1 contains twenty avoiding particles; both receive separation and wander.

Both have grouped canvas layouts, embedded starting data, instruction panels, playback controls, and frame/status outputs. They do not require a separate Rhino model or the original Culebra plugin. They mirror general workflows, not exact copies of Culebra's sample files.

Both binary `.gh` and XML `.ghx` versions were produced. A direct background Rhino startup failed, and a computer-use approval prompt timed out. Generation therefore used Grasshopper's component serialization and GH_IO archive library without a full running document.

Checks performed:

- Component data could be read into fresh component instances.
- Both archive formats could be read back.
- The bounded flock had 35 objects and 24 valid wire references.
- The chase/avoid example had 40 objects and 31 valid wire references.
- Source counts matched saved wire counts; Run was saved as False.

**These checks did not verify the effective values produced by the whole Grasshopper graph. The user subsequently found a real example-file defect.**

## 8. Known example-file defect — important current issue

The user opened the bounded flock and reported:

> Supply one value for each playback control, Settings and Crawl Mesh.

> Live Engine needs one data branch. Flatten P, B and V, and supply one value for each playback control.

Inspection of the generated XML found that the example builder had saved **both component defaults and intended overrides** in several persistent inputs. For example:

- Settings Trail Length: `100` and `80`.
- Settings XY Only: `False` and `True`.
- Spawn Box XY Only: `False` and `True`.
- Separation Radius: `10` and `5`.

The builder used `SetPersistentData` without first clearing existing defaults. In this usage, that left multiple saved values, causing repeated upstream evaluation and multiple values/branches reaching the live engine. The engine's cardinality guards then raised the errors.

### User instruction and current status

The user explicitly said: **“dont regen just tell me how o fix”**.

Accordingly:

- No corrected examples were regenerated after this discovery.
- No plugin rebuild or installation was performed for this issue.
- Manual repair instructions were provided.
- The user has not yet confirmed the repair worked.
- Existing example files and the example ZIP must still be treated as containing the duplicate-default defect. The same builder created both examples, so the issue is not limited to the bounded flock.

### Manual fix provided for the bounded flock

Connect a single-value slider or Boolean Toggle to each input below. Connected values override the duplicated saved values.

| Component | Input | Value |
| --- | --- | --- |
| Settings | Trail Length | Integer 80 |
| Settings | XY Only | True |
| Settings | Particle Limit | Integer 100 |
| Spawn Box | XY Only | True |
| Spawn Box | Group | Integer 0 |
| Separation | Radius | 5 |
| Separation | Strength | 1.5 |
| Alignment | Radius | 12 |
| Cohesion | Radius | 18 |
| Cohesion | Strength | 0.8 |

Keep the existing Wander Strength slider connected; it already overrides its duplicate values. Flatten Live Engine P, B, V and G. Set Run False, press Reset, then set Run True.

This repair does not require reinstalling the plugin. Flattening alone is insufficient: the duplicated scalar inputs must first be overridden or reduced to one value.

For future regeneration, explicitly clear each input's persistent data before setting its replacement and add regression checks for the number of stored values in every scalar input. Do not regenerate now without a new user request.

## 9. Deliverables and source locations

Paths below are relative to this handover file's directory (`outputs`).

### Packages

- [0.1 prototype](Particle%20Engine%200.1.0%20prototype.zip) and [0.1 source](Particle%20Engine%200.1.0%20source.zip)
- [0.2 prototype](Particle%20Engine%200.2.0%20prototype.zip) and [0.2 source](Particle%20Engine%200.2.0%20source.zip)
- [0.3 prototype](Particle%20Engine%200.3.0%20prototype.zip) and [0.3 source](Particle%20Engine%200.3.0%20source.zip)
- [Grasshopper examples ZIP — known defect above](Particle%20Engine%20-%20Grasshopper%20Examples.zip)

### Current working source and documentation

- `Particle Engine/src/ParticleEngine.Core/`
- `Particle Engine/src/ParticleEngine.Grasshopper/`
- `Particle Engine/tests/ParticleEngine.Tests/`
- `Particle Engine/.github/workflows/core-tests.yml`
- [README](Particle%20Engine/README.md)
- [Wiring recipes](Particle%20Engine/EXAMPLES.md)
- [Earlier validation report](Particle%20Engine/VALIDATION.md)
- `Particle Engine/dist/Particle Engine/` — latest staged plugin binaries.

### Example files

- [Bounded Flock .gh](Particle%20Engine%20Examples/01%20-%20Bounded%20Flock.gh)
- [Chase and Avoid .gh](Particle%20Engine%20Examples/02%20-%20Chase%20and%20Avoid.gh)
- Matching `.ghx` versions and `START HERE.txt` are in the same folder.

The example-generation utility is in the workspace's `work/example-builder/` directory. It is an intermediate development tool, not a plugin dependency. Its persistent-input assignment helpers are the source of the known defect.

The earlier packaged README and validation notes predate the discovery of the duplicate defaults. **This handover is the current record of that issue.**

## 10. Remaining scope and sensible next steps

1. Confirm the user's manual bounded-flock repair works before adding more features.
2. Test actual Run/Pause/Reset/Step, stop-frame, source matching and viewport output in Rhino.
3. When requested, repair the example builder, validate scalar defaults, regenerate both examples and test them as complete Grasshopper documents.
4. Test installation and playback on a Mac; do not describe Mac compatibility as verified before then.
5. Test saved-definition migration, document switching, locking, deletion and reopening.
6. Consider remaining Culebra-style features: per-group motion settings, group filtering of older behaviours, mesh-colour/image-driven weights, more complete mesh/path following, and specialised interactions.
7. Improve icons, ready-made examples, packaging and performance after stabilising the workflows.

Other current limitations include value noise rather than exact Culebra noise parity, incomplete closed-path seam handling, closest-point mesh projection, expensive trail following, no collision guarantees and no saved live-state checkpoints. A distribution licence for the new project has not yet been selected.

## 11. Handover summary

Particle Engine 0.3 is a new 19-component Grasshopper prototype with a tested simulation core, two engine modes, particle groups, boundary controls and group interactions. The initial Windows implementation was reported working. The latest concrete blocker is a **generated example-data defect**, for which a manual repair has been explained but not yet confirmed. No Mac host test has been completed, and no exact Culebra compatibility or full feature parity is claimed.
