# Particle Engine — Build Specification (Culebra-parity subset)

**Audience:** the implementing assistant (ChatGPT) and the project owner (Abhinav Chaudhary)
**Date:** 31 October 2026
**Supersedes:** the "remaining scope" in `Particle Engine - Project Handover.md` (0.3.0 prototype). The handover remains the record of what exists.
**Target version:** 0.4.0 (port-parity rebuild)

---

## 0. How to work from this document

### 0.1 Evidence tags

Every statement below carries a confidence tag. Do not silently upgrade a tag.

| Tag | Meaning |
| --- | --- |
| `[DOC]` | Taken from public community documentation of Culebra (grasshopperdocs.com, Culebra 2.0-Beta listing). The installed Culebra 2.1 may differ in small ways. |
| `[HO]` | Taken from the project handover. |
| `[DECIDED]` | A decision made in this spec. Follow it unless the owner overrides it. |
| `[VERIFY]` | Unknown. The original plugin must be probed (Section 12) before this is treated as final. Implement the stated interim behaviour, mark it clearly in code, and keep it easy to change. |

### 0.2 Ground rules (non-negotiable)

1. **Port parity is the contract.** Component names, nicknames, categories, input/output order, port names, port nicknames, data types and access (item/list) must match Section 6. A conformance test (Section 11.1) fails the build if they drift.
2. **Behavioural parity is best-effort and evidence-based.** No Culebra source is available and none may be copied (Section 13). Where behaviour is unknown, use the interim rule, tag it `[VERIFY]`, and list it in the milestone report.
3. **Never claim a platform works without a log from that platform.** "Compiles" ≠ "loads" ≠ "runs correctly". Say which of the three has been shown, and on which host.
4. **No allocation in the per-frame hot path.** See Section 9.
5. **Never reorder, remove or retype a port after a version is shared.** Append only (Grasshopper restores parameters by position). New GUIDs are used for this rebuild; do not reuse the 0.1–0.3 GUIDs.
6. **Deliver in the milestones of Section 14.** Each milestone report lists: files changed, tests run and results (with OS/runtime), deviations from this spec, and new `[VERIFY]` items.
7. **Review your own output** against the checklist in Section 9.4 before presenting it, and say what you found.

### 0.3 Honest limits of this document

- The performance and logic review in Section 3 is **design-level**, made from the handover text. The 0.3 source code was not available to the author of this spec. Run the Section 9.4 checklist against the actual source and report findings; do not assume the 0.3 code is clean.
- The Culebra port tables come from community documentation of a 2.0-Beta listing. Section 12 describes how to extract ground truth from the owner's installed copy.

---

## 1. Goal and scope

Build **Particle Engine**: a native C# Grasshopper plugin that reproduces a chosen subset of Culebra.NET's components and behaviour, running on **Rhino 8 (Windows and macOS, Intel and Apple Silicon)** and, as a secondary target, **Rhino 7 (Windows and macOS)**.

### 1.1 In scope (owner's list)

| Group | Included components |
| --- | --- |
| Spawn Types | Box Spawn, Point Spawn |
| Initialize | Init Settings, Move Settings |
| Behaviors | Controller, Flocking, Stigmergy, Multi Path Tracking (+ II), Mesh Crawl (+ II) |
| Forces | Attraction Force, Repulsion Force, Bundling |
| Engines | Creeper_Engine, Creeper_ZombieEngine, Bundling_Engine, **Bundling_ZombieEngine (new)** |
| Display | Trail Data, Visual Settings (Graphic mode and Geometry mode) |

Total: **20 components** (19 that exist in Culebra, counting Multi Path Tracking II and Mesh Crawl II separately, plus 1 new: Bundling_ZombieEngine).

### 1.2 Out of scope

Wandering, Weaving Wandering, Noise, Separation (standalone), all `*Mapped` components (they need image/colour sampling), Bundling II, Creeper_Engine_Dual, Disco_Color, Gradient_Color, Graphic_Polyline, texture particles.

`[DECIDED]` Do not delete working 0.3 core code for out-of-scope behaviours. Leave it in Core, unit-tested, with **no component registered**. Do not expose them in the UI.

### 1.3 Interpretation calls the owner should confirm

| # | Call | Default taken |
| --- | --- | --- |
| I-1 | "Multi-path tracking" means both *Multi Path Tracking* and *Multi Path Tracking II* | Include both. II is a superset (adds Trigger Spawn, Max Children). |
| I-2 | "Mesh tracking" means *Mesh Crawl* (+ *Mesh Crawl II*) | Include both. |
| I-3 | "Mac OS old and new" means older and newer **macOS versions and both Intel/Apple Silicon**, which in practice means Rhino 7 *and* Rhino 8 | Rhino 8 primary; Rhino 7 best-effort (Section 2). |
| I-4 | "Colour data" is dropped | Visual Settings keeps the **Color Data** and **Apply Texture** ports for wiring parity but ignores them (Section 6.6). |

---

## 2. Platform and build targets

### 2.1 Support matrix

| Host | OS | Runtime used by Rhino | Assembly built for it | Tier |
| --- | --- | --- | --- | --- |
| Rhino 8, Windows | Windows 10/11 x64 | .NET Framework 4.8 **or** modern .NET (user-switchable) | `net48` and `net7.0` | 1 |
| Rhino 8, macOS | macOS 12.4 Monterey or later (Intel and Apple Silicon); check rhino3d.com/8/system-requirements for the current ceiling | modern .NET only | `net7.0` | 1 |
| Rhino 7, Windows | Windows 8.1/10/11 x64 | .NET Framework 4.8 | `net48` (compiled against RhinoCommon 7) | 2 |
| Rhino 7, macOS | macOS 10.14 Mojave to 14 Sonoma (Intel and Apple Silicon) | Mono | `net48` assembly (same as Rhino 7 Windows) | 2 |

`[DOC]` Rhino 8 Mac uses modern .NET only; a multi-targeted plugin runs `net7.0` on Mac, while Windows can use either runtime. Rhino 7 packages must be single-target.
Support lists for Rhino 8's macOS range differ between sources. Treat the matrix as the design target and confirm against McNeel's current page.

### 2.2 Target frameworks `[DECIDED]`

| Project | TargetFrameworks | Why |
| --- | --- | --- |
| `ParticleEngine.Core` | `netstandard2.0` | Loads in .NET Framework 4.8, Mono and modern .NET. |
| `ParticleEngine.Grasshopper` | `net48;net7.0` | McNeel's documented multi-target for Rhino 8; `net48` also serves Rhino 7. |
| `ParticleEngine.Tests` | `net8.0` (all OSes) plus `net48` (Windows) | Exercises Core on both runtime families. |

**Defect in 0.3 `[HO]`:** Core and plugin target `netstandard2.1`. .NET Framework 4.8 tops out at `netstandard2.0`, so 0.3 cannot load on Rhino 7 for Windows or on Rhino 8 for Windows when it runs the .NET Framework runtime. Rhino 7 for Mac runs on Mono, whose `netstandard2.1` support depends on the Mono version Rhino bundles; do not rely on it. 0.3 worked in testing only because the owner's Rhino was running .NET 8. **Retarget to the table above.**

Reference packages (compile only, never copied to output):
- `net48` → `RhinoCommon` and `Grasshopper` **7.x** packages (lowest supported API; this also stops accidental use of Rhino 8-only APIs).
- `net7.0` → `RhinoCommon` and `Grasshopper` **8.x** packages.
- Mark both `ExcludeAssets="runtime"` / `Private=false`. This also removes the MSB3277 `System.Net.Http` warning noted in the handover.
- `net48` only: add `System.Memory` and `System.Buffers` if `Span<T>`/`ArrayPool<T>` are used. No other third-party packages.
- Use McNeel's official `dotnet new` Rhino/Grasshopper templates to scaffold; confirm the current package name.
- `LangVersion` 10. Anything that compiles for both `net48` and `net7.0` is safe on Mono.

### 2.3 Solution layout

