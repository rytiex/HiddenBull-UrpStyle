# 01 — Vision and Working Rules

## 1. What we are building

A shared URP toolkit that gives every HiddenBull game the same **painterly, readable look** — wrapped
diffuse light, gradient ambient, brush strokes everywhere, stylised sky, water, weather and foliage — at a
cost low enough for mid-range PCs (reference GPU: **RTX 3060, 1080p**).

The old package proved the look. It failed on structure: features were bolted onto each shader, systems
duplicated work per camera, shaders were huge (6000–10000 instruction slots), compile times were long, and
fixes stacked on fixes. v1 keeps the look and rebuilds the structure.

### Priorities, in order

1. **Performance** — GPU and CPU. Fixed, known costs; no hidden per-frame work; no oversized textures or
   buffers; short shader compile times.
2. **Robustness over convenience.** A fixed, well-understood path beats a dynamic, clever one. Prefer baked
   or precomputed data when it removes runtime work.
3. **Clear structure.** Every system is a module with one owner, one data asset, one renderer hook.
   Shaders compose shared modules; nothing is copy-pasted.
4. **Ease of use.** Few settings, good defaults, organised menus, presets.
5. **Look.** Keep what already looks good; improve where the spec says so.

## 2. Decisions already made (do not reopen)

| Topic | Decision |
|---|---|
| Repository | Same repo and sandbox. New branch `refactor/v1`. `Package/` is rebuilt from scratch; old code is reference only. |
| Compatibility | **Clean break.** No old component GUIDs, property names or scenes kept working. One-shot converters only where listed in the spec (Unity TerrainData height import; basic texture slots of old materials). |
| Terrain | **Our own terrain** (camera-centred clipmap like Liquid Terrain), our own height + layer data. Unity TerrainData is used only to import height once. |
| Terrain + Liquid | One family: liquid uses the terrain's data directly; one set of components and tools. |
| Environment settings | **ScriptableObject profile on the HB Style Sun. No URP Volume components.** Runtime API for dynamic scenes (weather transitions, profile blending) and a box zone component for local overrides. |
| Fog | Replace the froxel volumetric fog with a cheaper analytic **height fog + horizon / aerial perspective** applied in-shader. |
| Transparency | Keep moment-based OIT (no Weighted-Blended tiers), but optimise it and add real tinted transmission. |
| MSAA | **Not supported** (Depth Priming Forced and OIT need it off). Document it; post AA only. |
| Shader Graph | **Node library only** (Custom Function / Sub Graph nodes for an Unlit graph). No template graph. |
| Unity version | Unity 6000.4 (6000.0 minimum), URP 17.4, Forward+, Render Graph only. PC and console. No WebGL/mobile. |
| Lighting | Mixed lighting with **Shadowmask** is the supported bake mode. APV recommended. Time of day lives on HB Style Sun. |

## 3. Naming and organisation

| Thing | Rule | Example |
|---|---|---|
| C# public types | `HBStyle` prefix | `HBStyleSun`, `HBStyleTerrain`, `HBStyleQuality` |
| Internal C# types | `Style` prefix, `internal` | `StyleShelterPass` |
| Namespace | `HiddenBull.UrpStyle` (+ `.Editor`) | |
| Component display name | `HB Style <Name>` | `HB Style Terrain` |
| Add Component menu | `HiddenBull/HB Style/<Category>/HB Style <Name>` | `.../Environment/HB Style Sun` |
| Categories | Environment, Terrain, Liquid, Vegetation, Weather, Rendering | |
| User-visible shaders | `HB URP Style/<Name>` | `HB URP Style/Lit` |
| Internal shaders | `Hidden/HB URP Style/<Name>` | `Hidden/HB URP Style/Sky Depth` |
| **Never** | a user-visible shader name starting with "Hidden" — it disappears from the material dropdown | |
| Tools menu | `Tools/HiddenBull/URP Style/<Tool>` | `.../Texture Studio` |
| Create menu | `Assets/Create/HiddenBull/URP Style/<Asset>` | `.../Environment Profile` |
| Generated assets | `<TYPE>_<Category>_<Name>_<Index>` | `TEX_Terrain_Rock_01`, `MAT_Billboard_Oak` |
| Tooltips | every serialized field has an English tooltip that says what it does and its unit | |

