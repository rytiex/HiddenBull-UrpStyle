# Contributing

## Repository layout

```
Package/          the published UPM package — everything shipped lives here
  Runtime/
    Core/         StyleSettings, shared types
    Rendering/    renderer features and Render Graph passes
    Volume/       custom VolumeComponents
    Shaders/
      Library/    shared .hlsl — the lighting model lives here, nowhere else
      Lit/        master shaders, all including Library/
  Editor/
    Inspectors/ Setup/ Validation/ ShaderStripping/ ShaderGUI/
  Tests/
    Runtime/ Editor/
  Samples~/       excluded from Unity import by the trailing tilde

Docs/             architecture decisions, roadmap, per-feature pages
HiddenBullUrpStyleSandbox/   local test project, git-ignored, never pushed
```

## Sandbox workflow

The sandbox references the package from disk, so edits to `Package/` apply immediately on
returning to the editor. In `HiddenBullUrpStyleSandbox/Packages/manifest.json`:

```json
"com.hiddenbull.urpstyle": "file:../../Package"
```

The sandbox is ignored by git. It is a throwaway test bed — nothing that matters may live only
there. Anything worth keeping belongs in `Package/Samples~/` or in `Docs/`.

## Conventions

**C#**

- Serialized fields use the `m_` prefix and are private, exposed through properties.
- Public API carries XML documentation. Comments explain *why*, not *what*.
- Namespace: `HiddenBull.UrpStyle`, `HiddenBull.UrpStyle.Editor`.

**Shaders**

- The lighting model is implemented once, in `Runtime/Shaders/Library/`. A shader that
  reimplements it is a bug.
- Prefer `shader_feature_local` over `multi_compile`. Every `multi_compile` multiplies the variant
  count across the whole package and must be justified.
- `UnityPerMaterial` CBUFFER layouts must match exactly across all passes of a shader, or the SRP
  Batcher silently drops it.

**Meta files**

`.meta` files are committed. Deleting or regenerating them breaks every reference in every project
consuming the package.

## Versioning

Semantic versioning, per [Keep a Changelog](https://keepachangelog.com/):

- **Patch** — fixes that change no visuals and no API.
- **Minor** — new features, or visual changes that are opt-in.
- **Major** — API breaks, or visual changes that alter existing scenes without opting in.

A visual change that silently alters how a shipped project looks is a breaking change, even if the
API is untouched.

Every change updates `Package/CHANGELOG.md` in the same commit.

## Definition of done

See the phase completion criteria in [Roadmap.md](Roadmap.md). In short: it works in the sandbox,
it can be switched off cleanly, its cost is measured and written down, and the docs and changelog
are updated.
