# legacy/ — Particle Engine 0.3.0 prototype (read-only)

Put the unzipped contents of `Particle Engine 0.3.0 source.zip` here (the ChatGPT-produced prototype). It should look like:

```
legacy/
  Particle Engine/
    src/ParticleEngine.Core/
    src/ParticleEngine.Grasshopper/
    tests/ParticleEngine.Tests/
    README.md, EXAMPLES.md, VALIDATION.md
    .github/workflows/core-tests.yml
```

Rules for Claude Code:

- **Never edit anything under `legacy/`.** Copy code out into `src/` and `tests/`.
- Known facts about this code (from the project handover): targets `netstandard2.1`; 19 components; 40 console tests passed on Windows; one reported example-file defect (duplicate saved defaults, spec R3); MSB3277 `System.Net.Http` warning; no Mac testing; live-state scheduling code in the Live Engine component is worth keeping as the optional self-timer.
- Do not copy the example-builder utility or the example `.gh` files; they carry the duplicate-defaults defect.
- Treat the handover's claims as unverified until the code and tests confirm them.
