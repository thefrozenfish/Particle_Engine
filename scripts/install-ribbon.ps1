param([ValidateSet('Debug','Release')][string]$Configuration = 'Release')
$ErrorActionPreference = 'Stop'
$rhinoProcesses = @(Get-Process Rhino* -ErrorAction SilentlyContinue)
if ($rhinoProcesses.Count -gt 0) {
    $details = ($rhinoProcesses | ForEach-Object { $_.ProcessName + ' (PID ' + $_.Id + ')' }) -join ', '
    throw "Close Rhino and any Rhino Compute process before installing. Still running: $details"
}
$repoRoot = Split-Path -Parent $PSScriptRoot
$source = Join-Path $repoRoot "src\ParticleEngine.Grasshopper\bin\$Configuration\netstandard2.0"
$libraries = Join-Path $env:APPDATA 'Grasshopper\Libraries'
$files = @('ParticleEngine.gha','ParticleEngine.Core.dll')
foreach ($name in $files) {
    if (-not (Test-Path -LiteralPath (Join-Path $source $name))) { throw "Build output missing: $name" }
}
$existing = @()
if (Test-Path -LiteralPath $libraries) {
    $existing = @(Get-ChildItem -LiteralPath $libraries -Recurse -File -Filter 'ParticleEngine.gha')
}
if ($existing.Count -gt 1) {
    throw ('Multiple Particle Engine installations found. Keep one installation before continuing: ' + (($existing | ForEach-Object FullName) -join ', '))
}
$dest = Join-Path $libraries 'ParticleEngine-dev'
if ($existing.Count -eq 1) { $dest = $existing[0].DirectoryName }
$backup = Join-Path $repoRoot ('.install-backups\' + [Guid]::NewGuid().ToString('N'))
New-Item -ItemType Directory -Path $backup -Force | Out-Null
New-Item -ItemType Directory -Path $dest -Force | Out-Null
# Backups stay outside Libraries, avoiding duplicate plugin discovery.
foreach ($name in $files) {
    $old = Join-Path $dest $name
    if (Test-Path -LiteralPath $old) { Copy-Item -LiteralPath $old -Destination $backup }
}
try {
    foreach ($name in $files) {
        $from = Join-Path $source $name
        $to = Join-Path $dest $name
        Copy-Item -LiteralPath $from -Destination $to -Force
        Unblock-File -LiteralPath $to
        if ((Get-FileHash -LiteralPath $from).Hash -ne (Get-FileHash -LiteralPath $to).Hash) {
            throw "Installed file verification failed: $name"
        }
    }
} catch {
    foreach ($name in $files) {
        $saved = Join-Path $backup $name
        if (Test-Path -LiteralPath $saved) { Copy-Item -LiteralPath $saved -Destination (Join-Path $dest $name) -Force }
    }
    throw
}
Write-Host "Installed and verified: $dest"
Write-Host "Previous files saved in: $backup"
Write-Host 'Start Rhino and Grasshopper, then check the Particle Engine ribbon tab.'
