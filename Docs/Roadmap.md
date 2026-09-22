# Roadmap

## Working rule

**Every phase opens with a design discussion before any code is written.** What the feature should
look like, how it is built, what it costs, and which trade-offs are accepted — agreed first, then
implemented. The outcome of that discussion is recorded in [Architecture.md](Architecture.md) as a
numbered decision, so the reasoning survives the conversation.

A phase is finished when all of the following are true:

- The feature works in the sandbox project.
- It can be switched off cleanly, so a low-end renderer can drop it entirely.
- Its cost is measured and written down in `Docs/Features/`.
- `Docs/` and `Package/CHANGELOG.md` are updated.

## Phases

### Phase 0 — Foundation ✅

Package structure, assembly definitions, settings model, sandbox ↔ package link, documentation
skeleton, versioning discipline. No rendering code.

### Phase 1 — Shader core and ambient model — implemented, not signed off

The shared HLSL library, the first master Lit shader built on it, the three-zone gradient ambient
model (Architecture D12) and a custom ShaderGUI. Establishes SRP Batcher compliance and the keyword
budget that everything after this inherits.

The ambient model belongs here rather than in the environment phase because in this style it *is* the lighting
model (Architecture D10, D13).

**Deliverable:** a single directional light and zero textures are enough to make a rock convincing.

Verified in the sandbox. **Cost measurement is deliberately deferred to Phase 2**, where the
profiling harness is built and both phases are measured together — Phase 1 is a thin per-pixel
layer, while the screen-space passes of Phase 2 are where the real budget goes.

This is a deferral, not a cancellation. The point of measuring is less to learn the absolute number
than to have one to compare against: when Phase 5 adds transparency, the only way to notice that
the albedo-only path quietly grew is to hold it against what it cost before.

### Phase 2 — Brush and shadows

Split into three steps, each ending in something that can be looked at before the next begins.

**2a — Brush source and surface layer.** *Implemented.* The brush atlas generator (Architecture
D18) and the surface layer that consumes it (D17): terminator break-up and albedo variation. Done
first because the look can be judged immediately and independently — if the brush
is wrong, it is far cheaper to find out here.

Verify against [TestPlans/Phase2a-Brush.md](TestPlans/Phase2a-Brush.md).

**2b — Shadows.** Not started. A screen-space mask over URP shadow maps was built and reverted;
see Architecture D5 for what that cost and why the technique is open again.

**2c — Profiling harness.** Carried over from Phase 1: build it under `Package/Samples~/`, then fill
in the cost tables for both phases. Shipping it as a sample rather than leaving it in the sandbox
means costs can be re-measured on any consumer's own hardware.

### Phase 3 — Environment

Height fog and aerial perspective, plus directional coloured occlusion (Architecture D6) — GTAO
with bent normals feeding the ambient term established in Phase 1.

### Phase 4 — Transparency and sorting

The layered strategy from Architecture D9: dithered alpha, weighted blended OIT, transparent depth
prepass with sort priority, and the draw-order debug view.

### Phase 5 — Post-processing

Volume components for the style's grading and screen-space effects.

### Phase 6 — Optimisation

Shader variant stripping driven by the renderers' settings, the package's own temporal accumulation layer,
profiling scenes, and a documented cost budget per feature.

## Measurement

Performance is the first priority, so it is measured rather than assumed. Each feature's page in
`Docs/Features/` records its GPU cost at a stated resolution and settings, taken in the sandbox
profiling scene. A feature without a recorded cost is not considered complete.
