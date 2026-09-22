# HiddenBull URP Style

A shared Universal Render Pipeline visual style toolkit for HiddenBull games — shaders, renderer
features and volume components that give every project the same look, with performance treated as
the first requirement rather than a later pass.

Unity 6 · URP 17 · Forward+ · PC and console

## Install

```json
"com.hiddenbull.urpstyle": "https://github.com/rytiex/HiddenBull-UrpStyle.git?path=Package"
```

Append a tag such as `#v0.4.0` to pin a release.

## Documentation

- [Overview](Docs/README.md)
- [Architecture](Docs/Architecture.md) — the decisions the package is built on
- [Roadmap](Docs/Roadmap.md) — development phases
- [Contributing](Docs/Contributing.md) — layout, workflow, conventions

## Status

Phase 1 implemented — shader core and ambient model. A single directional light and no textures are
enough to light a scene in the style. Costs are not yet measured, so the phase is not signed off.
See the [changelog](Package/CHANGELOG.md).

## License

[MIT](LICENSE)