```
ParticleEngine.sln
src/
  ParticleEngine.Core/            netstandard2.0, NO RhinoCommon reference
  ParticleEngine.Grasshopper/     net48;net7.0, TargetExt .gha
tests/
  ParticleEngine.Tests/           console/xUnit-or-NUnit, Core only
  ParticleEngine.Parity/           conformance manifest + probe fixtures (Section 11–12)
bench/
  ParticleEngine.Bench/           Stopwatch benchmarks (Section 9.5)
packaging/
  manifest.yml, build scripts
```

`[DECIDED]` **Core never references RhinoCommon.** It works on `double[]` and small structs. Anything needing Rhino geometry crosses an interface implemented in the Grasshopper project:

| Core interface | Implemented in GH layer with |
| --- | --- |
| `IMeshSurface` (closest point + normal, optional bounded search) | `Rhino.Geometry.Mesh.ClosestMeshPoint` |
| curve → point-array conversion | `Curve.TryGetPolyline`, `Curve.ToPolyline`, `DivideByCount` |

This keeps Core unit-testable on any OS without a Rhino licence.

### 2.4 Packaging

- **Primary channel: Yak package.** Multi-target layout for Rhino 8: `manifest.yml` at the package root, with `net48/` and `net7.0/` folders beside it, tag `rh8-any`. Rhino picks the folder by runtime and platform.
- **Second package for Rhino 7:** single-target, `net48` files at the root, tag `rh7-any`.
- Each folder contains `ParticleEngine.gha`, `ParticleEngine.Core.dll` and nothing else. Never ship RhinoCommon, Grasshopper or GH_IO.
- Manual install (fallback): both files together in **File > Special Folders > Components Folder**, one install location only. On Windows unblock the downloaded ZIP; on macOS, if Rhino reports a blocked load, clear the quarantine attribute on the extracted files.
- Check where Rhino 8 on Windows actually unpacks a multi-target Yak install; a McNeel forum report shows a flattened folder of DLLs. Confirm the right runtime's assemblies are the ones loaded.

### 2.5 Cross-platform rules for the GH layer

- No `System.Windows.Forms` dialogs, no registry, no `Environment.SpecialFolder` assumptions, no hard-coded path separators, no native P/Invoke.
- Icons: 24×24 PNG embedded resources. `System.Drawing.Bitmap` is acceptable (Rhino 8 Mac provides the System.Drawing shim). Add icons in the last milestone.
- Viewport drawing only through `DisplayPipeline` (`DrawLines`, `DrawPoints`, `DrawPolyline`). No shaders, no GDI.
- Settings that need UI go in right-click menu items (`AppendAdditionalComponentMenuItems`), which behave the same on both platforms.

---

## 3. Review of the 0.3 design (what to fix before building)

Severity: **B** blocker, **H** high, **M** medium, **L** low. Items marked *(unverified)* are inferred from the handover text only.

| # | Sev | Finding | Required action |
| --- | --- | --- | --- |
| R1 | B | `netstandard2.1` target cannot load on .NET Framework 4.8 (Rhino 7 Win, Rhino 8 Win in .NET Framework mode); Mono support is uncertain. | Section 2.2. |
| R2 | H | Only one runtime was ever tested. Rhino 8 on Windows can run either runtime; Mac runs modern .NET only. | Test matrix, Section 11.3. |
| R3 | H | Example builder used `SetPersistentData` without clearing, leaving duplicate defaults (e.g. Trail Length `100` and `80`) and causing "supply one value" errors. | Never write defaults outside component code. If any tool writes `.gh` files, call `PersistentData.Clear()` first and add a test that every scalar input holds ≤ 1 stored value. |
| R4 | H | Engines raised errors on any multi-value or multi-branch input (cardinality guards). A stateful engine run once per iteration would advance several frames per solution. | Declare engine inputs `GH_ParamAccess.item`. In `SolveInstance`, `if (DA.Iteration > 0) { AddRuntimeMessage(Warning, ...); return; }` so the engine advances **exactly once per solution**. Do not hard-fail upstream. |
| R5 | H | *(unverified)* No spatial index is described. Flocking is O(N²); trail following is O(N²·T). | Mandatory uniform-grid neighbour search, Section 9.2. |
| R6 | M | "Bounds and mesh crawling cannot be combined." Culebra's Init Settings always carries a boundary mode. | Remove the restriction. Order: integrate → boundary → mesh projection. |
| R7 | M | *(unverified)* Trails "bounded by length". If stored as `List<T>` with `RemoveAt(0)` this is O(n) per frame. | Fixed-capacity ring buffers, Section 9.3. |
| R8 | M | `System.Random` sequences are not guaranteed identical across .NET Framework, Mono and modern .NET. The handover leaves cross-OS determinism "unverified". | Own PRNG (PCG32 or xoshiro128**) in Core. Same seed → same stream on every runtime. |
| R9 | M | View-angle tests via `Math.Acos`. | Compare dot products against a per-frame precomputed `cos(VA/2)`. No trig in the hot loop. |
| R10 | M | `Math.Sin/Cos/Atan2/Pow` can differ in the last bits between platforms. | Tests use tolerances (e.g. 1e-9 relative), never bit equality across OSes. Prefer algebra over trig. |
| R11 | M | Component set (19) does not match Culebra's structure; Live Engine's Run/Pause/Step differ from Culebra's Timer + Reset engines. | Rebuild components to Section 6. Keep Live Engine's self-scheduling code as an *optional* right-click feature (Section 7.3). |
| R12 | M | Wrap boundary preserved velocity and cleared the trail. Culebra's boundary modes are Bounce / Respawn / Do Nothing. | Replace with the three Culebra modes, Section 7.6. |
| R13 | M | Trail output built for every particle every solve. | Build geometry only in Geometry mode, only after a frame advanced, Section 7.9. |
| R14 | M | Children "capped" by a population limit only. Max Children × parents can still explode memory. | Add a hard global cap (default 50 000) with a warning, Section 7.7. |
| R15 | L | 40 tests, all run on Windows; remote CI prepared but never executed. | CI on Windows, macOS Intel and macOS Apple Silicon, Section 11.2. |
| R16 | L | No icons, packaging via ZIP only. | Yak + icons in final milestone. |

---

## 4. Architecture

### 4.1 Layers

```
Grasshopper layer  (components, Goo types, display, scheduling, Rhino geometry glue)
        │  immutable settings objects + IMeshSurface + point arrays
Core layer         (simulation state, behaviours, forces, spatial grids, PRNG)
```

### 4.2 Data contracts between components `[DECIDED]`

Culebra's "Generic Data" ports carry opaque objects. Implement each as an **immutable** class wrapped in a `GH_Goo<T>` subclass whose `Duplicate()` returns itself. Immutability makes it safe for Grasshopper to share, cache and copy them, and lets a running engine pick up a changed upstream object just by reading the new reference.

| Goo type | Produced by | Consumed by |
| --- | --- | --- |
| `SpawnSettings` | Box Spawn, Point Spawn | Init Settings |
| `InitSettings` | Init Settings | Creeper_Engine, Creeper_ZombieEngine |
| `MoveSettings` | Move Settings | Controller |
| `BehaviorSpec` (one subtype per behaviour/force) | Flocking, Stigmergy, Path Tracking, Mesh Crawl, Attraction, Repulsion, Bundling | Controller |
| `BehaviorData` | Controller | All four engines (Behavioral Settings port) |
| `TrailDataSettings` | Trail Data | Visual Settings |
| `VisualSettings` | Visual Settings | All four engines |

Type-hint every Generic Data port with a descriptive `TypeName`, and give each Goo a clear `ToString()` so wiring errors are readable in the panel.

### 4.3 Simulation state (Core)

Struct-of-arrays, double precision (CAD coordinates can be large; `float` positions lose precision far from the origin):

```
double[] px, py, pz, vx, vy, vz;       // front buffers (read this frame)
double[] qx, qy, qz, ux, uy, uz;       // back buffers (written this frame)
int[]    parent;                        // -1 for originals
int[]    childCount;
double[] trail;                         // ring buffers, N * cap * 3
int[]    trailHead, trailLen;
```

