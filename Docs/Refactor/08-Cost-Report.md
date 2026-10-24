# 08 — Cost Report

Costs per phase, measured offline with `Tools~/Check-All.ps1` (fxc instruction slots and texture reads) unless
a row says it was profiled in Unity. Budgets are in `01-Vision-and-Rules.md` §5.

## Phase 0 — Foundation

Confirmed by the user on 2026-10-24 in the sandbox: `HB Style Globals` runs after `Setup Camera Properties` and
before `Draw Main Light Shadowmap` for the base and the overlay camera (Render Graph Viewer); only base cameras get
a camera state; overlay and preview cameras do not.

| Item | Cost | How measured |
|---|---|---|
| `HB Style Globals` pass | One Render Graph unsafe pass per camera (base, overlay, preview): one 48-byte constant buffer upload + global bind. No draw, no dispatch, no texture. | By construction; not profiled in Unity yet |
| CPU per camera | Environment resolve (a few field reads), camera-state lookup in a list of base cameras; no per-frame allocations by construction | Code review; not profiled |
| Camera states | One small object per base camera; pruned 2 s after the camera stops rendering | Code review |
| Core HLSL library | 0 slots when included and unused; `HB_DEBUG_VIEW` is the constant 0 without `HB_STYLE_DEBUG` | fxc |
| Core probe (every Core helper used once) | 80 slots, 0 texture reads; 82 slots with `HB_STYLE_DEBUG` | fxc, `Core Probe` variants |
| Debug keyword | Editor only; variants with `HB_STYLE_DEBUG` are stripped from player builds | `StyleDebugStripper` |

## Phase 1 — Lit core

Confirmed by the user on 2026-10-24 in the sandbox ("her şey iyi durumda"); compile time of the full material judged
very good. Known gap carried to Phase 2: the light jumps at the 6:00 / 18:00 sun–moon swap because the key-light
colour and hand-off fade (Sky Light gradient, Moon Light) arrive with the Sky section.

Static fxc counts per variant. "Full" URP keywords: cascades, soft shadows, cluster light loop, additional light
shadows, lightmap + shadowmask, light layers, cookies, SSAO, DBuffer. URP's own Lit is compiled the same way as a
reference (`Tools~/Shaders/UrpLitReference.hlsl`).

| Variant | Slots | Texture reads | Added above URP Lit | Budget (added) |
|---|---|---|---|---|
| URP Lit, Full keywords (reference) | 1048 | 101 | – | – |
| HB Lit albedo only, Full keywords | 1307 | 107 | +259 / +6 | 500 / 8 — OK |
| HB Lit brush only, Full keywords | 1806 | 107 | +758 / +6 | – |
| HB Lit all features (UV), Full keywords | 2357 | 119 | +1309 / +18 | 2500 / 60 — OK |
| HB Lit all features (triplanar), Full keywords | 2452 | 127 | +1404 / +26 | 2500 / 60 — OK |
| HB Lit vertex | 40 | 0 | – | – |
| HB Lit ShadowCaster (clip, triplanar) | 85 | 3 | – | – |
| HB Lit DepthNormals (normal map) | 38 | 2 | – | – |

Budgets count the cost added above URP's own Lit forward pass at the same keyword set (01 §5).

Albedo-only variant against the URP keyword set (HB / URP Lit):

| URP keywords | HB slots / reads | URP Lit slots / reads |
|---|---|---|
| none | 104 / 2 | 85 / 2 |
| + cascades + soft shadows | 471 / 36 | 291 / 32 |
| + cluster light loop | 631 / 36 | 457 / 32 |
| Full | 1307 / 107 | 1048 / 101 |

Most static reads come from URP's soft-shadow and additional-light shadow filters, which fxc unrolls; they exist in
URP Lit too. Reads inside dynamic branches (contact filter vs. raw filter, brush patch edges) are counted once each
but do not all run per pixel.

## Phase 2a — Key light, Sky, Atmosphere in Lit (compiled offline, not run in Unity)

