param([string]$Out, [string]$Filter = '*', [string]$Manifest)

. (Join-Path $PSScriptRoot 'Common.ps1')

$ErrorActionPreference = 'Continue'

$dir = Get-OutDir $Out 'shaders'
$root = Join-Path $dir 'root'
$packages = Join-Path $root 'Packages'
New-Item -ItemType Directory -Force $packages | Out-Null

$links = @{
    'com.unity.render-pipelines.universal' = Join-Path $BuiltInPackages 'com.unity.render-pipelines.universal'
    'com.unity.render-pipelines.core' = Join-Path $BuiltInPackages 'com.unity.render-pipelines.core'
    'com.unity.render-pipelines.universal-config' =
        Join-Path $BuiltInPackages 'com.unity.render-pipelines.universal-config'
    'com.hiddenbull.urpstyle' = $Package
}

foreach ($name in $links.Keys) {
    $link = Join-Path $packages $name
    if (-not (Test-Path $link)) { New-Item -ItemType Junction -Path $link -Target $links[$name] | Out-Null }
}

$tools = Join-Path $root 'Tools~'
if (-not (Test-Path $tools)) { New-Item -ItemType Junction -Path $tools -Target $PSScriptRoot | Out-Null }

if (-not $Manifest) { $Manifest = Join-Path $PSScriptRoot 'ShaderVariants.psd1' }
$data = Import-PowerShellDataFile $Manifest
$results = @()
$measured = @{}
$failed = $false

foreach ($variant in $data.Variants) {
    if ($variant.Name -notlike $Filter -and -not $variant.IsReference) { continue }

    $slug = ($variant.Name -replace '[^A-Za-z0-9]+', '_').Trim('_')
    $stage = if ($variant.Stage -eq 'vs') { 'VERTEX' } else { 'FRAGMENT' }

    $lines = @(
        '#define SHADER_API_D3D11 1',
        '#define UNITY_VERSION 60000041',
        '#define SHADER_TARGET 35',
        "#define SHADER_STAGE_$stage 1",
        '#define SHADER_AVAILABLE_INTERPOLATORS15 1'
    )
    $lines += $data.Urp[$variant.Urp] | ForEach-Object { "#define $_ 1" }
    $lines += $variant.Defines | ForEach-Object { "#define $_ 1" }
    $lines += "#include `"$($variant.Include)`""

    $wrapper = Join-Path $dir "$slug.wrapper.hlsl"
    $pre = Join-Path $dir "$slug.pre.hlsl"
    $asm = Join-Path $dir "$slug.asm"
    $lines | Set-Content -Encoding ascii $wrapper

    $dxcOut = & $Dxc -P $pre -I $root $wrapper 2>&1
    if ($LASTEXITCODE -ne 0) {
        Write-Host "[$($variant.Name)] preprocess FAILED"
        $dxcOut | ForEach-Object { Write-Host "  $_" }
        $failed = $true
        continue
    }

    $fxcOut = & $Fxc /nologo /Gec /T "$($variant.Stage)_5_0" /E $variant.Entry /Fc $asm $pre 2>&1
    $messages = @($fxcOut | ForEach-Object { "$_" } | Where-Object { $_ -match 'error|warning' })

    if ($LASTEXITCODE -ne 0) {
        Write-Host "[$($variant.Name)] compile FAILED"
        $messages | ForEach-Object { Write-Host "  $_" }
        $failed = $true
        continue
    }

    $text = Get-Content $asm
    $slots = 0
    $match = $text | Select-String 'Approximately (\d+) instruction slots used' | Select-Object -Last 1
    if ($match) { $slots = [int]$match.Matches[0].Groups[1].Value }
    $reads = @($text | Where-Object { $_ -match '^\s*(sample|ld_|gather)' }).Count

    $measured[$variant.Name] = @{ Slots = $slots; Reads = $reads }

    $addedSlots = $slots
    $addedReads = $reads
    $status = 'OK'

    if ($variant.Reference) {
        $reference = $measured[$variant.Reference]
        if ($reference) {
            $addedSlots = $slots - $reference.Slots
            $addedReads = $reads - $reference.Reads
        } else {
            $status = 'NO REF'
        }
    }

    if ($variant.Slots -gt 0 -and $addedSlots -gt $variant.Slots) { $status = 'OVER' }
    if ($variant.Reads -gt 0 -and $addedReads -gt $variant.Reads) { $status = 'OVER' }

    $results += [pscustomobject]@{
        Variant = $variant.Name
        Slots = $slots
        Reads = $reads
        AddedSlots = $(if ($variant.Reference) { $addedSlots } else { '-' })
        AddedReads = $(if ($variant.Reference) { $addedReads } else { '-' })
        Budget = $(if ($variant.Slots -gt 0) { "$($variant.Slots) / $($variant.Reads)" } else { '-' })
        Warnings = $messages.Count
        Status = $status
    }

    $messages | ForEach-Object { Write-Host "[$($variant.Name)] $_" }
}

$results | Format-Table -AutoSize | Out-String | Write-Host

if ($failed) { exit 1 }
exit 0