Swap front/back after each step. Grow arrays by doubling; never per-particle objects.

### 4.4 Engine lifecycle

- State lives in the component instance, never in statics.
- Clear and release state in `RemovedFromDocument`, `DocumentContextChanged` (for close), and when the component is locked/disabled.
- Do not serialise live state. On file open, the first solve initialises frame 0.
- Initialise automatically on first solve; re-initialise when **Reset** is true.
- `InitSettings` changing mid-run does **not** auto-reset `[VERIFY]` (Culebra's Reset doc says it "resets all fields", implying reset is explicit). `BehaviorData` and `VisualSettings` changes take effect on the next frame.

---

## 5. Category structure `[DOC]`

Tab: **Particle Engine**. Subcategories, named and ordered exactly:

`01 | Spawn Types` · `02 | Initialize` · `03 | Behaviors` · `04 | Forces` · `05 | Engine` · `06 | Display`

Exposure order within a subcategory follows the order of the tables in Section 6.

---

## 6. Component specification

Column key: **Nick** = port nickname; **Acc** = access (item / list / tree); **Fallback** = value used when the port is unconnected *until the real default is confirmed* `[VERIFY]`.
"Generic" means the port uses a generic parameter that accepts the Goo type shown or a raw GH value where noted.
Port names and nicknames are `[DOC]`. Types and access are `[DOC]` where stated, otherwise `[DECIDED]`. Real defaults `[VERIFY]` throughout.

### 6.1 Spawn Types

#### Box Spawn — nick `BS` — description: "Uses a box to contain the creepers spawn area in 2D or 3D"

| In/Out | Name | Nick | Type | Acc | Notes | Fallback |
| --- | --- | --- | --- | --- | --- | --- |
| In | Box | B | Box | item | Spawn volume. | World unit box, origin-centred |
| In | Spawn Location | S | Integer (value list 0/1) | item | `[DOC]` 0 and 1 choose between two spawn distributions; the public text is truncated and ambiguous. `[VERIFY]` P12 | 1 |
| In | Spawn Count | CC | Integer | item | Number of creepers at start. | 100 |
| Out | Spawn Settings | SS | `SpawnSettings` | item | | |

Interim spawn semantics `[VERIFY]` P12: **1** = uniform random inside the box (3D) / on the box's base rectangle (2D); **0** = regular grid fill of the box (3D) / of the base rectangle (2D). Use the engine PRNG, not `System.Random`.

#### Point Spawn — nick `PS` — description: "Uses a list of points as starting positions for the creepers in 2D or 3D"

| In/Out | Name | Nick | Type | Acc | Notes | Fallback |
| --- | --- | --- | --- | --- | --- | --- |
| In | Points | P | Point | list | One creeper per point. | none (error) |
| In | Box | B | Box | item | Bounding volume the creepers bounce off (used by the boundary mode). | box around points, padded |
| Out | Spawn Settings | SS | `SpawnSettings` | item | | |

### 6.2 Initialize

#### Init Settings — nick `IS`

| In/Out | Name | Nick | Type | Acc | Notes | Fallback |
| --- | --- | --- | --- | --- | --- | --- |
| In | Spawn Settings | ST | Generic (`SpawnSettings`) | item | | none (error) |
| In | Dimension | D | Generic (integer) | item | `[DOC]` 0 = 2D, 1 = 3D | 1 |
| In | Boundary | B | Generic (integer) | item | `[DOC]` 0 = Bounce, 1 = Respawn, 2 = Do Nothing | 0 |
| Out | Init Settings | IS | `InitSettings` | item | | |

Add value-list-style hints in the port descriptions. Reject values outside the allowed integers with a warning and use the fallback.

#### Move Settings — nick `MS`

| In/Out | Name | Nick | Type | Acc | Notes | Fallback |
| --- | --- | --- | --- | --- | --- | --- |
| In | Initial Speed\|\|Vector | IS | Generic (number or vector) | item | `[DOC]` Initial speed as a scalar or a vector. A scalar means a random direction of that length `[VERIFY]` P1. | 1 |
| In | Max Speed | MS | Generic (number) | item | | 5 |
| In | Max Force | MF | Generic (number) | item | | 0.5 |
| In | Velocity Multiplier | VM | Generic (number) | item | | 1 |
| Out | Move Settings | MS | `MoveSettings` | item | | |

`[DOC]` The Move Settings output is **not** wired to an engine directly (engines have no such port). It feeds the **Controller**. Document this in the port tooltip.

### 6.3 Behaviors

#### Controller — nick `BC` — "Behavior Merging Controller, you can add/remove/rearrange behaviors. The input order will be the behavior execution stack"

| In/Out | Name | Nick | Type | Acc | Notes |
| --- | --- | --- | --- | --- | --- |
| In | Behaviors (variable) | B… | Generic | list | `[VERIFY]` The public page lists no inputs. Implement as a **variable-parameter component** (zoomable + / − UI via `IGH_VariableParameterComponent`) so users can add and reorder inputs. Each input accepts any behaviour, force or Move Settings Goo. |
| Out | Behavior Data Object | BDO | `BehaviorData` | item | The ordered stack. |

Rules:
- Execution order = input order, then list order within an input `[DOC]`.
- Exactly zero or one `MoveSettings` is expected. If several arrive, use the first and warn. If none arrives, use the fallbacks of Move Settings.
- The Controller also accepts forces (Attraction, Repulsion) and Bundling, treating them as entries in the same stack. `[DOC]` shows the Controller feeding every engine's Behavioral Settings port, including the Bundling engine's, and describes Attract/Repel as "force methods inside the controller class"; that forces are wired into the Controller is an inference. `[VERIFY]`
- Output is a new immutable `BehaviorData` each solve; it must compare equal-by-content cheaply (store a content hash) so engines can detect "unchanged" and skip rebuilding caches.

#### Flocking — nick `FL` — "Flocking Algorithm"

| In/Out | Name | Nick | Type | Acc | Notes | Fallback |
| --- | --- | --- | --- | --- | --- | --- |
| In | Connect | C | Boolean | item | True = draw connections between heads and neighbours; the original warns this "dramatically reduces performance". | false |
| In | View Angle | VA | Number | item | Degrees. | 270 |
| In | Search Radius | SR | Number | item | Distance each creeper can see. | 10 |
| In | Align Value | AV | Number | item | Alignment weight. | 1 |
| In | Separation Value | SV | Number | item | Separation weight. | 1.5 |
| In | Cohesion Value | CV | Number | item | Cohesion weight. | 1 |
| Out | Flocking Behavior | FB | `FlockingSpec` | item | | |

#### Stigmergy — nick `ST` — "2D/3D Trail Chasing Algorithm – Agents will chase agents trails"

| In/Out | Name | Nick | Type | Acc | Notes | Fallback |
| --- | --- | --- | --- | --- | --- | --- |
| In | View Angle | VA | Number | item | Degrees. | 270 |
| In | Cohesion Mag | CM | Number | item | Steer towards average trail position. | 1 |
| In | Cohesion Range | CR | Number | item | Trail points within this range enable the cohesion magnitude. | 20 |
| In | Separation Mag | SM | Number | item | Avoid crowding on a trail. | 1.5 |
| In | Separation Range | SR | Number | item | Trail points within this range enable the separation magnitude. | 5 |
| Out | Stigmergy Behavior | SB | `StigmergySpec` | item | | |

`[DOC]` The original recommends **Trail Step 6** and **Max Trail Size 100** when using stigmergy. Stigmergy reads other creepers' stored trails, so **trail storage must be active whenever a Stigmergy behaviour is in the stack, regardless of the Trail Data "visibility" toggle** `[VERIFY]` P5. If no Trail Data is connected, use Trail Step 6, Max 100 for storage only.

#### Multi Path Tracking — nick `T` — "Multi Path Following Algorithm"

| In/Out | Name | Nick | Type | Acc | Notes | Fallback |
| --- | --- | --- | --- | --- | --- | --- |
| In | Polylines | P | Curve | list | Paths to follow. `[DOC]` "Polyline resolution is important, keep as low as possible." | none (error) |
| In | Polyline Threshold | PT | Number | item | Distance within which an agent can "see" a path. | 20 |
| In | Projection Distance | PD | Number | item | Look-ahead distance along the velocity for the predicted position. | 10 |
| In | Polyline Radius | PR | Number | item | Radius of the path corridor. | 2 |
| Out | Tracking Behavior | TB | `PathTrackingSpec` | item | | |

#### Multi Path Tracking II — nick `TT`

As above, plus two inputs **appended after Polyline Radius**:

| In | Trigger Spawn | TS | Boolean | item | Allows creepers to spawn children. | false |
| In | Max Children | MC | Integer | item | Maximum children per creeper. | 1 |

Output: Tracking Behavior (TB).

#### Mesh Crawl — nick `MC` — "Mesh Crawling allows agent to move along a mesh object"

| In/Out | Name | Nick | Type | Acc | Notes | Fallback |
| --- | --- | --- | --- | --- | --- | --- |
| In | Mesh | M | Mesh | item | | none (error) |
| In | Mesh Threshold | MT | Number | item | Minimum distance from the mesh before an agent steers back towards it. | 0.1 |
| In | Mesh Projection Distance | MPD | Number | item | Distance the current position is projected along the current velocity before looking up the closest mesh point. | 5 |
| Out | Mesh Crawling Behavior | MCB | `MeshCrawlSpec` | item | | |

#### Mesh Crawl II — nick `MC`

Mesh Crawl plus appended **Trigger Spawn (TS, Boolean)** and **Max Children (MC, Integer)**. Output: Mesh Crawling Behavior (MCB).

### 6.4 Forces

#### Attraction Force — nick `AF` — "Attracts a object towards a set of targets. Differs from Seek"

| In/Out | Name | Nick | Type | Acc | Notes | Fallback |
| --- | --- | --- | --- | --- | --- | --- |
| In | Targets | T | Point | list | | none (error) |
| In | Thresholds | TH | Number | list | One per target; a single value applies to all (standard longest-list). | 10 |
| In | Attraction Value | AV | Number | item | Magnitude. | 1 |
| In | Max Attraction | MA | Number | item | Upper limit. | 5 |
| Out | Attraction Force | AB | `AttractionSpec` | item | | |
| Out | Attraction Threshold Viz | V | Circle | list | One circle per target at its threshold radius, for display. | |

#### Repulsion Force — nick `RF` — "Repels a object away from a set of targets"

| In/Out | Name | Nick | Type | Acc | Notes | Fallback |
| --- | --- | --- | --- | --- | --- | --- |
| In | Targets | T | Point | list | | none (error) |
| In | Thresholds | TH | Number | list | | 10 |
| In | Repel Value | AV | Number | item | Magnitude. (The nickname is `AV` in the original; keep it.) | 1 |
| In | Max Repel | MA | Number | item | | 5 |
| Out | Repulsion Force | AB | `RepulsionSpec` | item | | |
| Out | Repulsion Threshold Viz | V | Circle | list | | |

Interim force law `[VERIFY]` P2/P3: for a target at distance `d < threshold`, force magnitude `min(Value × (1 − d/threshold), Max)` directed towards (attraction) or away from (repulsion) the target; zero outside the threshold. Circles for the Viz output: plane = world XY at the target, or the XY/XZ plane according to Dimension `[VERIFY]`.

#### Bundling — nick `B` — "Settings for Self Organization of Curve Networks"

| In/Out | Name | Nick | Type | Acc | Notes | Fallback |
| --- | --- | --- | --- | --- | --- | --- |
| In | Threshold | T | Number | item | Distance threshold for self-organisation. | 5 |
| In | Ratio | R | Number | item | Distance each point can move per cycle. | 0.1 |
| In | Rebuild | RB | Boolean | item | Rebuild input curves before bundling. | true |
| In | Point Count | PC | Integer | item | Point count of the rebuild. | 50 |
| In | Weld Count | WC | Integer | item | Number of points at each end to weld. | 0 |
| Out | Bundling Behavior | BB | `BundlingSpec` | item | | |

### 6.5 Engines

All engines: **item** access on every input (see R4), output geometry only when needed (Section 7.9), `Message` shows `frame N · M agents` throttled to ≤ 10 updates/s.

#### Creeper_Engine — nick `CE` — "Culebra Objects Engine"

| In/Out | Name | Nick | Type | Acc | Notes |
| --- | --- | --- | --- | --- | --- |
| In | Init Settings | S | Generic (`InitSettings`) | item | Required. |
| In | Behavioral Settings | BS | Generic (`BehaviorData`) | item | From Controller. Optional: absent = no behaviours. |
| In | Visual Settings | VS | Generic (`VisualSettings`) | item | Optional: absent = Graphic mode, no trails. |
| In | Reset | R | Generic (bool-like) | item | Accept Boolean, button, number (non-zero) or text "true". |
| Out | Creepers | C | Point | list | Head positions. Always produced. |
| Out | Trails | T | Generic (curve) | tree | One branch `{i}` per creeper, one polyline per branch, oldest→newest then head. **Geometry mode only** `[DOC]`. |
| Out | Connectivity | CN | Generic (curve) | list | Lines from heads to neighbours within search radius (Flocking with Connect = true). **Geometry mode only** `[VERIFY]`. |

Behaviour:
- **One frame per solution.** The standard Grasshopper **Timer** component drives it, exactly as in Culebra. Reset = true → re-initialise to frame 0 and output frame 0; otherwise advance one frame.
- Preview must be on to see Graphic mode `[DOC]`.

#### Creeper_ZombieEngine — nick `CZE` — "Culebra Object Zombie Engine"

| In/Out | Name | Nick | Type | Acc | Notes |
| --- | --- | --- | --- | --- | --- |
| In | Init Settings | S | Generic | item | |
| In | Behavioral Settings | BS | Generic | item | |
| In | Visual Settings | VS | Generic | item | |
| In | Iterations | I | Integer | item | Steps to run. |
| In | Run | R | Generic (bool-like) | item | Button/toggle. |
| Out | Creepers | C | Point | list | |
| Out | Trails | T | Generic (curve) | tree | |
| Out | Connectivity | CN | Generic (curve) | list | |

Behaviour `[DECIDED]`: a **zombie** engine needs no Timer. On the solution where Run goes false→true (or Run is true on first solve), re-initialise and run `Iterations` frames in one go, then output the final state. While Run stays unchanged the component returns the cached result without recomputing. The original text calls Run a button that "resets the sim"; treat it as run-and-reset.
Long runs: execute the loop on a background task with cancellation (`CancellationToken`), update `Message` with progress, and publish the result via `ExpireSolution` on the UI thread. The loop must only touch Core state, never Rhino objects.

#### Bundling_Engine — nick (placeholder in the original) — "Engine for Self Organization of Curve Networks"

| In/Out | Name | Nick | Type | Acc | Notes |
| --- | --- | --- | --- | --- | --- |
| In | Curves | C | Curve | list | Curves to bundle. |
| In | Behavioral Settings | BS | Generic (`BehaviorData`) | item | Must contain a Bundling behaviour. |
| In | Visual Settings | VS | Generic | item | Display Mode only. |
| In | Reset | R | Generic (bool-like) | item | |
| Out | Geometry | G | Generic (curve) | list | Self-organised curves. |

Use nickname `BE` (the original shows a placeholder). One bundling cycle per solution; Timer-driven.

#### Bundling_ZombieEngine — nick `BZE` — **NEW** `[DECIDED]`

Same as Bundling_Engine but follows the Zombie pattern:

| In/Out | Name | Nick | Type | Acc | Notes |
| --- | --- | --- | --- | --- | --- |
| In | Curves | C | Curve | list | |
| In | Behavioral Settings | BS | Generic | item | |
| In | Visual Settings | VS | Generic | item | |
| In | Iterations | I | Integer | item | Cycles to run. |
| In | Run | R | Generic (bool-like) | item | |
| Out | Geometry | G | Generic (curve) | list | |

Same run-on-edge, cache-while-unchanged and background-task rules as Creeper_ZombieEngine. The Bundling logic in Core is **one shared implementation**; the two bundling engines are thin drivers over it, as are the two Creeper engines.

### 6.6 Display

#### Trail Data — nick `TD` — "Controls the Trail Data for the Visual Settings Component"

| In/Out | Name | Nick | Type | Acc | Notes | Fallback |
| --- | --- | --- | --- | --- | --- | --- |
| In | Trail | T | Generic (bool-like) | item | Trail visibility. | false |
| In | Trail Step | TS | Integer | item | Minimum number of steps between stored trail points; higher = faster. | 1 |
| In | Max Trail Size | MT | Integer | item | Maximum trail points per creeper; lower = faster. | 100 |
| Out | Trail Data | TD | `TrailDataSettings` | item | | |

Clamp Trail Step ≥ 1 and Max Trail Size ≥ 2, with a warning.

#### Visual Settings — nick `VS` — "Controls the visual settings for the Creeper Engine Outputs"

| In/Out | Name | Nick | Type | Acc | Notes | Fallback |
| --- | --- | --- | --- | --- | --- | --- |
| In | Trail Data | TD | Generic (`TrailDataSettings`) | item | | trails off |
| In | Color Data | CD | Generic | item | **Reserved, ignored** (colour data is out of scope). If connected, add a Remark-level message "Color Data is not supported". | |
| In | Apply Texture | AT | Boolean | item | **Reserved, ignored.** The original warns it is very unstable. | false |
| In | Display Mode | DM | Generic (integer) | item | `[DOC]` 0 = **Graphic**, 1 = **Geometry**. | 0 |
| Out | Visual Settings | VS | `VisualSettings` | item | | |

Display mode semantics `[DOC]` + `[DECIDED]`:
- **Graphic (0):** the engine draws heads and trails directly through the display pipeline each redraw. The `Trails` and `Connectivity` outputs are empty. Cheapest mode.
- **Geometry (1):** the engine produces real `Point`/`Curve` outputs. Viewport preview uses Grasshopper's standard output preview.
- Solid colour only: heads and trails use the component's preview colour (`args.WireColour`, or `args.WireColour_Selected` when selected). No gradients.

---

## 7. Simulation semantics

All formulas are **reference semantics** (classic Reynolds-style steering, which Culebra's lineage uses `[DOC]`). Each is `[VERIFY]` against probes before it is called final. Keep each one in its own small, separately testable function.

### 7.1 Frame step

```
Step():
  1. read-only front buffers (previous frame); write to back buffers.
  2. build the neighbour grid only if the stack contains Flocking; build the
     trail-point grid only if it contains Stigmergy.
  3. for each particle i (parallel if N >= 2000):
       acc = 0
       for each behaviour b in stack order:
            acc += b.Steer(i)         // reads front buffers only
       acc = clampLength(acc, MaxForce)
       v   = clampLength(v_front + acc, MaxSpeed)
       p   = p_front + v * VelocityMultiplier
       2D: z held at its spawn value
       boundary handling                 (7.6)
       mesh projection if Mesh Crawl     (7.5)
       write p, v to back buffers
  4. serial: store trail points per Trail Step (7.8)
  5. serial: child spawning if allowed  (7.7)
  6. swap buffers, frame++
```

Because forces read only the previous frame, step 3 is order-independent between particles and safe to parallelise. Order between *behaviours* only affects floating-point summation order, which is fixed by stack order, so runs are reproducible.

### 7.2 Flocking

For particle *i*, neighbours *j ≠ i* with `|pj − pi|² < SR²` and inside the view cone: `dot(hat(vi), hat(pj − pi)) ≥ cos(VA/2)` (`VA` as full field of view `[VERIFY]` P4). Skip the cone test when `|vi|` is zero.

- Separation: sum of `(pi − pj)/d²`, averaged, steered.
- Alignment: average neighbour velocity, steered.
- Cohesion: seek the average neighbour position.
- `steer(target) = clampLength(setLength(target, MaxSpeed) − vi, MaxForce)`.
- Result = `SV·sep + AV·ali + CV·coh`.
- Neighbour search through the uniform grid (Section 9.2). Use squared distances; take square roots only for the few survivors.
- If any Flocking in the stack has Connect = true, record neighbour pairs (capped, default 200 000 edges) for the Connectivity output. Do not record them otherwise.

### 7.3 Stigmergy

Candidate trail points belong to **other** creepers, within `max(CR, SR)` of the head and inside the view cone.
- Points within `CR`: steer towards their average position with magnitude `CM`.
- Points within `SR`: steer away with magnitude `SM`.
Trail points are looked up in a trail-point grid (Section 9.2) rebuilt each frame from the ring buffers.

### 7.4 Multi Path Tracking (I and II)

Reynolds path following over several paths:
1. Predicted position `pf = p + hat(v) · PD`.
2. For each path whose bounding box lies within `PT` of `pf`, find the closest point on the path to `pf` (precomputed segment arrays).
3. Choose the path with the smallest distance within `PT`. If none, no steering.
4. If that distance ≤ `PR`, no steering. Otherwise seek a target a short distance ahead of the closest point along the path direction.
Paths are converted once (in the GH layer) to point arrays: `Curve.TryGetPolyline` first, otherwise `Curve.ToPolyline` with document tolerance; segment direction/length precomputed; per-path AABB precomputed. Closed-path seam handling: treat closed polylines as cyclic.
Version II adds child spawning (7.7).

### 7.5 Mesh Crawl (I and II)

1. Projected position `pp = p + hat(v) · MPD` (unit-velocity direction; whether the original scales by the speed instead is `[VERIFY]` P7).
2. Closest mesh point `c` to `pp`, with `maximumDistance` bound.
3. If `|pp − c| > MT`, steer to seek `c`. Always remove the velocity component along the surface normal and snap the final position to the surface `[VERIFY]` P7.
`IMeshSurface.Closest(point, maxDist)` is implemented with `Mesh.ClosestMeshPoint(point, maxDist)`. Profile first. Only if it dominates, add a face-hint warm start using mesh face topology. Do not invent a custom BVH unless profiling demands it.

### 7.6 Boundary (Init Settings, Boundary port)

Bounds come from the Box Spawn / Point Spawn box.

| Value | Mode | Behaviour |
| --- | --- | --- |
| 0 | Bounce | Reflect position and velocity at the box faces. Implement with a closed-form triangle-wave fold per axis (no loops), so a step that crosses several box widths is still exact. 2D ignores Z. |
| 1 | Respawn | Particle that leaves the box is re-placed using the spawn rule; its trail is cleared; its velocity is re-initialised `[VERIFY]` P8 (interim: random point inside the box, initial speed, trail cleared). |
| 2 | Do Nothing | No boundary handling. |

### 7.7 Children (Multi Path Tracking II, Mesh Crawl II)

`[DOC]` Only "parent" objects spawn; children cannot spawn. `TS` allows spawning; `MC` caps children per creeper.
Interim rule `[VERIFY]` P10: while `TS` is true, every parent with fewer than `MC` children spawns one child per frame at its current position and velocity. Children inherit nothing else.
**Global cap `[DECIDED]`: 50 000 agents** (right-click setting). When reached, stop spawning and show a warning once.

### 7.8 Trails

- One ring buffer per creeper, capacity `MaxTrailSize` (no `RemoveAt(0)`, no re-allocation after construction).
- Push the head position when `frame % TrailStep == 0` (TrailStep ≥ 1).
- Respawn/teleport clears that creeper's ring so no line is drawn across the domain.
- Trail storage is **independent of visibility**: it runs when Trail = true **or** a Stigmergy behaviour is in the stack.

### 7.9 Output and drawing

- **Graphic mode:** override `DrawViewportWires`. Build one `Line[]` buffer for all trails and one `Point3d[]` for heads only when a frame advanced; draw with a single `DrawLines` and a single `DrawPoints` call. Reuse the buffers.
- **ClippingBox:** override it and keep a running min/max of heads and trail points, otherwise Rhino culls or ignores the custom drawing (and zoom-extents ignores it). Update the box when the frame advances.
- **Geometry mode:** create `Point3d[]`, `PolylineCurve`s and `LineCurve`s only after a frame advanced, and only for the outputs that are connected (`DA`/`Params.Output[i].Recipients.Count > 0`). On unchanged solutions reuse the previous output list.
- Never call `RedrawCanvas` or `Views.Redraw` more than once per frame.

### 7.10 Randomness

Own PRNG in Core (PCG32 or xoshiro128**). `[DECIDED]` The engine seed defaults to a fixed constant, so identical inputs reproduce identical runs, which is an intentional improvement on an unseeded original. Expose the seed as a right-click menu item saved with the file; do **not** add a port. Derive any per-particle stream from `(seed, particleIndex)` so parallel execution does not change results.

### 7.11 Bundling (curve network self-organisation)

Documented ports only; the algorithm is not documented `[VERIFY]` P9. Interim design:
1. Curves → point arrays in the GH layer. If Rebuild, resample each to `PC` points (`DivideByCount` or equivalent) preserving endpoints.
2. Each cycle: for every non-pinned point, find points of **other** curves within `Threshold` (uniform grid), compute their centroid, move the point towards it by at most `Ratio` per cycle (step = `min(Ratio, distance)` along the direction `[VERIFY]` whether Ratio is absolute or a fraction).
3. Weld: pin or merge the first and last `WC` points of each curve to nearby end points `[VERIFY]` P9.
4. Double-buffer positions so a cycle reads only the previous cycle's state.
5. Output: curves rebuilt through the points, degree 3 or polyline `[VERIFY]`.

---

## 8. Compatibility behaviours

- **Resetting:** Reset true on any solution restores frame 0 and outputs frame 0 without advancing.
- **Timer stop:** if the Timer stops, nothing changes; the engine holds its last state and outputs.
- **Document close/lock/delete:** state released (4.4).
- **Many inputs:** R4 rule. One engine = one simulation.
- **Error handling:** any missing required input → `GH_RuntimeMessageLevel.Warning` or `Error` with a precise message ("Init Settings is required") and empty outputs. No exceptions escape `SolveInstance`.
- **Units:** document-unit agnostic. All distances are in model units.

---

## 9. Performance requirements and efficiency review

### 9.1 Targets (to be measured, not promised)

| Scenario | Target step time |
| --- | --- |
| 5 000 agents, Flocking, 3D, SR ≈ 5 % of domain width | ≤ 10 ms on a current laptop CPU |
| 1 000 agents, Stigmergy, trail 100, step 6 | ≤ 15 ms |
| 10 000 agents, Attraction + Repulsion (20 targets) | ≤ 5 ms |
| 2 000 agents, Multi Path Tracking, 20 paths × 200 segments | ≤ 8 ms |
| Graphic redraw at 5 000 agents with trails 50 | no per-frame allocation > 100 KB |

Record actual numbers per OS/CPU in the milestone report.

### 9.2 Spatial indexing `[DECIDED]`

- **Uniform grid** with cell size ≈ the largest search radius in the stack. Build with a **counting sort into flat arrays (CSR layout: `cellStart[]`, `cellItems[]`)**: one pass to count, one to scatter, no per-cell lists and no allocation after the first frame.
- Query = the 27 (3D) or 9 (2D) neighbouring cells.
- Trail points go in a separate grid built the same way (`P = N × T` points). Rebuild each frame; if profiling shows a problem, add incremental updates, not before.
- Cap total cells (e.g. 2 million); if the domain is too large for the radius, increase the cell size and accept more candidates per cell.
- Grid origin from the live particle bounding box (recomputed each frame), not the spawn box, because particles may leave the box in "Do Nothing" mode.

### 9.3 Memory and data layout

- SoA double arrays (4.3); no per-particle objects; no `List<Point3d>` per particle.
- Trail rings flat: one `double[N*cap*3]`. When N grows (children), grow all arrays by doubling at the end of the frame.
- Use `Vector3d`/`Point3d` as local structs only. Do not store them in lists inside the loop.
- Prefer `ArrayPool<T>`/pooled scratch arrays for transient neighbour buffers, sized once.

### 9.4 Review checklist (run on every milestone)

Search the code and fix or justify each hit:

| Anti-pattern | Why it hurts | Fix |
| --- | --- | --- |
| LINQ, `foreach` over `IEnumerable`, lambdas in the step loop | Allocation, virtual calls | `for` loops over arrays |
| `new` / `List.Add` per particle per frame | GC pressure → stutter | Preallocated buffers |
| `List.RemoveAt(0)`, `Insert(0, …)` | O(n) per call | Ring buffers |
| `Math.Sqrt`/`Math.Pow(x, 2)` where a squared compare works | Needless work | Compare squared distances |
| `Math.Acos`/`Atan2` in neighbour tests | Slow, platform-variable | Dot-product vs `cos(VA/2)` |
| `Curve.Duplicate`, `Mesh.DuplicateMesh`, `ToNurbsCurve` per frame | Heavy geometry copies | Convert once when the input object changes |
| Rebuilding polylines/curves when no frame advanced | Wasted work | Cache by frame number |
| `lock` or `ConcurrentX` in the inner loop | Contention | Per-thread scratch, or avoid sharing |
| Closures capturing loop variables inside `Parallel.For` | Allocation | Static local functions / range partitioning with `Parallel.For(0, chunks, …)` |
| String formatting every frame | Allocation | Throttled `Message` update |
| `ExpireSolution(true)` loops, repeated `RedrawCanvas` | UI thrash | One schedule per frame |
| `System.Random` in Core | Cross-runtime variance | Own PRNG |
| `GH_Structure` rebuilt every solution when output is unconnected | Waste | Check `Recipients.Count` |
| Defaults written with `SetPersistentData` more than once | Duplicate saved values (R3) | Once in `RegisterInputParams`; test `PersistentData.DataCount ≤ 1` |
| Static mutable state | Cross-document leaks | Instance state only |

### 9.5 Benchmark harness

A `bench` console project builds fixed synthetic scenarios (the table in 9.1) and prints median/95th-percentile step time and allocated bytes (`GC.GetAllocatedBytesForCurrentThread`). Acceptance: **steady-state allocation per step is zero (±1 KB)** for Core. Run it on Windows x64, macOS Intel and macOS Apple Silicon and include the results.

### 9.6 Parallelism

`Parallel.For` over contiguous particle ranges when N ≥ 2000 (configurable). Each worker reads front buffers and writes disjoint slices of back buffers. No shared RNG: derive from `(seed, index)`. Behaviour grid construction stays single-threaded or uses a parallel counting pass with per-thread histograms.

---

## 10. Cross-platform correctness

1. All Core arithmetic is IEEE double. No `float` in positions. No `unsafe`, no intrinsics.
2. No dependence on iteration order of hash-based collections. Use arrays/lists with defined order.
3. No culture-dependent string parsing; use `CultureInfo.InvariantCulture`.
4. No file system access from Core.
5. Cross-OS results are compared with tolerance; bit equality across OS/runtime is **not** claimed `[HO]`.
6. Check the `net48` build against Mono-specific traps: avoid APIs missing in Mono; avoid `Span<T>` unless `System.Memory` is referenced; `Parallel.For` is fine.
7. No behaviour may depend on a monitor, a Windows UI thread or a message pump.

---

## 11. Test plan

### 11.1 Automated: port conformance (runs on CI, no Rhino)

The Grasshopper project exposes a build-time `ComponentManifest.Generate()` that returns each component's name, nick, subcategory, GUID, and each port's name, nick, type name and access, **without instantiating Rhino** (read from a static registry the components also use to register their ports). The test compares it to the tables in Section 6, stored as `tests/ParticleEngine.Parity/culebra_ports.json`. Also assert: GUIDs unique; no input has more than one persistent value; port counts and order fixed; exactly 20 components registered.

### 11.2 Automated: Core unit tests (Windows, macOS Intel, macOS Apple Silicon)

Run on `net8.0`; also on `net48` on Windows. Minimum set:

- PRNG: same seed → same first 1 000 values on every runner (golden values stored in the repo).
- Integration: constant velocity, clamp by Max Speed/Max Force, Velocity Multiplier.
- Bounce fold: random positions and large steps vs a brute-force reference.
- Respawn and Do Nothing.
- Grid: neighbour set from the grid equals brute-force O(N²) for random clouds, 2D and 3D, several radii.
- Flocking: each of separation/alignment/cohesion in isolation on hand-computed two- and three-agent cases; view-cone boundary.
- Stigmergy: trail points of other agents attract; own trail ignored; CR/SR boundary.
- Path tracking: inside/outside corridor, nearest of several paths, closed-path seam.
- Forces: falloff, threshold edge, max clamp, list thresholds.
- Trails: ring wrap, Trail Step, clear on respawn, capacity 2 edge case.
- Children: Max Children, global cap, children never spawn.
- Bundling: two parallel curves converge; beyond-threshold curves do not move; weld.
- Determinism: same inputs and seed → identical state after 500 frames within a runner; across runners within 1e-9 relative.
- Allocation test: steady-state `Step()` allocates nothing.
- Zombie run equals N sequential single steps.
- Stateful-engine rule: calling the engine driver with `DA.Iteration > 0` does not advance state.

### 11.3 Host acceptance (manual; record in a table with OS, Rhino build, runtime)

| Host | Install | Load | Defs run | Timer run | Reset | Zombie run | Graphic | Geometry | Save/reopen | Notes |
| --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- |
| Rhino 8 Win, .NET Framework | | | | | | | | | | |
| Rhino 8 Win, modern .NET | | | | | | | | | | |
| Rhino 8 Mac, Apple Silicon | | | | | | | | | | |
| Rhino 8 Mac, Intel | | | | | | | | | | |
| Rhino 7 Win | | | | | | | | | | |
| Rhino 7 Mac | | | | | | | | | | |

Acceptance sequence per host: install → components appear in the six subcategories → Box Spawn → Init → Move → Flocking → Controller → Creeper_Engine → Timer → run 60 s → Reset → toggle Graphic/Geometry → change a behaviour live → add a Stigmergy → save → close → reopen (frame 0, no errors) → delete engine mid-run (no exception) → switch Rhino document (no crash).

### 11.4 Parity probes

Section 12. They produce ground truth from the original; the results become golden fixtures in `tests/ParticleEngine.Parity/`.

---

## 12. Getting ground truth from the original Culebra

The public documentation does not give defaults, access types, exact formulas or several semantics. The owner has Culebra installed on Windows (Rhino 7). Two things close the gap.

### 12.1 Export every port definition (once)

Run in **Rhino 7's Python editor** (command `EditPythonScript`) with Grasshopper open, not in a GhPython component, because the original warns scripting components can clash with its bundled IKVM libraries. This is an **untested draft**; adjust if an attribute differs.

```python
import json, Grasshopper as gh

def port(p):
    d = {"name": p.Name, "nick": p.NickName, "desc": p.Description,
         "access": str(p.Access), "optional": bool(p.Optional),
         "type": p.TypeName}
    try:
        d["default"] = [str(g) for g in p.PersistentData.AllData(True)]
    except:
        pass
    return d

rows = []
for proxy in gh.Instances.ComponentServer.ObjectProxies:
    desc = proxy.Desc
    if "culebra" not in (desc.Category or "").lower():
        continue
    try:
        obj = proxy.CreateInstance()
        rows.append({"name": desc.Name, "nick": desc.NickName,
                     "sub": desc.SubCategory, "exposure": str(desc.Exposure),
                     "inputs":  [port(p) for p in obj.Params.Input],
                     "outputs": [port(p) for p in obj.Params.Output]})
    except Exception as e:
        rows.append({"name": desc.Name, "error": str(e)})

open(r"C:\Temp\culebra_ports.json", "w").write(json.dumps(rows, indent=1))
```

Give the resulting JSON to the implementing assistant. It replaces every `Fallback` column and confirms access (item/list), optional flags and the Controller's inputs. If the script cannot load the components, fall back to right-clicking each input in Grasshopper and noting its default, access and whether it is optional.

### 12.2 Single-step probes

Method: use **Point Spawn** (fixed start points, so no randomness), the **Zombie engine** with `Iterations` = 1, 2, 3, and read `Creepers` into a Panel. Copy the panel text to the assistant. Vary one setting at a time.

| ID | Question | Setup | Record |
| --- | --- | --- | --- |
| P1 | Initial Speed scalar vs vector; Max Speed/Force clamps; Velocity Multiplier | 1 point at origin, no behaviours except Move Settings. IS = vector (1,0,0), then scalar 1. MS = 10, 0.5. MF = 1. VM = 1 and 2. Iterations 1, 2, 3. | Positions. Is a scalar direction random? Is VM applied to velocity or displacement? |
| P2 | Attraction law | 1 stationary agent (IS = 0), target at d = 0.25 / 0.5 / 0.75 / 0.99 / 1.01 × threshold. AV = 1, MA large; then AV = 1, MA small. | 1-step displacement vs d; behaviour outside the threshold; effect of MA. |
| P3 | Repulsion law | Same as P2. | Same. |
| P4 | Flocking maths | Two agents, known headings, distance d. Test Align, Separation, Cohesion one at a time (others 0). Then place the neighbour behind the agent and sweep VA = 90 / 180 / 270 / 360. Then d just inside/outside SR. | 1-step velocity change. Whether VA is full or half angle; separation falloff; cap by MF. |
| P5 | Stigmergy | Agent A moves in a straight line leaving a trail (Trail Step = 1); agent B starts beside the trail at distances inside/outside CR and SR. Repeat with Trail toggle off. | B's velocity change. Whether trail storage works when Trail = false. |
| P6 | Path tracking | One straight polyline; agent at distances inside/outside PR and PT, with PD = 0 and PD > 0; two parallel paths at different offsets. | 1-step steering; which path wins; seek-ahead distance. |
| P7 | Mesh crawl | Flat quad mesh; agent at height h above it moving parallel, then at an angle. MT = 0 / large; MPD = 0 / large. | Does the agent snap to the surface? Is velocity flattened? When does it steer back? |
| P8 | Boundary | Agent exits the box at speed. Bounce, Respawn, Do Nothing; 2D and 3D; trail on. | Where it reappears; velocity after respawn; what 2D does with Z. |
| P9 | Bundling | Two parallel straight curves at d inside/outside Threshold; vary Ratio, Rebuild, PC, WC. Run 1–5 cycles on Bundling_Engine (Reset, then Timer ticks). | Point positions per cycle; is Ratio absolute or a fraction; what Weld does at the ends; output curve degree. |
| P10 | Children | Multi Path Tracking II with Trigger Spawn true/false, Max Children = 1, 2. | When children appear, from where, with what velocity; per-frame or event-based. |
| P11 | Trail data | Trail on, Trail Step = 1 / 5, Max Trail Size = 10; run 30 frames. | Number of points in each polyline; whether the head is included. |
| P12 | Box Spawn | Spawn Location = 0 and 1, 2D and 3D, Count = 100. | Distribution (grid vs random; base plane vs volume); does the third option the docs hint at exist? |
| P13 | Engines | Creeper_Engine: does it output the head positions before the first step? Does Reset also output frame 0? Do Graphic mode outputs come back empty? | Output state per solution. |

Each completed probe becomes a golden fixture (`probe_PNN.json`: inputs and recorded outputs) and an automated Core test with a stated tolerance. If a probe contradicts a formula in Section 7, the probe wins; update Section 7 and the code, and note it in the milestone report.

---

## 13. Licence and clean-room statement

- `[HO]` Culebra's public repository identifies GPL-3.0. This project is a clean-room reimplementation from public documentation and observed behaviour. **No Culebra source, binary, icon or documentation text may be copied or translated.** Port names and nicknames are reproduced for interface compatibility.
- If any Culebra source is ever read, record it; that can affect the licence of this work. Choose and state this project's licence before any distribution beyond the owner. This is not legal advice.
- The component descriptions in Section 6 are paraphrased; write new tooltip text in your own words.

---

## 14. Milestones

| M | Deliverable | Exit criteria |
| --- | --- | --- |
| **M0** | Retarget: Core `netstandard2.0`, GH `net48;net7.0`, solution layout, CI on 3 OS runners, existing 40 tests green on all, MSB3277 gone. | CI logs from Windows, macOS Intel, macOS Apple Silicon. Manual load on at least Rhino 8 Win (both runtimes). |
| **M1** | Port-parity scaffold: all 20 components registered with correct ports, no logic; manifest conformance test; Goo types. | Conformance test green. Components visible in the six subcategories on Win and Mac. |
| **M2** | Spawn, Init, Move Settings, Controller, Trail Data, Visual Settings with real behaviour; Creeper_Engine with Move Settings only (straight-line motion, bounds, trails, Graphic and Geometry modes). | Probes P1, P8, P11, P12, P13 matched. Timer-driven run on Win and Mac. |
| **M3** | Spatial grid + Flocking + Attraction + Repulsion; Connectivity output. | P2–P4 matched; grid vs brute-force test; benchmark table. |
| **M4** | Stigmergy + trail grid; Multi Path Tracking I/II; Mesh Crawl I/II; children. | P5–P7, P10 matched. |
| **M5** | Creeper_ZombieEngine with background task and cancellation. | Zombie = N single steps test; UI stays responsive on a 10 000-agent, 1 000-iteration run. |
| **M6** | Bundling, Bundling_Engine, **Bundling_ZombieEngine**. | P9 matched. Shared Core implementation. |
| **M7** | Performance pass against Section 9 targets; zero-allocation proof; review checklist report. | Bench results on three machines. |
| **M8** | Icons, Yak packages (rh8-any multi-target, rh7-any), install docs, examples that avoid R3. | Fresh-machine install on Win and Mac via Yak. Host acceptance table complete for Tier 1 hosts. |

At every milestone, state which hosts were actually exercised, and which items remain `[VERIFY]`.

---

## 15. Mapping from 0.3 to the new component set

| 0.3 component | Fate |
| --- | --- |
| Simulate | Retire (frame-replay slider). Keep the code in Core if useful for tests. |
| Live Engine | Replace by Creeper_Engine. Its self-scheduling code becomes an optional "Self-timer (ms)" right-click item on the Creeper engines, off by default. |
| Settings | Split across Move Settings, Init Settings, Trail Data. |
| Behaviour Controller | Becomes Controller. |
| Flock, Separation, Alignment, Cohesion | Merged into Flocking. |
| Follow Path | Becomes Multi Path Tracking (single-path is the one-item case). |
| Follow Trails | Becomes Stigmergy. |
| Point Force | Split into Attraction Force and Repulsion Force. |
| Spawn Box | Becomes Box Spawn (and adds Point Spawn). |
| Boundaries | Becomes the Boundary port on Init Settings. |
| Wander, Noise, Pursuit, Avoidance, Assign Group, Select Group | Unregistered (Core retained). |

---

## 16. Sources

- [Culebra.NET overview](https://www.complicitmatter.com/culebra-net/) · [Culebra repository](https://github.com/elQuixote/Culebra)
- [Community component documentation index (Culebra 2.0-Beta)](https://grasshopperdocs.com/addons/culebra.html) and per-component pages under `grasshopperdocs.com/components/culebra/`
- [Culebra group page (feature list)](https://grasshopper3d.com/group/culebra)
- [Yak package structure and multi-targeting](https://developer.rhino3d.com/guides/yak/the-anatomy-of-a-package)
- [McNeel forum: multi-target plugin, Rhino/Grasshopper](https://discourse.mcneel.com/t/multitarget-plugin-rhino-grasshopper/199966)
- [McNeel forum: cross-platform plugin development with .NET](https://discourse.mcneel.com/t/cross-platform-rhino-plugin-development-with-net/184376)
- [McNeel forum: cross-platform Yak packages](https://discourse.mcneel.com/t/a-little-confused-about-cross-platform-yak-packages/175981)
- [Rhino 8 system requirements](https://www.rhino3d.com/8/system-requirements)
- [Rhino Visual Studio extensions release notes (net48 + net7.0 templates)](https://github.com/mcneel/RhinoVisualStudioExtensions/releases)
- `Particle Engine - Project Handover.md` (31 October 2026)

---

## 17. Spec change log

Ribbon follow-up, 2026-10-31: at the owner's request, shared branding is brought forward from M8: a 16x16 category icon and 24x24 component/library icon. The current netstandard2.0 target is retained for this focused change. Prepared and build-tested in the Codex workspace; application to the original repository and host testing remain pending. See RIBBON_FIX.md.

Append-only. Each entry: date, section, what changed, why (probe ID or owner decision). Do not edit earlier sections silently; record the deviation here and update the section it affects.

| Date | Section | Change | Reason |
| --- | --- | --- | --- |
| 2026-10-31 | 12.1 | Port dump is run in **Rhino 8**, not Rhino 7. | Owner runs Culebra on Rhino 8; Rhino 8 is the primary target. |
| 2026-10-31 | 1 / 2 | Build is a **fresh** 0.4.0, not a port of the 0.3.0 source (which was never supplied). Handover kept as an overridable reference only. | Owner decision, 2026-10-31. |
| 2026-10-31 | 6.5 / 6.2 / 4.2 | **Engines take a Move Settings input directly.** Creeper_Engine / Creeper_ZombieEngine have both `Init Settings` and `Move Settings` inputs (both nicknamed `S`). This overrides §6.2's note that Move Settings feeds the Controller; Move Settings wires into the engine. | Port dump (owner's Culebra, Rhino 8). |
| 2026-10-31 | 6.5 | Engine `Init Settings` / `Move Settings` kept **item** access (one sim per solution, R4); Culebra uses `list`. Documented exception in the conformance test. | Deviation per R4. |
| 2026-10-31 | 6.3 | **Mesh Crawl has a 4th input** `Multiplier` (nick `M`, Number, default 1.5), after Mesh Projection Distance; spec missed it. Semantics `[VERIFY]` P7. | Port dump. |
| 2026-10-31 | 6.1–6.6 | Real defaults replace the interim guesses: Box Spawn Count **500**/Location **0** (only 0/1 exist); Flocking VA **180**/SR **70**/AV **0.24**/SV **0.09**/CV **0.045**; Stigmergy VA **60**/CM **1.5**/CR **80**/SM **0**/SR **5**; MPT PT **500**/PD **50**/PR **15** (II: TS **True**, MC **2**); Attraction AV **0.97**/MA **0.06**; Repulsion AV **1**/MA **1.5**; Bundling T **100**/R **0.5**/RB **True**/PC **40**/WC **2**; Trail Data TS **0**/MT **5000**. | Port dump. |
| 2026-10-31 | 6.2 | Init Settings `Spawn Settings` input is **list** access. | Port dump. |
| 2026-10-31 | 6.5 | Bundling_Engine nickname is `BE` (Culebra ships the placeholder `"Nickname"`). | Spec §6.5 intent. |

| 2026-10-31 | 2 / 2.5 / 14 | Final ribbon follow-up supersedes the earlier pending-status note: the retained single netstandard2.0 build, compile-only Rhino 7 API references and build-only net48 drawing reference were installed and loaded in Windows Rhino 8/.NET 10.0.11. All 20 proxies and category branding were verified. The original multi-target matrix remains a design reference, not proof of the current implementation or untested hosts. | Owner host reports; docs/RIBBON_FIX.md. |
| 2026-10-31 | 5 / 14 | Missing tab confirmed as overflow: tab 64 of 65 was beyond the ribbon width. Session-only repositioning after Rhino restored visibility. Persistent positioning is not implemented; no runtime change or simulation change was needed. | Saved header bounds and owner confirmation, "shows up now". |
