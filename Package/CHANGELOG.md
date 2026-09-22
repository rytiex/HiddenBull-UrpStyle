# Changelog

All notable changes to this package are documented in this file.
The format is based on [Keep a Changelog](https://keepachangelog.com/), and this project adheres to [Semantic Versioning](https://semver.org/).

## [0.2.0] - 2026-09-22

Phase 0 — foundation. No rendering code yet; this release establishes the structure everything
after it is built on.

### Added

- Package folder structure: `Runtime/{Core,Rendering,Volume,Shaders}`, `Editor/{Inspectors,Setup,Validation,ShaderStripping,ShaderGUI}`, `Tests/{Runtime,Editor}`.
- `StyleProfile` ScriptableObject holding one `StyleTierSettings` per `StyleQualityTier`, the single source of configuration for renderer features and for editor-side variant stripping.
- `StyleQualityTier` (Low/Medium/High/Ultra) and `StyleResolutionScale` with per-tier defaults.
- Test assemblies plus coverage for the profile's tier-resolution invariants.
- Documentation: architecture decisions, roadmap, and contribution guide under `Docs/`.

### Changed

- Runtime assembly now references `Unity.RenderPipelines.Core.Runtime`, required for Render Graph and Volume APIs.
- Editor assembly now references `Unity.RenderPipelines.Core.Editor`.

### Removed

- Placeholder `StyleSettings` ScriptableObject, superseded by `StyleProfile`.

## [0.1.0] - 2026-09-22

### Added

- Initial package skeleton: package.json, Runtime/Editor assembly definitions, Samples~ folder.
- Placeholder `StyleSettings` ScriptableObject.
