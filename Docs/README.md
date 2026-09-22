# HiddenBull URP Style — Documentation

A shared Universal Render Pipeline visual style toolkit for HiddenBull games: shaders, renderer
features and volume components that give every project the same look, with performance treated as
the first requirement rather than a later pass.

## Requirements

| | |
|---|---|
| Unity | 6000.0 or newer (developed on 6000.4.1f1) |
| URP | 17.4.0 or newer |
| Rendering path | Forward+ |
| Target | PC and console |

## Installation

Add to your project's `Packages/manifest.json`:

```json
"com.hiddenbull.urpstyle": "https://github.com/rytiex/HiddenBull-UrpStyle.git?path=Package"
```

Pin a release by appending a tag, for example `#v0.1.0`.

## Contents

- **[Architecture](Architecture.md)** — the decisions the package is built on, and the reasoning
  behind each one.
- **[Roadmap](Roadmap.md)** — development phases and what "done" means.
- **[Contributing](Contributing.md)** — repository layout, sandbox workflow, code conventions,
  versioning.
- **[Features](Features/)** — one page per feature: what it does, how it is configured, and what
  it costs.
  - [Lighting and Ambient](Features/Lighting.md)
  - [Brush Layer](Features/Brush.md)
- **[Test Plans](TestPlans/)** — per-phase verification checklists.
  - [Phase 2a — Brush](TestPlans/Phase2a-Brush.md)

## Status

The shader core, the ambient model and the brush layer are implemented and usable. Shadows are
still URP's own; the package's own shadow system is not started.

Costs are not yet measured, so no phase is signed off. See the [changelog](../Package/CHANGELOG.md).
