# Kickoff prompt for Claude Code

Open a terminal in the project folder, run `claude`, and paste the block below as your first message.

```
Read CLAUDE.md, then docs/BUILD_SPEC.md (Sections 0, 2, 3, 6 and 14 in full, the rest skimmed), then docs/PROGRESS.md and legacy/README.md.

Then do milestone M0 from the spec:
1. Inspect the 0.3.0 source in legacy/ and tell me, in a short list, what you find against the spec's review table (R1–R16): which findings are confirmed by the actual code, which are not, and anything efficient or inefficient the spec did not anticipate. This is the first real code review, so be specific (file and line).
2. Create the new solution layout from spec Section 2.3 (Core netstandard2.0; Grasshopper net48;net7.0 with .gha extension; tests net8.0 + net48; bench project stub), copying the tested Core logic you can reuse out of legacy/ into src/ without editing legacy/.
3. Port the existing tests so they pass on net8.0 and net48 here on Windows. Fix anything that depended on netstandard2.1.
4. Make sure .github/workflows/ci.yml works with the new layout and add .gitattributes.
5. Update docs/PROGRESS.md with an M0 report, including exactly which hosts were exercised.

Assume the spec's interpretation calls I-1 to I-4 unless I tell you otherwise. Do not start M1. When M0 is green, stop and give me a numbered Rhino acceptance script for Windows covering both .NET runtimes.
```

## What to do around it

- Before you start: unzip the 0.3.0 source into `legacy/` (see `legacy/README.md`) and install the .NET 8 SDK and git.
- While it runs: Claude Code will build and test itself. You test in Rhino when it asks.
- Before M1: do the Culebra port dump (spec Section 12.1) and save the JSON. M1 conformance tests are far stronger with it than with the public docs.
- Before M2 onwards: run the probes listed in `docs/PROGRESS.md` Section 2. Without them, behaviour follows the spec's interim rules, which are reasonable guesses, not ground truth.

## Prompts for later milestones

Use the same shape each time:

```
Read docs/PROGRESS.md and CLAUDE.md. Continue with the first milestone not marked done (currently M<n>). Follow the spec section for it. Use the probe results in tests/ParticleEngine.Parity/ where they exist. When it is green on all CI runners and the review checklist is done, write the milestone report and give me the Rhino acceptance script. Do not start the next milestone.
```

When you have test results for a host, paste them under Section 4 of `docs/PROGRESS.md`, or give them to Claude Code and ask it to record them.

If a probe contradicts the spec:

```
Probe P<n> results are in tests/ParticleEngine.Parity/probe_P<n>.md. They override the interim rule. Update the code, add golden tests from the recorded numbers, log the change in spec Section 17, and mark P<n> resolved in docs/PROGRESS.md.
```
