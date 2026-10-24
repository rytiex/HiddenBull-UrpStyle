param([switch]$WhatIf)

. (Join-Path $PSScriptRoot 'Common.ps1')

$importers = @{
    '.cs' = "MonoImporter:`n  externalObjects: {}`n  serializedVersion: 2`n  defaultReferences: []`n" +
            "  executionOrder: 0`n  icon: {instanceID: 0}`n"
    '.hlsl' = "ShaderIncludeImporter:`n  externalObjects: {}`n"
    '.shader' = "ShaderImporter:`n  externalObjects: {}`n  defaultTextures: []`n  nonModifiableTextures: []`n"
    '.compute' = "ComputeShaderImporter:`n  externalObjects: {}`n"
    '.asmdef' = "AssemblyDefinitionImporter:`n  externalObjects: {}`n"
    '.json' = "TextScriptImporter:`n  externalObjects: {}`n"
    '.md' = "TextScriptImporter:`n  externalObjects: {}`n"
}

$tail = "  userData: `n  assetBundleName: `n  assetBundleVariant: `n"

function Test-Hidden([string]$Path) {
    $relative = $Path.Substring($Package.Length).TrimStart('\')
    foreach ($part in $relative.Split('\')) {
        if ($part.StartsWith('.') -or $part.EndsWith('~')) { return $true }
    }
    return $false
}

$created = 0

Get-ChildItem -Recurse -Force $Package | Where-Object { $_.Extension -ne '.meta' -and -not (Test-Hidden $_.FullName) } |
    ForEach-Object {
        $meta = $_.FullName + '.meta'
        if (Test-Path $meta) { return }

        $guid = [guid]::NewGuid().ToString('N')
        if ($_.PSIsContainer) {
            $body = "folderAsset: yes`nDefaultImporter:`n  externalObjects: {}`n"
        } elseif ($importers.ContainsKey($_.Extension)) {
            $body = $importers[$_.Extension]
        } else {
            $body = "DefaultImporter:`n  externalObjects: {}`n"
        }

        $text = ("fileFormatVersion: 2`nguid: $guid`n" + $body + $tail) -replace "`n", "`r`n"
        Write-Host "meta: $($_.FullName.Substring($Package.Length + 1))"
        if (-not $WhatIf) { [IO.File]::WriteAllText($meta, $text) }
        $created++
    }

Write-Host "$created meta file(s)"