Every component's AddComponent entry must make clear what it is for (category + name).

## 4. How to work with me

### Communication
- Reply in **Turkish**. All code, tooltips and docs in **English**.
- Keep replies short; lead with the result. Use tables for comparisons.
- Open design points: **AskUserQuestion with 2–4 options, one marked "(Recommended)"**. Never present a bare
  decision as settled when it is mine to make; never ask about things these documents decide.
- Each phase starts with a **design discussion** before code.

### Code
- **No code comments.** Lines **≤ 120 characters**. Match the surrounding style.
- I commit and push. **Never commit, push or create PRs.**
- Proceed with code changes inside an agreed phase without asking each time.
- **Never add unrequested settings, rules or "safety" options** to dodge side effects. Ask instead.
- Use only non-deprecated Unity 6000.4 APIs (see 04-Known-Pitfalls §6).
- Line endings: the repo uses CRLF via `core.autocrlf=true`; when editing with scripts, preserve them.

### Verification
- **Compile offline before every hand-off**: shaders with dxc+fxc, C# with Unity's Roslyn (see 07-Tooling).
  Say "compiled offline, not run in Unity" — never claim it works.
- **Measure shader cost offline** (instruction slots, texture reads) and report it against the budget.
- **Simulate geometry and maths** (C# via PowerShell / Unity's dotnet) before asking me to test geometry
  bugs (grids, LOD morphing, waterlines, shelter follow logic).
- **Trace a reported symptom through the code with degenerate inputs** before asking me to test anything.
- **No unmeasured diagnosis stated as fact.** Use the debug views first.
- **A toggle I report as fixing/causing a bug is the measurement** — act on it before your own theory.
- **Ask how I placed / use things** in my scene; asset files on disk are not my live setup.
- In fragile areas (weather, OIT, waterline, shadows): **one change + one check per turn**.
- If a look target is missed twice: stop tuning constants, break the reference into components and name the
  missing one.
- Batch work: I prefer "discuss → implement the whole agreed batch → one test list", not test-fix loops.
- Don't revert my working state when something breaks near the end of a session — fix forward.
- When I report a mistake of yours that could recur, record it (memory) in the same turn.

## 5. Performance budgets (targets, measured offline with fxc, full keywords)

Surface shaders lit by URP lights are budgeted by the cost they **add above URP's own Lit forward pass compiled with
the same URP keywords** (decided 2026-10-24: URP Lit alone is 1048 slots / 101 reads with the full keyword set, so an
absolute budget would charge URP's own features to us). `Tools~/ShaderVariants.psd1` names the reference variant per
budgeted variant. Shaders with no URP counterpart (Sky) are budgeted absolutely.

| Shader / pass | Budget (instruction slots) | Texture reads |
|---|---|---|
| Lit forward (all features on) | ≤ 2500 | ≤ 60 |
| Lit forward (albedo only) | ≤ 500 | ≤ 9 (raised from 8 on 2026-10-24 for the Fog + Horizon split) |
| Foliage forward | ≤ 2500 | ≤ 60 |
| Terrain forward | ≤ 3500 | ≤ 140 |
| Grass | ≤ 2000 | ≤ 30 |
| Liquid surface | ≤ 3500 | ≤ 120 |
| Sky | ≤ 1500 | ≤ 20 |
| Billboard | ≤ 1500 | ≤ 30 |

Frame targets on the reference GPU at 1080p (Ultra, busy scene): whole package ≤ 4 ms GPU above plain URP;
grass ≤ 2 ms; precipitation ≤ 0.5 ms; shelter maps ≤ 0.3 ms; transparency (no refraction) ≤ 0.6 ms.
CPU: no per-object C# work per frame; no GC allocations per frame; no `Find*` calls at runtime.

Compile time: a full Lit variant set must compile in under ~60 s cold. Prefer `shader_feature_local`,
avoid multiplying `multi_compile` sets, keep loops as loops (`UNITY_LOOP`) instead of unrolled copies.
