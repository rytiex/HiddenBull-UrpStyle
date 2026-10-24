# 07 — Offline Tooling (run before every hand-off)

The machine has no Python or Node. Use PowerShell, Unity's bundled dotnet/Roslyn and the Windows SDK.
Rebuild these harnesses in the session scratchpad (they are not kept between sessions), or commit them
under `Tools~/` in Phase 0 so they persist.

Paths on the user's machine:

| Tool | Path |
|---|---|
| Unity | `C:\Program Files\Unity\Hub\Editor\6000.4.1f1\Editor` |
| URP / Core source | `...\Editor\Data\Resources\PackageManager\BuiltInPackages\com.unity.render-pipelines.*` |
| dxc / fxc | `C:\Program Files (x86)\Windows Kits\10\bin\10.0.22621.0\x64\` |
| dotnet + Roslyn | `...\Editor\Data\NetCoreRuntime\dotnet.exe`, `...\Editor\Data\DotNetSdkRoslyn\csc.dll` |
| Sandbox assemblies | `HiddenBullUrpStyleSandbox\Library\ScriptAssemblies` |

## 1. Shader compile + cost (dxc preprocess, fxc compile)

1. Make a folder with a `Packages` subfolder of **junctions**: `com.unity.render-pipelines.universal`,
   `com.unity.render-pipelines.core`, `com.unity.render-pipelines.universal-config` → BuiltInPackages,
   and `com.hiddenbull.urpstyle` → the repo's `Package` folder.
2. Per variant, a wrapper `.hlsl`:

```hlsl
#define SHADER_API_D3D11 1
#define UNITY_VERSION 60000041
#define SHADER_TARGET 35
#define SHADER_STAGE_FRAGMENT 1
#define SHADER_AVAILABLE_INTERPOLATORS15 1
#define _MAIN_LIGHT_SHADOWS_CASCADE 1
#define _SHADOWS_SOFT 1
// material / pass keywords of the variant here
#include "Packages/com.hiddenbull.urpstyle/Runtime/Shaders/<Family>/<Pass>.hlsl"
```

3. `dxc -P out.pre.hlsl -I <root> wrapper.hlsl` then
   `fxc /nologo /Gec /T ps_5_0 /E <EntryPoint> /Fc out.asm out.pre.hlsl` (vs_5_0 for vertex).
4. Cost: instruction slots from the fxc summary; texture reads =
   count of lines matching `^\s*(sample|ld_)` in the `.asm`. Nesting in the asm shows whether reads sit inside
   dynamic branches.

Why: fxc's own preprocessor rejects `#define PopMarker()` in core `Common.hlsl` (hence dxc -P); `/Gec` because
URP declares `half` globals; **do not define `INSTANCING_ON`** (dxc does not expand macro args before `##`
the way Unity does). X4000 "potentially uninitialized" warnings match what Unity reports.

## 2. C# compile (Runtime, player defines, Editor)

```powershell
$u   = "C:\Program Files\Unity\Hub\Editor\6000.4.1f1\Editor\Data"
$pkg = "C:\Users\rytie\Documents\Github\HiddenBull-UrpStyle\Package"
$out = "<scratchpad>\cs"
$refs = @()
$refs += Get-ChildItem "$u\Managed\UnityEngine\*.dll" | ForEach-Object FullName
$refs += "$u\NetStandard\ref\2.1.0\netstandard.dll"
$refs += "$u\NetStandard\compat\2.1.0\shims\netfx\mscorlib.dll"
$refs += Get-ChildItem "<repo>\HiddenBullUrpStyleSandbox\Library\ScriptAssemblies\*.dll" |
         Where-Object { $_.Name -notlike "HiddenBull.*" -and $_.Name -notlike "Assembly-CSharp*" } |
         ForEach-Object FullName
function Build($dir, $name, $defines, $srcDir, $extra) {
  $rsp = @("-target:library","-unsafe+","-langversion:9.0","-nostdlib+","-nologo","-define:$defines",
           ("-out:" + "$out\$dir\$name.dll"))
  $rsp += ($refs + $extra) | ForEach-Object { "-r:`"$_`"" }
  $rsp += Get-ChildItem -Recurse "$srcDir\*.cs" | ForEach-Object { "`"$($_.FullName)`"" }
  $rsp | Set-Content -Encoding utf8 "$out\$dir\$name.rsp"
  & "$u\NetCoreRuntime\dotnet.exe" "$u\DotNetSdkRoslyn\csc.dll" "@$out\$dir\$name.rsp"
}
$ed = "UNITY_EDITOR;UNITY_6000_0_OR_NEWER;UNITY_2023_1_OR_NEWER"
Build "ed" "HiddenBull.UrpStyle.Runtime" $ed "$pkg\Runtime" @()
Build "pl" "HiddenBull.UrpStyle.Runtime" "UNITY_6000_0_OR_NEWER;UNITY_2023_1_OR_NEWER;UNITY_STANDALONE" `
      "$pkg\Runtime" @()
Build "ed" "HiddenBull.UrpStyle.Editor" $ed "$pkg\Editor" @("$out\ed\HiddenBull.UrpStyle.Runtime.dll")
```

- Output dll names **must equal the asmdef names** (InternalsVisibleTo).
- Do NOT add `Managed\UnityEditor.dll` (duplicates → CS0433); mscorlib shim is required (CS0012 'Object').
- Show every `warning CS` (CS0618 deprecated APIs, CS0108 hidden members), not only errors.
- It does **not** enforce asmdef references — check them by hand.
- Build sandbox scripts too when renaming public APIs.

## 3. Simulation

- Quick: PowerShell `Add-Type -TypeDefinition $src -Language CSharp` (C# 5 only: no interpolation, no
  `out var`, no expression-bodied members).
- Larger: build a net8 console exe with the same csc (`-target:exe`, refs from
  `NetCoreRuntime\shared\Microsoft.NETCore.App\8.0.16\System*.dll` minus `*.Native.dll`) and run it with
  Unity's `dotnet.exe` plus a hand-written `runtimeconfig.json` (tfm net8.0, framework 8.0.16).
- Mirror shader maths in float, compare with a brute-force double reference, report measured error rates.
- Images: write BMP, convert to PNG with `System.Drawing`, then look at it.

## 4. Git

- Branch `refactor/v1`. The user commits and pushes; never commit.
- `core.autocrlf = true`: working tree is CRLF. Scripted edits must preserve CRLF.
