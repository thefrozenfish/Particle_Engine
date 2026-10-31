# ParticleEngine.Parity

Conformance and parity fixtures (spec 11.1, 11.4, 12).

- **M1** adds the port-conformance test: it compares the component manifest
  (`ComponentManifest.Generate()`) against `culebra_ports.json` and asserts GUID
  uniqueness, fixed port order, ≤ 1 persistent value per scalar input, and exactly
  20 registered components — all without instantiating Rhino.
- `culebra_ports.json` — the raw port dump from the owner's installed Culebra
  (spec 12.1). **Not yet present**; the owner runs the dump before M1.
- `probe_P*.md` / `probe_P*.json` — recorded ground-truth probes (spec 12.2), added
  as each milestone's probes come in.

This project is an empty placeholder in M0 so the solution layout matches spec 2.3.
