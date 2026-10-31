# Particle Engine — instructions for Claude Code

## Project

A native C# Grasshopper plugin that reproduces a chosen subset of Culebra.NET (agent/particle simulation) for Rhino. Primary targets: **Rhino 8 on Windows and macOS** (Intel and Apple Silicon). Best-effort: **Rhino 7 on Windows and macOS**.

Owner: Abhinav Chaudhary, computational designer, London. He develops on **Windows** and tests in Rhino himself. He cannot always test on a Mac.

## Read these first, in this order

1. `docs/BUILD_SPEC.md` — the contract: scope, exact port tables for 20 components, architecture, semantics, performance rules, tests, milestones. Follow it. It is long; read Sections 0, 2, 3, 6 and 14 fully before writing code, then the rest as each milestone needs it.
2. `docs/PROGRESS.md` — current state, open `[VERIFY]` items, host test results. Read at the start of every session. Update before ending every session.
3. `legacy/` — the 0.3.0 prototype source (read-only reference; see its README). Reuse tested Core logic where the spec allows, by copying it into `src/`. Never edit `legacy/`.

## Rules that override your defaults

1. **Port parity is the contract.** Names, nicknames, order, types and access of every port must match Section 6 of the spec. A manifest conformance test enforces it. Never reorder, remove or retype a port once a version has been shared; append only.
2. **Core never references RhinoCommon.** `ParticleEngine.Core` targets `netstandard2.0` and works on `double[]` and small structs. Rhino geometry crosses the interfaces in spec Section 2.3.
3. **Target frameworks:** Core `netstandard2.0`; Grasshopper project `net48;net7.0` (`.gha` extension); tests `net8.0` plus `net48` on Windows. Do not use `netstandard2.1`. Reference RhinoCommon and Grasshopper 7.x for `net48` and 8.x for `net7.0`, compile-only (no copy to output).
4. **No third-party NuGet packages** beyond `System.Memory`/`System.Buffers` for `net48`, test frameworks, and the Rhino/Grasshopper references. Ask first for anything else.
5. **No allocation in the per-frame hot path.** Struct-of-arrays, ring-buffer trails, counting-sort uniform grid, own PRNG (no `System.Random` in Core), no LINQ or lambdas in the step loop, no trig in neighbour tests. Run the Section 9.4 checklist before declaring a milestone done and report what you found.
6. **Engines advance exactly once per solution.** Item access on all engine inputs plus an `if (DA.Iteration > 0)` guard (spec R4). No hard errors on multi-value inputs.
7. **Defaults are written once, in component code.** Never call `SetPersistentData` repeatedly on one input; any tool that writes `.gh` files must `PersistentData.Clear()` first. A test asserts every scalar input holds at most one stored value.
8. **Unknown behaviour is not yours to invent silently.** Where the spec says `[VERIFY]`, implement the stated interim rule, mark the code with a greppable comment `// VERIFY(P<n>): <what is unknown>`, and list it in `docs/PROGRESS.md`. When the owner supplies probe results, probes beat the spec: update the code, add a golden test, and record the change in the spec change log (spec Section 17).
9. **Clean-room.** No Culebra source, binaries, icons or documentation text may be copied or translated. Write original tooltip text. If you ever see Culebra source, tell the owner.
10. **Never claim a platform works without evidence from that platform.** Distinguish "compiles", "loads in Rhino", "runs correctly", and say which host. You cannot run Rhino on a Mac; say so rather than implying otherwise.

## How to work

- Work milestone by milestone (spec Section 14), starting at the first one not marked done in `docs/PROGRESS.md`. Do not start a later milestone until the earlier one meets its exit criteria, unless the owner says so.
- At the start of a milestone, write a short plan (files to create/change, tests to add) and wait for the owner's go-ahead only if the plan departs from the spec. Otherwise proceed.
- Write tests with the code. Run `dotnet test` and `dotnet build` yourself and fix failures; do not hand back red builds.
- Commit after each green step with a clear message. Never commit `bin/`, `obj/`, `TestResults/`, or `.yak` / `.zip` artefacts.
- At the end of each milestone, add a milestone report to `docs/PROGRESS.md`: files changed, tests run with OS and runtime, benchmark numbers if relevant, deviations from the spec, new `[VERIFY]` items, and **exactly which hosts were exercised**.
- Manual Rhino testing is done by the owner. After a build, run `scripts/install-dev.ps1` (Windows) if asked, then give the owner a short numbered acceptance script for that milestone (use spec Section 11.3 as the base) and wait for results. Record them in `docs/PROGRESS.md`.
- When blocked by a decision that is the owner's (scope, naming, licence, interpretation calls I-1 to I-4 in the spec), ask one concrete question with a recommended default. Do not stall on things the spec already decides.

## Commands (create them in milestone M0; keep this list current)

```
dotnet build ParticleEngine.sln -c Release
dotnet test tests/ParticleEngine.Tests -c Release -f net8.0
dotnet test tests/ParticleEngine.Tests -c Release -f net48          # Windows only
dotnet run -c Release --project bench/ParticleEngine.Bench          # benchmarks
pwsh scripts/install-dev.ps1 -Target net7.0                         # Windows dev install; use net48 for .NET Framework mode
```

Set `<CheckEolTargetFramework>false</CheckEolTargetFramework>` if the SDK warns about the `net7.0` target. The `net7.0` assets load on the .NET 8 runtime that Rhino 8 uses.

## Environment notes

- Owner's Rhino 8 on Windows has been seen running .NET 8.0.15. Rhino 8 on Windows can also run the .NET Framework runtime, so both builds must be tested.
- Grasshopper loads `.gha` from its Libraries folder (File > Special Folders > Components Folder). Install `ParticleEngine.gha` and `ParticleEngine.Core.dll` together, in one location only. Close Rhino before copying.
- Keep file paths and line endings portable; the repo is used on Windows and macOS. Use `.gitattributes` `* text=auto`.
- Prefer the Read/Edit/Write tools for file edits. Keep scripts in `scripts/`.
