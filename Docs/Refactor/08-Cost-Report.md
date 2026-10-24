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

Globals CBUFFER `HBStyleFrame` (48 bytes):

| Field | x | y | z | w |
|---|---|---|---|---|
| `_HB_Environment` | time of day (h) | – | – | – |
| `_HB_Weather` | rain 0–1 | snow 0–1 | wind 0–1 | – |
| `_HB_Debug` | debug view | – | – | – |
