# Offline Tools

PowerShell scripts that compile and measure the package outside Unity. Run them before every hand-off
(see `Docs/Refactor/07-Tooling.md`). Output goes to `-Out <folder>` (default `%TEMP%\hb-urpstyle-tools`).

| Script | What it does |
|---|---|
| `Check-All.ps1` | Runs the three checks below; exit code 1 if any fails. |
| `Check-CBufferLayout.ps1` | Compares each C# constant-buffer struct with its HLSL `CBUFFER` (names, types, offsets, size). |
| `Compile-CSharp.ps1` | Builds Runtime (editor + player defines) and Editor with Unity's Roslyn; prints every `warning CS`. |
| `Compile-Shaders.ps1` | Preprocesses with dxc, compiles with fxc and prints instruction slots and texture reads per variant against its budget. `-Filter` selects variants by name. |
| `New-Meta.ps1` | Writes a `.meta` with a fresh GUID for every file and folder in `Package/` that has none. `-WhatIf` lists them only. |

- Variants and budgets live in `ShaderVariants.psd1`. `Urp = 'Full'` adds the heaviest URP keyword set
  (cascades, soft shadows, cluster light loop, additional light shadows, lightmap + shadowmask, light layers,
  cookies, SSAO, DBuffer); budgets in `01-Vision-and-Rules.md` are measured with it.
- Texture reads count `sample*`, `ld_*` and `gather*` instructions in the fxc listing.
- Paths default to Unity `6000.4.1f1` and Windows SDK `10.0.22621.0`; override with `$env:HB_UNITY` and
  `$env:HB_WINSDK`.
- The C# build references every Unity assembly, so it does **not** catch a missing asmdef reference.
