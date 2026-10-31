# Particle Engine — project starter

A Grasshopper plugin that reproduces a chosen subset of Culebra.NET for Rhino 8 (Windows and macOS) and, best-effort, Rhino 7. This folder is the starting point for building it with Claude Code.

## Contents

| Path | Purpose |
| --- | --- |
| `CLAUDE.md` | Standing instructions Claude Code reads automatically |
| `docs/BUILD_SPEC.md` | The full build specification (20 components, ports, semantics, performance, tests, milestones) |
| `docs/PROGRESS.md` | Milestone tracker, open verify items, host test log, benchmarks |
| `docs/RIBBON_FIX.md` | Ribbon overflow repair + opt-in persistent tab placement (host-verified) |
| `.github/workflows/ci.yml` | CI: Core tests on Windows, macOS Intel, macOS Apple Silicon; plugin build on Windows; manual benchmark run |
| `scripts/install-ribbon.ps1` | Windows dev install of the built plugin into Grasshopper's Libraries folder (or `Install Particle Engine.cmd`) |
| `legacy/` | 0.3.0 prototype handover, kept as an overridable reference (not reused; see `legacy/README.md`) |

## Setup (about 15 minutes)

1. **Install tools:** the .NET 8 SDK, git, and Claude Code. Optionally the `gh` CLI for the GitHub step below.
2. **Create the project folder** (for example `C:\dev\ParticleEngine`) and copy this starter into it, keeping the folder structure.
3. **Add the 0.3.0 source:** unzip `Particle Engine 0.3.0 source.zip` into `legacy/` so that `legacy/` contains the `Particle Engine` source tree. Leave it unmodified.
4. **Initialise git:**
   ```
   git init
   git add -A
   git commit -m "Starter: spec, instructions, CI, legacy 0.3.0 source"
   ```
5. **Optional but recommended, GitHub for CI:** create a private repository and push. The workflow in `.github/workflows/ci.yml` then runs the Core tests on Windows and on both kinds of Mac automatically. This is the only way to get Mac test evidence for Core without a Mac of your own.
6. **Start Claude Code** in the folder (`claude`) and paste the prompt from `docs/KICKOFF_PROMPT.md`.

## Your part

Claude Code can build, test and fix code on its own. It cannot run Rhino for you, and it cannot see Culebra. You do three things:

1. Test builds in Rhino when it hands you an acceptance script. Paste results back or record them in `docs/PROGRESS.md`.
2. Run the Culebra **port dump** and **probes** (spec Section 12). This turns guessed behaviour into measured behaviour. Do the port dump before milestone M1.
3. Test on a Mac at the milestones listed in `docs/PROGRESS.md` Section 2.

## Notes

- `ci.yml` uses the runner labels `windows-latest`, `macos-15-intel` and `macos-latest` (Apple Silicon). GitHub changes runner labels from time to time; if a job fails to start, check the label in GitHub's runner documentation.
- The CI plugin job needs the Rhino and Grasshopper NuGet packages, which are available from nuget.org with no licence.
- Nothing in this folder contains Culebra code. See spec Section 13.
