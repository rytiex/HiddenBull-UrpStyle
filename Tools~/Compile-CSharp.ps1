param([string]$Out)

. (Join-Path $PSScriptRoot 'Common.ps1')

$dir = Get-OutDir $Out 'cs'

$refs = @()
$refs += Get-ChildItem (Join-Path $UnityData 'Managed\UnityEngine\*.dll') | ForEach-Object FullName
$refs += Join-Path $UnityData 'NetStandard\ref\2.1.0\netstandard.dll'
$refs += Join-Path $UnityData 'NetStandard\compat\2.1.0\shims\netfx\mscorlib.dll'
$refs += Get-ChildItem (Join-Path $Sandbox 'Library\ScriptAssemblies\*.dll') |
         Where-Object { $_.Name -notlike 'HiddenBull.*' -and $_.Name -notlike 'Assembly-CSharp*' } |
         ForEach-Object FullName

$failed = $false

function Build([string]$Leaf, [string]$Name, [string]$Defines, [string]$Source, [string[]]$Extra) {
    $target = Join-Path $dir $Leaf
    New-Item -ItemType Directory -Force $target | Out-Null

    $rsp = @('-target:library', '-unsafe+', '-langversion:9.0', '-nostdlib+', '-nologo', "-define:$Defines",
             '-nowarn:0169,0649,0282,1701,1702', '-preferreduilang:en-US',
             ('-out:"' + (Join-Path $target "$Name.dll") + '"'))
    $rsp += ($refs + $Extra) | ForEach-Object { "-r:`"$_`"" }
    $rsp += Get-ChildItem -Recurse (Join-Path $Source '*.cs') | ForEach-Object { "`"$($_.FullName)`"" }

    $file = Join-Path $target "$Name.rsp"
    $rsp | Set-Content -Encoding utf8 $file

    $output = & $Dotnet $Csc "@$file"
    $code = $LASTEXITCODE
    $lines = @($output | Where-Object { $_ -match 'error CS|warning CS' })

    Write-Host ("[{0}] {1}: {2}" -f $Leaf, $Name, $(if ($code -eq 0) { 'OK' } else { 'FAILED' }))
    $lines | ForEach-Object { Write-Host "  $_" }

    if ($code -ne 0) { $script:failed = $true }
}

$editor = 'UNITY_EDITOR;UNITY_6000_0_OR_NEWER;UNITY_2023_1_OR_NEWER'
$player = 'UNITY_6000_0_OR_NEWER;UNITY_2023_1_OR_NEWER;UNITY_STANDALONE'
$runtime = Join-Path $Package 'Runtime'

Build 'ed' 'HiddenBull.UrpStyle.Runtime' $editor $runtime @()
Build 'pl' 'HiddenBull.UrpStyle.Runtime' $player $runtime @()
Build 'ed' 'HiddenBull.UrpStyle.Editor' $editor (Join-Path $Package 'Editor') `
      @((Join-Path $dir 'ed\HiddenBull.UrpStyle.Runtime.dll'))

if ($failed) { exit 1 }
exit 0
