<#
.SYNOPSIS
  Copies a built Particle Engine plugin into Grasshopper's Libraries folder for development testing (Windows).

.DESCRIPTION
  Close Rhino first. Copies ParticleEngine.gha and ParticleEngine.Core.dll together into a single
  dev folder and removes the previous copy. Use -Target net48 when Rhino is running the .NET Framework
  runtime (and for Rhino 7), net7.0 when Rhino 8 is running modern .NET.

  If you also installed the plugin through Yak or by hand elsewhere, remove that copy: two installs of the
  same plugin cause confusing load behaviour.

.EXAMPLE
  pwsh scripts/install-dev.ps1 -Target net7.0
  pwsh scripts/install-dev.ps1 -Target net48 -Configuration Release
#>
param(
    [ValidateSet('net48', 'net7.0')]
    [string]$Target = 'net7.0',

    [ValidateSet('Debug', 'Release')]
    [string]$Configuration = 'Debug'
)

$ErrorActionPreference = 'Stop'

$rhino = Get-Process -ErrorAction SilentlyContinue | Where-Object { $_.Name -like 'Rhino*' }
if ($rhino) {
    $names = ($rhino | Select-Object -ExpandProperty Name -Unique) -join ', '
    throw "Rhino is running (process: $names). Save your work and close ALL Rhino windows, then run this again. Closing only the Grasshopper window is not enough - Rhino keeps the plugin loaded."
}

$repoRoot = Split-Path -Parent $PSScriptRoot
$source   = Join-Path $repoRoot "src\ParticleEngine.Grasshopper\bin\$Configuration\$Target"
$dest     = Join-Path $env:APPDATA 'Grasshopper\Libraries\ParticleEngine-dev'

$gha  = Join-Path $source 'ParticleEngine.gha'
$core = Join-Path $source 'ParticleEngine.Core.dll'

foreach ($file in @($gha, $core)) {
    if (-not (Test-Path $file)) {
        throw "Missing build output: $file. Build first: dotnet build src/ParticleEngine.Grasshopper -c $Configuration"
    }
}

if (Test-Path $dest) {
    try {
        Remove-Item -Recurse -Force $dest -ErrorAction Stop
    }
    catch {
        throw "Could not replace the dev install at '$dest': a file there is locked. ParticleEngine.gha is still loaded by a running Rhino. Close ALL Rhino processes (check Task Manager for Rhino.exe) and run this again."
    }
}
New-Item -ItemType Directory -Path $dest -Force | Out-Null

Copy-Item $gha  $dest
Copy-Item $core $dest
Get-ChildItem $dest | Unblock-File

Write-Host "Installed $Target ($Configuration) to $dest"
Write-Host 'Start Rhino, then Grasshopper. If components do not appear, check File > Special Folders > Components Folder for the right Libraries location.'
