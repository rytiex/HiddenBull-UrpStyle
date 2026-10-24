param([string]$Out)

$failed = $false

foreach ($step in @('Check-CBufferLayout.ps1', 'Compile-CSharp.ps1', 'Compile-Shaders.ps1')) {
    Write-Host "== $step"
    & (Join-Path $PSScriptRoot $step) -Out $Out
    if ($LASTEXITCODE -ne 0) { $failed = $true }
}

if ($failed) { Write-Host 'FAILED'; exit 1 }
Write-Host 'All checks passed (compiled offline, not run in Unity).'
exit 0
