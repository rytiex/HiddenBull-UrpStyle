# Roadmap

## Working rule

**Every phase opens with a design discussion before any code is written.** What the feature should
look like, how it is built, what it costs, and which trade-offs are accepted — agreed first, then
implemented. The outcome of that discussion is recorded in [Architecture.md](Architecture.md) as a
numbered decision, so the reasoning survives the conversation.

A phase is finished when all of the following are true:

- The feature works in the sandbox project.
- It has a defined behaviour on every quality tier, including *off*.
- Its cost is measured and written down in `Docs/Features/`.
- `Docs/` and `Package/CHANGELOG.md` are updated.

## Phases

### Phase 0 — Foundation ✅

Package structure, assembly definitions, `StyleProfile` with quality tiers, sandbox ↔ package
link, documentation skeleton, versioning discipline. No rendering code.

### Phase 1 — Shader core and ambient model

The shared HLSL library, the first master Lit shader built on it, the three-zone gradient ambient
model (Architecture D12) and a custom ShaderGUI. Establishes SRP Batcher compliance and the keyword
budget that everything after this inherits.

The ambient model belongs here rather than in Phase 3 because in this style it *is* the lighting
model (Architecture D10, D13).

**Deliverable:** a single directional light and zero textures are enough to make a rock convincing.

### Phase 2 — Lighting and shadows

The contact-hardening, painterly shadow mask (Architecture D5). Blocker search, variable-radius
PCF, world-space brush warp, screen-space resolve.

### Phase 3 — Environment

Height fog and aerial perspective, plus directional coloured occlusion (Architecture D6) — GTAO
with bent normals feeding the ambient term established in Phase 1.

### Phase 4 — Transparency and sorting

The layered strategy from Architecture D9: dithered alpha, weighted blended OIT, transparent depth
prepass with sort priority, and the draw-order debug view.

### Phase 5 — Post-processing

Volume components for the style's grading and screen-space effects.

### Phase 6 — Optimisation

Shader variant stripping driven by the tier data, the package's own temporal accumulation layer,
profiling scenes, and a documented cost budget per feature.

## Measurement

Performance is the first priority, so it is measured rather than assumed. Each feature's page in
`Docs/Features/` records its GPU cost at a stated resolution and tier, taken in the sandbox
profiling scene. A feature without a recorded cost is not considered complete.