| Variant | Slots | Reads | Added above URP Lit | Budget |
|---|---|---|---|---|
| HB Lit albedo only, Full keywords | 1405 | 109 | +357 / +8 | 500 / 8 — OK |
| HB Lit all features (UV), Full keywords | 2480 | 123 | +1432 / +22 | 2500 / 60 — OK |
| HB Lit all features (triplanar), Full keywords | 2577 | 131 | +1529 / +30 | 2500 / 60 — OK |
| HB Sky (gradient, brush, sun, moon, stars, clouds, haze) | 671 | 12 | absolute | 1500 / 20 — OK |

Atmosphere adds about +98 slots and +2 reads (two sky LUT reads, toward and away from the sun) to every Lit variant;
reflections fall back to the live sky in Specular variants (+2 reads). Albedo-only is now exactly at its read budget.
Globals CBUFFER grew to 27 float4 (432 bytes); one global texture array `_HB_SkyLut` (ambient + sky slices) and the
cloud atlas.

## Phase 2b — Fog split, stars+, milky way, aurora, cloud light, profile inspectors (compiled offline)

| Variant | Slots | Reads | Added above URP Lit | Budget |
|---|---|---|---|---|
| HB Lit albedo only, Full keywords | 1443 | 110 | +395 / +9 | 500 / 9 — OK (read budget raised from 8) |
| HB Lit all features (UV), Full keywords | 2518 | 124 | +1470 / +23 | 2500 / 60 — OK |
| HB Lit all features (triplanar), Full keywords | 2615 | 132 | +1567 / +31 | 2500 / 60 — OK |
| HB Sky | 920 | 12 | absolute | 1500 / 20 — OK |

After the 2b test (2026-10-24): the milky way was removed at the user's request, and the aurora was rebuilt from
eight stacked height slices of a repeating ribbon into one analytic vertical curtain (a single finite, folded sheet
in the Aurora Heading direction; no loop over slices, no texture reads). CBUFFER: 34 float4 (544 bytes).

The extra read is the Fog colour: Fog now reads its own LUT slice (1 read) while the Horizon term reads the sky
colour toward and away from the sun (2 reads); before the split both shared the 2 sky reads. Aurora, milky way and
star colour use ALU noise only (no texture reads). All atmosphere reads sit inside a branch that is skipped where
the surface has no fog or haze. CBUFFER: 36 float4 (576 bytes).

Aurora count (1–4 curtains) added after the test: a uniform loop over the analytic curtain; Sky 925 slots / 12 reads.

## Phase 2c — Environment zones and runtime profile blending (compiled offline)

No shader cost: mixing happens on the CPU before the globals upload. Per base camera per frame: zone weights
(one box distance each), then — only when a zone or an API blend is active — one globals resolve per contributing
profile and a section-wise lerp. Each profile's sky LUT (3 × 256 × 3 RGBAHalf) is baked once and cached; a mixed LUT
(per base camera) is re-blended and uploaded only when the profiles, their versions or the weights change. With no
zone in reach and no blend running, the path is the same single-profile path as before. Zone weight maths simulated
offline (inside, edge, blend band, corner, rotated and scaled boxes, zero blend distance).

Phase 1 globals table, for reference (the 2a fields follow the same pattern in `StyleGlobals.hlsl`):

Globals CBUFFER `HBStyleFrame` (Phase 1, 128 bytes):

| Field | x | y | z | w |
|---|---|---|---|---|
| `_HB_Environment` | time of day (h) | ambient LUT row | light bias × hand-off | ambient intensity |
| `_HB_AmbientFloor` | ground ambient r | g | b | probes baked |
| `_HB_Weather` | rain 0–1 | snow 0–1 | wind 0–1 | – |
| `_HB_Brush` | tiles per metre | fade start | fade rate | atlas bound |
| `_HB_BrushVariation` | randomize | grow | grow start | – |
| `_HB_ShadowFilter` | softness | contact | taps | contact reach (15 m) |
| `_HB_ShadowBrush` | edge brush | 1 / edge size | atlas tile x | atlas tile y |
| `_HB_Debug` | debug view | – | – | – |

Global textures: `_HB_BrushAtlas` (profile atlas), `_HB_AmbientLut` (256 × 3 RGBAHalf array, rebaked only when the
profile changes).
