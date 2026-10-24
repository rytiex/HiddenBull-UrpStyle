# 04 — Known Pitfalls, Bugs Already Paid For, Rejected Approaches

Read this before touching the related system. Each entry: **symptom → cause → rule**.

## 1. URP / Render Graph traps

| # | Symptom | Cause | Rule |
|---|---|---|---|
| 1.1 | Shadows randomly vanish or everything goes black after clicking an object | URP's empty-shadowmap path writes `_MainLightShadowmapSize = (1,1,1,1)` and leaves stale shadow matrices; an Inspector preview render leaks them into the Scene view | Treat every URP shadow global as possibly another camera's. Guard custom filters with `_MainLightShadowmapSize.z < 2.0` → fall back to `MainLightRealtimeShadow`. Never build clamp ranges from `.xy` unbounded. |
| 1.2 | Terrain shadows disappear above Low | A `depth < 0.9999` "no data" guard discarded casters nearest the light (reversed-Z) | Never treat shadow depth near 1.0 as "empty". The size guard is enough. |
| 1.3 | "CullingResults instance is no longer valid" + ZBinning error in edit mode | Per-camera cache stamped with `Time.frameCount`, which does not advance between editor repaints | Invalidate at the start of every Prepare, consume once in Record. **Never gate on `Time.frameCount`.** |
| 1.4 | Snow / fog flicker while hovering the Inspector | `beginCameraRendering` runs before URP updates the Volume stack, so it holds the previous camera's (preview's) values | Never read per-camera settings in `beginCameraRendering`. v1 has no Volumes, but the same rule applies to any state set by another camera. |
| 1.5 | Foliage shadows snap unwound while hovering the Inspector | Globals pass ran after shadow casters; casters read the preview camera's globals | Globals read by shadow/depth casters are set at or before **BeforeRenderingShadows**. |
| 1.6 | Material preview camera overwrote per-camera globals → flicker in water/shore | Preview cameras render between scene cameras | Every pass rebinds its own camera's data before use; preview cameras never advance simulations. |
| 1.7 | Rain drawn but invisible, no error | Rendering into an intermediate target flips Y: `UNITY_MATRIX_P[1][1]` is negative; pixel sizes became negative → alpha 0 | Use `abs(UNITY_MATRIX_P[1][1])` for pixels-per-metre maths. If something draws but shows nothing, take a Frame Debugger capture early. |
| 1.8 | Grass, rain and snow visible through walls in an FPS game | World passes also ran for the **overlay** camera, whose depth is cleared; grass ignored the culling mask | Overlay cameras run globals + transparency only. GPU content respects `camera.cullingMask` via the owner layer. |
| 1.9 | Unity terrain far patches dark, no debug, no snow | Unity draws patches beyond Base Map Distance with the `BaseMapShader` dependency (URP's), which knows nothing of our lighting; it happened even at 20000 | v1 has its own terrain renderer. Do not depend on Unity terrain shader dependencies. |
| 1.10 | Snow vanishes depending on camera angle in Play mode | Static batching merged renderers; `unity_RendererBounds` stopped being per object | Shaders that read per-renderer data need `"DisableBatching" = "True"` — or better, bake the data into the mesh (v1 foliage). |
| 1.11 | "UnityPerMaterial var is not declared in shader property section" (BRG / GPU Resident Drawer) | A cbuffer variable (`_Metallic`) had no Properties entry in Foliage | Every UnityPerMaterial variable is declared in Properties of **every** shader that uses the cbuffer. Per-family cbuffers, not one shared superset. |
| 1.12 | Statistics window shows no triangles for billboards / grass | Unity does not count indirect draws (Won't Fix) | Provide our own counters (AsyncGPUReadback, only while an inspector watches). |
| 1.13 | Depth priming silently off | MSAA on, or renderer left at Disabled | Feature forces Depth Priming = Forced in editor; MSAA unsupported. |
| 1.14 | Transparents break with MSAA | OIT targets are not MSAA-resolved | MSAA unsupported; document. |
| 1.15 | Cascade-shaped light leak on huge triangles | Shadow pancaking clamps per vertex | Set Directional Light → Shadows → Near Plane = 1 (user's choice; do not add ZClip changes unless asked). |
| 1.16 | Shadow artefact looked like a lightmap leak | Our contact filter returned lit when its blocker search found nothing at contact points | For any "leak", first ask whether it survives turning the realtime sun off; compare Attenuation vs Unfiltered debug views. |

## 2. Shader authoring traps

- A user-visible shader name starting with **"Hidden"** disappears from the material dropdown (string prefix
  check, no slash needed). Use `HB URP Style/...`; internal ones `Hidden/HB URP Style/...`.
- fxc needs `/Gec` because URP declares `half` globals; Unity also compiles with back-compat, which is why
  procedural instancing setup can write `unity_ObjectToWorld` etc.
- Arrays indexed in loops force unrolling (fxc X3531): structure loops so they stay loops.
- `shader_feature_local` for material features; `multi_compile` only for URP/global state. Every extra
  `multi_compile` multiplies variants and compile time.
- Library names: core `Packing.hlsl` is `UnpackNormalMapRGorAG` (capital M) — grep the package source before
  calling a library function.
- Terrain clips itself out of the sky depth on purpose (it is the ground, not a roof); liquids are drawn into
  shelter at rest level without waves.
- `half` precision: world positions and depths in `float`.
- Compile editor-only `.shader` files offline too.

## 3. C# / Unity API traps (Unity 6000.4)

- **Deprecated, never use:** `FindObjectsSortMode`, `FindObjectOfType`, `FindObjectsOfType`,
  `Object.GetInstanceID()` (use `GetEntityId`), `VolumeComponent.displayName` (use `[DisplayInfo]`).
  Use `FindObjectsByType<T>()` only in editor tools; prefer static registries.
- Never name members `enabled`, `name`, `tag`, `transform` on MonoBehaviours (CS0108 warnings).
- ShaderGUI: `FindProperty(name, props)` throws if missing — use an optional lookup for optional props.
- Domain-reload-off: every static registry needs a `[RuntimeInitializeOnLoadMethod(SubsystemRegistration)]`
  reset.
- The offline csc compile references every assembly, so it **does not catch missing asmdef references**
  (GPUResidentDrawerMode compiled offline, failed in Unity with CS0012). Check asmdefs by hand; read such
  settings through `SerializedObject` when the type lives in an unreferenced assembly.
- Modifying a ScriptableObject or renderer asset in Play mode persists to disk in the editor — runtime
  setting APIs must restore authored values on exiting Play mode.
- Editing the URP asset from code: some fields (soft shadow quality) are editor-only (SerializedObject).

## 4. Editing / tooling traps

- CRLF files: deleting a line by matching `"\n<line>"` leaves a lone `\r` and glues lines. Include a full
  neighbouring line in the replacement; grep for glued lines afterwards.
- Bulk renames with perl `s///e`: a nested match resets `$1` — copy the capture first (`my $n = $1;`). Dry-run
  on one file and diff before running on many. Untracked files have no git copy to recover from.
- Unity `.meta` files: create one with a fresh GUID for every new asset in the package (git-installed
  packages are immutable, Unity cannot create them there). Renaming a MonoBehaviour/ScriptableObject file
  keeps references only if the `.meta` moves with it.

## 5. Approaches the user rejected (do not propose again without a new design talk)

| Area | Rejected | Why / what was chosen |
|---|---|---|
| Weather | Per-texel **local accumulation** memory (snow/wet history per shelter texel) | Cracks on spheres/slopes, moving objects lost snow; "daha da kötü". Global state + shelter + cover/uncover fade was kept. |
| Weather | Single-read pre-filtered sky exposure | Not accepted at the time (look risk). |
| Fog | More incremental fog tweaks; APV in fog | Every patch broke another light mode. v1: analytic atmosphere, decided. |
| Water | Distance-phased surf lines / displaced surf | Shown twice as "mavi çizgiler"; foam is texture only. |
| Water colour | Extra colour settings (Clarity, Depth Range, Horizon colour) | One Depth Gradient only: colour keys = colour, alpha keys = floor hidden, x = 1 − exp(−path / 4 m). |
| Water | Wind multiplier on waves | Material toggle "Wind Affects Waves" instead. |
| Shadows | Shrinking contact-hardening reach below 15 m | Keep 15 m. |
| Shadows | ZClip / depth clamp change for pancaking | User sets Near Plane = 1. |
| Variant stripping | Pass-level stripping (ForwardLit on transparent, OIT on opaque) | Risky for runtime-created materials. Only strip keywords that are provably unused project-wide. |
| Transparency | Weighted-Blended OIT tiers | Always MBOIT. |
| Shader Graph | A base/template graph | Node library only. |
| Brush | "Minimum Detail" setting | Destroyed fine detail, no FPS gain. |
| Temporal | A temporal accumulation layer | Dropped. |
| UI | Inspector warnings the user did not ask for | e.g. no "moving object uses Static weather" warning. |

## 6. Process lessons

- When a look is missed twice, the gap is a missing component, not a constant (fog, water side view needed
  reflection + fresnel, not gradient tweaks).
- Simulate geometry before reasoning: the liquid grid's flipped triangles and sliver walls, and the waterline
  mismatch (analytic wave vs piecewise-linear mesh, 7.5 cm error), were found by simulation.
- Use debug views before theorising (shadows: Filter Values → Search Radius → Blocker Count; weather: Sky
  Exposure).
- Waterline: the underwater mask must match the **rendered mesh**, not the analytic surface; a flat or
  near-flat surface avoids the problem entirely (v1 spec).
- Near the end of a session, never revert working code to "start over" — fix forward and keep turns short.
- `delayCall` / editor hooks: guard against stacking and destroyed targets.
