# Generates original 24x24 component icons for Particle Engine (clean-room: our own monogram tiles,
# nothing copied from Culebra). Each component gets a rounded tile in its subcategory colour with its
# nickname in white. Re-run after adding a component. Output: src/.../Resources/icons/<Key>.png
#
#   powershell -NoProfile -File scripts/generate-icons.ps1
#
# Windows-only (uses System.Drawing). The PNGs are committed, so this only needs re-running when the
# component set or branding changes.

Add-Type -AssemblyName System.Drawing

$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $PSScriptRoot
$outDir = Join-Path $root 'src/ParticleEngine.Grasshopper/Resources/icons'
New-Item -ItemType Directory -Force -Path $outDir | Out-Null

# Subcategory colours (R,G,B).
$spawn = [System.Drawing.Color]::FromArgb(46, 139, 87)    # green
$init  = [System.Drawing.Color]::FromArgb(47, 111, 176)   # blue
$behav = [System.Drawing.Color]::FromArgb(224, 123, 42)   # orange
$force = [System.Drawing.Color]::FromArgb(192, 57, 43)    # red
$engine= [System.Drawing.Color]::FromArgb(125, 60, 152)   # purple
$disp  = [System.Drawing.Color]::FromArgb(21, 152, 149)   # teal

# key, nickname, colour
$components = @(
    @('BoxSpawn','BS',$spawn),
    @('PointSpawn','PS',$spawn),
    @('InitSettings','IS',$init),
    @('MoveSettings','MS',$init),
    @('Controller','BC',$behav),
    @('Flocking','FL',$behav),
    @('Stigmergy','ST',$behav),
    @('MultiPathTracking','T',$behav),
    @('MeshCrawl','MC',$behav),
    @('AttractionForce','AF',$force),
    @('RepulsionForce','RF',$force),
    @('Bundling','B',$force),
    @('CreeperEngine','CE',$engine),
    @('CreeperZombieEngine','CZE',$engine),
    @('BundlingEngine','BE',$engine),
    @('BundlingZombieEngine','BZE',$engine),
    @('TrailData','TD',$disp),
    @('VisualSettings','VS',$disp)
)

foreach ($c in $components) {
    $key = $c[0]; $label = $c[1]; $colour = $c[2]
    $bmp = New-Object System.Drawing.Bitmap 24, 24
    $g = [System.Drawing.Graphics]::FromImage($bmp)
    $g.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::AntiAlias
    $g.TextRenderingHint = [System.Drawing.Text.TextRenderingHint]::AntiAlias
    $g.Clear([System.Drawing.Color]::Transparent)

    # Rounded tile, inset 1px.
    $rect = New-Object System.Drawing.Rectangle 1, 1, 21, 21
    $r = 5
    $path = New-Object System.Drawing.Drawing2D.GraphicsPath
    $path.AddArc($rect.X, $rect.Y, $r, $r, 180, 90)
    $path.AddArc($rect.Right - $r, $rect.Y, $r, $r, 270, 90)
    $path.AddArc($rect.Right - $r, $rect.Bottom - $r, $r, $r, 0, 90)
    $path.AddArc($rect.X, $rect.Bottom - $r, $r, $r, 90, 90)
    $path.CloseFigure()
    $brush = New-Object System.Drawing.SolidBrush $colour
    $g.FillPath($brush, $path)

    # Nickname, white, size by length.
    $size = if ($label.Length -le 2) { 9 } elseif ($label.Length -eq 3) { 7 } else { 6 }
    $font = New-Object System.Drawing.Font 'Segoe UI', $size, ([System.Drawing.FontStyle]::Bold), ([System.Drawing.GraphicsUnit]::Pixel)
    $fmt = New-Object System.Drawing.StringFormat
    $fmt.Alignment = [System.Drawing.StringAlignment]::Center
    $fmt.LineAlignment = [System.Drawing.StringAlignment]::Center
    $white = New-Object System.Drawing.SolidBrush ([System.Drawing.Color]::White)
    $g.DrawString($label, $font, $white, (New-Object System.Drawing.RectangleF 1, 1, 22, 22), $fmt)

    $g.Dispose()
    $outPath = Join-Path $outDir ($key + '.png')
    $bmp.Save($outPath, [System.Drawing.Imaging.ImageFormat]::Png)
    $bmp.Dispose()
    Write-Host "wrote $key.png"
}

Write-Host "Done: $($components.Count) icons in $outDir"
