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

## Phase 3 — Weather, step 1: shelter map (compiled offline, not run in Unity)

The first weather batch was removed after its test; its numbers are kept below only as history. Step 1 adds no
cost to any Lit variant outside the debug view: Lit albedo only stays +395 / +9 and all features +1470 / +23
(UV), +1567 / +31 (triplanar). The Weather/Shelter debug view reads 4 shelter texels (+4 reads on the debug
variant). Per base camera while weather is active or the view is on: one top-down cull and one DepthOnly draw of
the opaques 160 m around the camera into a 256² / 512² / 1024² D16 map (Weather quality). The map preview kernel
(38 slots) runs only while the Debug window shows it. GPU time to be measured in Unity (Profiler, "HB Style
Shelter").

### Step 2: wetness (compiled offline, not run in Unity)

| Variant | Slots | Reads | Added above URP Lit | Budget |
|---|---|---|---|---|
| HB Lit albedo only | 1443 | 110 | +395 / +9 | 500 / 9 — OK |
| HB Lit wetness only (`_HB_WEATHER`) | 1905 | 119 | +857 / +18 | – |
| HB Lit all features + wetness (UV) | 2640 | 128 | +1592 / +27 | 2500 / 60 — OK |
| HB Lit all features + wetness (triplanar) | 2737 | 136 | +1689 / +35 | 2500 / 60 — OK |

Wetness costs +462 slots / +9 reads over albedo only: 4 Gathers of the shelter map (4×4 B-spline), the darken
and gloss, and the wet film (probe / sky reflection and the highlight per light). Dry surfaces skip it in one
uniform branch.

### Step 3: snow cover (compiled offline, not run in Unity)

| Variant | Slots | Reads | Added above URP Lit | Budget |
|---|---|---|---|---|
| HB Lit weather only (wetness + snow) | 1925 | 119 | +877 / +18 | – |
| HB Lit all features + weather (UV) | 2671 | 128 | +1623 / +27 | 2500 / 60 — OK |
| HB Lit all features + weather (triplanar) | 2768 | 136 | +1720 / +35 | 2500 / 60 — OK |

Snow adds +20 slots over step 2 and no reads: it shares the shelter lookup with wetness.

### Step 4a: patches and delays (compiled offline, not run in Unity)

| Variant | Slots | Reads | Added above URP Lit | Budget |
|---|---|---|---|---|
| HB Lit weather only | 2128 | 119 | +1080 / +18 | – |
| HB Lit all features + weather (UV) | 2881 | 128 | +1833 / +27 | 2500 / 60 — OK |
| HB Lit all features + weather (triplanar) | 2975 | 136 | +1927 / +35 | 2500 / 60 — OK |

Patches, edge and height delays add +206 slots and no reads (edge and height reuse the shelter Gathers).
After the 4a test edge and height come from the renderer bounds instead: weather only 2055 / 119 (+1007 / +18),
all features UV 2806 / 128 (+1758 / +27), triplanar 2902 / 136 (+1854 / +35) — all within budget.

### Step 4b–4d: rain spots, ripples, running water (compiled offline, not run in Unity)

| Variant | Slots | Reads | Added above URP Lit | Budget |
|---|---|---|---|---|
| HB Lit weather only | 2609 | 119 | +1561 / +18 | – |
| HB Lit all features + weather (UV) | 3351 | 128 | +2303 / +27 | 2500 / 60 — OK |
| HB Lit all features + weather (triplanar) | 3453 | 136 | +2405 / +35 | 2500 / 60 — OK |

The three details add +554 slots and no reads; each sits in a branch that only runs while it rains, within the
detail distance and (running water) on steep faces.

### Step 5: Moving bodies, drops and flakes (compiled offline, not run in Unity)

| Variant | Slots | Reads | Added above URP Lit | Budget |
|---|---|---|---|---|
| HB Lit weather Static only | 2608 | 119 | +1560 / +18 | – |
| HB Lit weather Moving only | 3226 | 119 | +2178 / +18 | – (drops + flakes; amount from the body or the shelter) |
| HB Lit all features + weather Static (UV) | 3349 | 128 | +2301 / +27 | 2500 / 60 — OK |

| HB Lit all features + weather Moving (UV) | 3958 | 128 | +2910 / +27 | 2500 / 60 — **OVER** by 410 slots |
| HB Lit all features + weather Moving (triplanar) | 4058 | 136 | +3010 / +35 | 2500 / 60 — **OVER** by 510 slots |

Step 5 confirmed by the user (2026-10-25). Measured on 2026-10-25 (all features UV, Moving, each piece removed
alone): drops 629 slots, flakes 637 slots, both 1317 slots, no reads. The Static all-features variants stay within
budget with 199 (UV) and 97 (triplanar) slots to spare. GPU time of the shelter pass is still to be profiled in
Unity (target ≤ 0.3 ms at Ultra).

Decided 2026-10-25: drops and flakes share one 8-cell loop (each cell carries a drop or a flake). After the change:

| Variant | Slots | Reads | Added above URP Lit | Budget |
|---|---|---|---|---|
| HB Lit weather Moving only | 2687 | 119 | +1639 / +18 | – |
| HB Lit all features + weather Moving (UV) | 3416 | 128 | +2368 / +27 | 2500 / 60 — OK (132 spare) |
| HB Lit all features + weather Moving (triplanar) | 3517 | 136 | +2469 / +35 | 2500 / 60 — OK (31 spare) |

Both Moving all-features variants are now in `Tools~/ShaderVariants.psd1`, so every check measures them.

CPU per Weather Body per frame: one lookup in the read-back map, one user value write when the
packed value changes. GPU: one 32-slot compute over the shelter map for the key camera while bodies exist, and an
async readback of it (512² RG32: roof and underside height, 2 MB) every 0.2 s.

### Step 6a: near precipitation layer (compiled offline, not run in Unity)

| Shader stage | Slots | Reads |
|---|---|---|
| Rain vertex (per streak corner) | 180 | 2 (shelter map, ambient LUT) |
| Rain vertex, debug view | 187 | 2 |
| Rain fragment | 20 | 0 |
| Snow vertex | 167 | 2 |
| Snow fragment | 11 | 0 |

Lit is unchanged. One raster pass per base camera after the skybox while rain or snow falls and Precipitation
quality is not Off: one `DrawProcedural` per kind, no buffers. Drops at Ultra (density 1): up to 40 000 rain
streaks and 30 000 snow flakes (140 000 triangles at full rain and snow), scaled by Rain / Snow and the quality
density. Drops beyond the 30 m box, under roofs or in thick fog collapse in the vertex shader. Globals CBUFFER:
37 float4 (592 bytes). GPU time to be profiled in Unity (target ≤ 0.5 ms at Ultra, together with 6b–6d).

6a test fixes (3D patch noise, rain spots on slopes, no rain on downward faces, Roof Margin):

| Variant | Slots | Reads | Added above URP Lit | Budget |
|---|---|---|---|---|
| HB Lit weather Static only | 2695 | 119 | +1647 / +18 | – |
| HB Lit all features + weather Static (UV) | 3436 | 128 | +2388 / +27 | 2500 / 60 — OK (112 spare) |
| HB Lit all features + weather Static (triplanar) | 3539 | 136 | +2491 / +35 | 2500 / 60 — OK (**9 spare**) |
| Shelter Margin kernel (per texel, each of 2 passes) | 41 | 3 | absolute | – |

The 3D patch noise costs +63 slots (a trilinear 3D value noise cost +142 and was over budget). Roof Margin adds two
compute passes over the shelter map (reach 1 texel at 512²: 3 loads per texel per pass) and one R32 map per base
camera (1 MB at 512²); none when Roof Margin is 0.

Overhang-only margin (second 6a test): one more draw of the opaques in the shelter view with the underside override
shader (fragment 5 slots, no reads) into a second D16 map per base camera (0.5 MB at 512²); margin kernels 44 / 2
(X, reads top and underside) and 40 / 2 (Z), through an RG32 intermediate. Lit unchanged. GPU time to be profiled
with the shelter (target ≤ 0.3 ms at Ultra).

Overhang check in the shelter lookup (third 6a test): one Gather of the underside map, +16 slots / +1 read.

| Variant | Slots | Reads | Added above URP Lit | Budget |
|---|---|---|---|---|
| HB Lit weather Static only | 2712 | 120 | +1664 / +19 | – |
| HB Lit weather Moving only | 2703 | 120 | +1655 / +19 | – |
| HB Lit all features + weather Static (UV) | 3452 | 129 | +2404 / +28 | 2550 / 60 — OK |
| HB Lit all features + weather Static (triplanar) | 3555 | 137 | +2507 / +36 | 2550 / 60 — OK (43 spare) |
| HB Lit all features + weather Moving (triplanar) | 3533 | 137 | +2485 / +36 | 2550 / 60 — OK |

The underside draw now runs whenever the shelter map is drawn, not only with a Roof Margin.

Measured in Unity by the user (2026-10-25, Game view Statistics, whole frame, not per pass): a ~50 × 50 m sandbox
scene, every quality at Ultra, raining, FPS hands overlay: GPU frame 2.2 ms, global frame 3.2 ms (311 FPS),
162.2k triangles, 71 draw calls (52 SRP Batcher, 11 not SRP compatible — likely the underside override-shader
draws). 6a accepted; on to 6b.

### Far shelter map and 1000 m depth window (compiled offline, not run in Unity)

| Variant | Slots | Reads | Added above URP Lit | Budget |
|---|---|---|---|---|
| HB Lit weather Static only | 2746 | 121 | +1698 / +20 | – |
| HB Lit all features + weather Static (UV) | 3488 | 130 | +2440 / +29 | 2550 / 60 — OK |
| HB Lit all features + weather Static (triplanar) | 3591 | 138 | +2543 / +37 | 2550 / 60 — OK (**7 spare**) |
| HB Lit all features + weather Moving (triplanar) | 3569 | 138 | +2521 / +37 | 2550 / 60 — OK |

The far fallback costs +36 slots / +1 read on Lit (one Gather, bilinear). It first measured +45 (2 over budget);
sharing one edge distance between the bounds test and the blend brought it back. GPU: one extra top-down depth
draw of the opaques within 1 km per base camera, only when the camera has moved 50 m; one 512² D16 map per base
camera (0.5 MB). The depth window change costs nothing.

### Step 6d: wind ribbons (compiled offline, not run in Unity)

| Shader stage | Slots | Reads |
|---|---|---|
| Wind vertex (per corner) | 268 | 2 (shelter map, ambient LUT) |
| Wind vertex, debug view | 274 | 2 |
| Wind fragment | 9 | 0 |

At most 12 ribbons × 20 segments = 480 triangles (1440 vertices) while Wind is above 0.3. Lit unchanged.

### Step 6c: rain splashes — removed

Built (vertex 222 slots / 5 reads, fragment 24, up to 20k triangles) and removed after the test: not visible at a
real viewing distance. No cost remains.

### Step 6b: far precipitation layer (compiled offline, not run in Unity)

| Shader stage | Slots | Reads |
|---|---|---|
| Far vertex (rain / snow) | 27 / 26 | 1 (ambient LUT, per vertex) |
| Far rain fragment | 196 | 1 (shelter map) |
| Far rain fragment, debug view | 205 | 1 |
| Far snow fragment | 184 | 1 |

Two curtains = 256 triangles per kind, drawn only when Precipitation quality Far is on and it rains / snows. The
cost is per pixel: both curtains cover the sky and anything beyond 20 m / 60 m (up to two full-screen layers of
~190 slots each at full rain or snow). GPU time to be profiled with 6a (target ≤ 0.5 ms at Ultra for all
precipitation).

History — first batch (removed):

| Variant | Slots | Reads | Added above URP Lit | Budget |
|---|---|---|---|---|
| HB Lit albedo only, Full keywords | 1443 | 110 | +395 / +9 | 500 / 9 — OK, unchanged |
| HB Lit weather Static only | 3132 | 172 | +2084 / +71 | – |
| HB Lit weather Moving only | 3979 | 172 | +2931 / +71 | – |
| HB Lit all features (UV, weather Static) | 3882 | 181 | +2834 / +80 | 2500 / 60 — **OVER** |
| HB Lit all features (triplanar, weather Static) | 3979 | 189 | +2931 / +88 | 2500 / 60 — **OVER** |
| Precipitation vertex (rain / snow / wind) | 299 / 284 / 390 | 9 | absolute | – |
| Shelter compute per texel (Close / Follow / Open / Reach / Far) | 373 / 82 / 110 / 2457 / 31 | 25 / 4 / 23 / 113 / 1 | absolute | – |

The approved old snow / wet look was ported first (+3371 slots / +103 reads on Lit). Look-neutral steps since:
Static and Moving split into two keywords (−1052 slots for Static) and the snow and rain-map shelter passes merged
into one loop that shares the floor, fade and far-map reads (−530 slots, −41 reads). Weather only costs dynamically
when it is active: dry surfaces take one uniform branch.

Where the remaining Static weather cost sits (slots / reads, each measured by removing that piece alone):

| Piece | Cost | What it does |
|---|---|---|
| Wet film | 355 / 5 | gloss reflection and highlight on wet surfaces without Specular |
| Rain spots | 293 / 0 | rain first spots the ground with growing drops |
| Ripples | 134 / 0 | rings on flat wet ground while it rains |
| Ledge + height | 133 / 0 | snow later on ledge rims and low parts of objects |
| Running water | 116 / 0 | streaks down steep faces while it rains |
| Patches | 103 / 0 | wet and snow settle in patches |
| Far shelter map | 100 / 8 | roofs beyond 160 m keep weather off |
| Snow block | 81 / 0 | slope and pattern of the snow cover |
| Rain past small things | 79 / 16 | rain reaches under objects smaller than ~1.5 m |
| Core | ~295 / 33 | shelter taps, reach floor, fade, applying wet and snow |

Over budget by 334 slots / 20 reads (UV) and 431 / 28 (triplanar). Decided 2026-10-24: keep the full approved
look for the Phase 3 test and choose the trade-off (pieces or budget) after seeing it in Unity.

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
