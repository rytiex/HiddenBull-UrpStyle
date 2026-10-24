$ErrorActionPreference = 'Stop'

$Script:Repo = Split-Path -Parent $PSScriptRoot
$Script:Package = Join-Path $Repo 'Package'
$Script:Sandbox = Join-Path $Repo 'HiddenBullUrpStyleSandbox'

if (-not $env:HB_UNITY) { $env:HB_UNITY = 'C:\Program Files\Unity\Hub\Editor\6000.4.1f1\Editor' }
if (-not $env:HB_WINSDK) { $env:HB_WINSDK = 'C:\Program Files (x86)\Windows Kits\10\bin\10.0.22621.0\x64' }

$Script:UnityData = Join-Path $env:HB_UNITY 'Data'
$Script:BuiltInPackages = Join-Path $UnityData 'Resources\PackageManager\BuiltInPackages'
$Script:Dotnet = Join-Path $UnityData 'NetCoreRuntime\dotnet.exe'
$Script:Csc = Join-Path $UnityData 'DotNetSdkRoslyn\csc.dll'
$Script:Dxc = Join-Path $env:HB_WINSDK 'dxc.exe'
$Script:Fxc = Join-Path $env:HB_WINSDK 'fxc.exe'

function Get-OutDir([string]$Out, [string]$Leaf) {
    if (-not $Out) { $Out = Join-Path $env:TEMP 'hb-urpstyle-tools' }
    $dir = Join-Path $Out $Leaf
    New-Item -ItemType Directory -Force $dir | Out-Null
    return $dir
}
