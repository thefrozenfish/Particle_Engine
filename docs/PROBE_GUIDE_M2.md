# M2 probe guide — ground truth from Culebra

These five probes (spec §12.2) confirm or correct the **interim rules** Particle Engine currently uses
for M2. Run each in **Rhino 8 with Culebra loaded**, record what you see, and save the results so I can
turn each into a golden test and fix the code where it differs.

## How to record

For each probe, save a short markdown file at `tests/ParticleEngine.Parity/probe_P<NN>.md` with: the
exact settings you used, and the numbers/observations you read. Pasting the values back to me in chat is
also fine. Keep inputs **fixed** (use **Point Spawn**, not Box Spawn, so there's no randomness) and
vary **one** setting at a time.

General setup for the motion probes (P1, P8, P11): **Point Spawn** (one or a few known points) →
**Init Settings** → Culebra's **Zombie engine** with **Iterations = 1, 2, 3** (and more where asked) →
read the **Creepers** output into a **Panel**. The Zombie engine gives an exact N-step result with no
timer, so positions are reproducible.

---

## P1 — Move Settings (initial speed, clamps, velocity multiplier)

**Why:** is a scalar Initial Speed a *random direction* of that length, or something else? Does Velocity
Multiplier scale the velocity or the displacement? How do Max Speed / Max Force clamp?

**Setup:** 1 point at the origin, **no behaviours** (Move Settings only).
- Run A: Initial Speed = **vector (1,0,0)**, Max Speed 10, Max Force 1, VM 1 → Iterations 1, 2, 3.
- Run B: as A but **VM = 2**.
- Run C: Initial Speed = **scalar 1** (a number, not a vector), same as A → Iterations 1, 2, 3.
- Run D: Max Speed = **0.5**, Initial Speed vector (10,0,0) → Iterations 1, 2, 3.

**Record:** the Creeper position(s) each iteration for each run.
**Tells us:** VM scales velocity vs displacement (compare A vs B); whether scalar speed is a random
direction (run C — does it move along a random axis?); Max Speed clamp (run D).
*(Our interim: scalar = random direction; VM scales displacement; clamp to Max Speed.)*

## P8 — Boundary (Bounce / Respawn / Do Nothing, 2D and 3D)

**Setup:** Point Spawn one point near a box face, Init Settings box small, Initial Speed a vector aimed
**out** of the box fast enough to leave in one step. Trail on. Run the Zombie engine a few iterations.
- Vary Boundary = **0 (Bounce), 1 (Respawn), 2 (Do Nothing)**.
- Do each in **2D** and **3D**.

**Record:** where the agent is after it hits/leaves the boundary; its velocity after a bounce/respawn;
for Respawn, where it reappears and whether the trail is cleared; for 2D, what happens to **Z**.
*(Our interim: Bounce reflects exactly; Respawn = random point in box + new velocity + cleared trail;
2D holds Z at the spawn value.)*

## P11 — Trails (length, step, head inclusion)

**Setup:** one point moving in a straight line, **Trail on**, **Max Trail Size = 10**. Run **30 frames**
(Zombie Iterations = 30, or Timer for 30 ticks).
- Run A: **Trail Step = 1**.
- Run B: **Trail Step = 5**.

**Record:** how many points are in each creeper's trail polyline; whether the **current head** is included
as the last point; what happens once more than 10 points would exist (does it cap at 10?).
*(Our interim: a point stored every Trail Step frames, capped at Max Trail Size, head appended last.)*

## P12 — Box Spawn distribution (Spawn Location 0 vs 1)

**Setup:** Culebra **Box Spawn**, Count = 100, bake/preview the resulting points.
- Vary **Spawn Location = 0** and **= 1**, each in **2D** and **3D**.

**Record:** for each, is the distribution a **regular grid** or **random**? In 2D, are points on the box's
**base plane** (constant Z) or spread through the volume? Does a third spawn option appear to exist?
*(Our interim: 1 = random, 0 = grid; 2D uses the base rectangle.)*

## P13 — Engine output states

**Setup:** Culebra **Creeper_Engine** (Timer-driven) and/or the Zombie engine.

**Record:**
- Does **Creepers** output the head positions **before the first step** (i.e. is there a frame 0)?
- On **Reset**, does it output **frame 0** (not advance)?
- In **Graphic** display mode, do the **Trails / Connectivity** outputs come back **empty**?
*(Our interim: frame 0 on first solve and on Reset; Graphic-mode geometry outputs are empty.)*

---

When a probe disagrees with our interim rule, the probe wins (spec rule 8): I update the code, add a
golden test from your recorded numbers, and log the change in BUILD_SPEC §17. Then M2 can be marked done.
