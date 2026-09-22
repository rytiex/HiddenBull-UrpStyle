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

## Status

Phase 1 is implemented: the shader core and the ambient model are in place, and a single
directional light with no textures is enough to light a scene in the style.

It is not yet signed off. Per the [roadmap](Roadmap.md) a feature is complete only once its cost is
measured and recorded, and the cost table in
[Features/Lighting.md](Features/Lighting.md) is still empty.

See the [changelog](../Package/CHANGELOG.md).
