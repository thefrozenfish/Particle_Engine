# Stages a Yak package for Particle Engine and (optionally) builds the .yak locally.
# It NEVER pushes to the Yak server — publishing is a deliberate owner step, run by hand once the
# behaviour is validated against Culebra. The script prints the exact push command at the end.
#
#   powershell -NoProfile -File scripts/build-yak.ps1            # stage + build the .yak if yak is on PATH
#   powershell -NoProfile -File scripts/build-yak.ps1 -StageOnly # just stage the folder, don't call yak
#
# Output goes to dist/ (gitignored). The committed manifest is packaging/manifest.yml.
#
# Packaging note (deviation from BUILD_SPEC 2.4): the plugin is a single netstandard2.0 assembly that
# Rhino 8 loads on BOTH the .NET Framework and modern-.NET runtimes, and Rhino 7 loads too. So there is
# no net48/ vs net7.0/ split to ship — one set of files serves every target. Distribution tags are set
# at push time (see the printed command): rh8 covers win/mac on Rhino 8.

param(
    [switch]$StageOnly,
    [string]$Configuration = 'Release'
)

$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $PSScriptRoot
$ghOut = Join-Path $root "src/ParticleEngine.Grasshopper/bin/$Configuration/netstandard2.0"
$stage = Join-Path $root 'dist/pkg'

$gha  = Join-Path $ghOut 'ParticleEngine.gha'
$core = Join-Path $ghOut 'ParticleEngine.Core.dll'
if (-not (Test-Path $gha) -or -not (Test-Path $core)) {
    throw "Build first: '$gha' or its Core.dll is missing. Run: dotnet build ParticleEngine.sln -c $Configuration"
}

# Fresh staging folder with ONLY the two assemblies + manifest + icon (never ship RhinoCommon/GH/GH_IO).
if (Test-Path $stage) { Remove-Item -Recurse -Force $stage }
New-Item -ItemType Directory -Force -Path $stage | Out-Null
Copy-Item $gha  $stage
Copy-Item $core $stage
Copy-Item (Join-Path $root 'packaging/manifest.yml') $stage
Copy-Item (Join-Path $root 'src/ParticleEngine.Grasshopper/Resources/ParticleEngine24.png') $stage

Write-Host "Staged package contents in $stage :"
Get-ChildItem $stage | ForEach-Object { Write-Host "  $($_.Name)" }

if ($StageOnly) {
    Write-Host "`n-StageOnly: not calling yak."
    return
}

$yak = (Get-Command yak -ErrorAction SilentlyContinue)
if (-not $yak) {
    $yak = (Get-Command 'C:\Program Files\Rhino 8\System\Yak.exe' -ErrorAction SilentlyContinue)
}
if (-not $yak) {
    Write-Host "`nYak not found on PATH. Install it with Rhino, then re-run, or build manually:"
    Write-Host "  cd `"$stage`"; yak build"
} else {
    Push-Location $stage
    try { & $yak.Source build } finally { Pop-Location }
    Write-Host "`nBuilt .yak in $stage."
}

Write-Host ""
Write-Host "PUBLISH IS HELD. When the behaviour is validated and you are ready to release, run by hand:"
Write-Host "  cd `"$stage`""
Write-Host "  yak push <the-built>.yak        # pushes to the public Yak server"
Write-Host "Set distribution tags for Rhino 8 (win/mac) at push time; a second rh7 push if Rhino 7 is supported."
