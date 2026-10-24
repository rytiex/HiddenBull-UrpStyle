. (Join-Path $PSScriptRoot 'Common.ps1')

$pairs = @(
    @{
        CSharp = 'Runtime/Rendering/StyleFrameGlobals.cs'; Struct = 'StyleFrameGlobals'
        Hlsl = 'Runtime/Shaders/Library/Core/StyleGlobals.hlsl'; Buffer = 'HBStyleFrame'
    }
)

$csSizes = @{ 'float' = 4; 'int' = 4; 'uint' = 4; 'Vector2' = 8; 'Vector3' = 12; 'Vector4' = 16
              'Color' = 16; 'Matrix4x4' = 64 }
$hlslSizes = @{ 'float' = 4; 'int' = 4; 'uint' = 4; 'float2' = 8; 'float3' = 12; 'float4' = 16
                'int4' = 16; 'uint4' = 16; 'float4x4' = 64 }

function Read-CSharp([string]$Path, [string]$Struct) {
    $text = Get-Content -Raw (Join-Path $Package $Path)
    $start = [regex]::Match($text, "struct\s+$Struct\b")
    if (-not $start.Success) { throw "struct $Struct not found in $Path" }
    $fields = @()
    $offset = 0
    foreach ($m in [regex]::Matches($text.Substring($start.Index), '(?m)^\s*public\s+(\w+)\s+(\w+)\s*;')) {
        $type = $m.Groups[1].Value
        if (-not $csSizes.ContainsKey($type)) { throw "Unknown C# type $type in $Struct" }
        $fields += [pscustomobject]@{ Name = $m.Groups[2].Value; Type = $type; Offset = $offset
                                      Size = $csSizes[$type] }
        $offset += $csSizes[$type]
    }
    return @{ Fields = $fields; Size = $offset }
}

function Read-Hlsl([string]$Path, [string]$Buffer) {
    $text = Get-Content -Raw (Join-Path $Package $Path)
    $body = [regex]::Match($text, "CBUFFER_START\($Buffer\)(?<b>[\s\S]*?)CBUFFER_END")
    $fields = @()
    $offset = 0
    foreach ($m in [regex]::Matches($body.Groups['b'].Value, '(?m)^\s*(\w+)\s+(\w+)\s*;')) {
        $type = $m.Groups[1].Value
        if (-not $hlslSizes.ContainsKey($type)) { throw "Unknown HLSL type $type in $Buffer" }
        $size = $hlslSizes[$type]
        $register = [math]::Floor($offset / 16)
        if ($size -ge 16 -or [math]::Floor(($offset + $size - 1) / 16) -ne $register) {
            $offset = [math]::Ceiling($offset / 16) * 16
        }
        $fields += [pscustomobject]@{ Name = $m.Groups[2].Value; Type = $type; Offset = $offset; Size = $size }
        $offset += $size
    }
    return @{ Fields = $fields; Size = [math]::Ceiling($offset / 16) * 16 }
}

$failed = $false

foreach ($pair in $pairs) {
    $cs = Read-CSharp $pair.CSharp $pair.Struct
    $hl = Read-Hlsl $pair.Hlsl $pair.Buffer
    $errors = @()

    if ($cs.Fields.Count -ne $hl.Fields.Count) {
        $errors += "field count C# $($cs.Fields.Count) vs HLSL $($hl.Fields.Count)"
    }

    for ($i = 0; $i -lt [math]::Min($cs.Fields.Count, $hl.Fields.Count); $i++) {
        $a = $cs.Fields[$i]
        $b = $hl.Fields[$i]
        if ($a.Name -ne $b.Name) { $errors += "field $i name $($a.Name) vs $($b.Name)" }
        if ($a.Offset -ne $b.Offset) { $errors += "$($a.Name) offset C# $($a.Offset) vs HLSL $($b.Offset)" }
        if ($a.Size -ne $b.Size) { $errors += "$($a.Name) type C# $($a.Type) vs HLSL $($b.Type)" }
    }

    if ($cs.Size -ne $hl.Size) { $errors += "size C# $($cs.Size) vs HLSL $($hl.Size) (pad the C# struct)" }

    if ($errors.Count -eq 0) {
        Write-Host "[$($pair.Buffer)] OK: $($cs.Fields.Count) fields, $($cs.Size) bytes"
    } else {
        Write-Host "[$($pair.Buffer)] MISMATCH"
        $errors | ForEach-Object { Write-Host "  $_" }
        $failed = $true
    }
}

if ($failed) { exit 1 }
exit 0
