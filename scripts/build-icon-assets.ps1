[CmdletBinding()]
param()
$ErrorActionPreference = 'Stop'
$root = if ($PSScriptRoot) { Split-Path $PSScriptRoot -Parent } else { (Get-Location).Path }
Add-Type -AssemblyName System.Drawing
$source = [Drawing.Image]::FromFile((Join-Path $root 'artwork/photo-clock-master.png'))
$assets = Join-Path $root 'windows-native/src/PhotoRepair.App/Assets'
function Write-Icon([string]$Name, [int]$Width, [int]$Height) {
    $bitmap = [Drawing.Bitmap]::new($Width, $Height, [Drawing.Imaging.PixelFormat]::Format32bppArgb)
    $graphics = [Drawing.Graphics]::FromImage($bitmap)
    try {
        $graphics.Clear([Drawing.Color]::Transparent)
        $graphics.InterpolationMode = [Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic
        $graphics.PixelOffsetMode = [Drawing.Drawing2D.PixelOffsetMode]::HighQuality
        $scale = [Math]::Min($Width / $source.Width, $Height / $source.Height)
        $drawWidth = [int]($source.Width * $scale)
        $drawHeight = [int]($source.Height * $scale)
        $graphics.DrawImage($source, [int](($Width-$drawWidth)/2), [int](($Height-$drawHeight)/2), $drawWidth, $drawHeight)
        $bitmap.Save((Join-Path $assets $Name), [Drawing.Imaging.ImageFormat]::Png)
    } finally { $graphics.Dispose(); $bitmap.Dispose() }
}
try {
    foreach ($scale in @(100,125,150,200,400)) {
        Write-Icon "Square44x44Logo.scale-$scale.png" ([int](44*$scale/100)) ([int](44*$scale/100))
        Write-Icon "Square150x150Logo.scale-$scale.png" ([int](150*$scale/100)) ([int](150*$scale/100))
        Write-Icon "Wide310x150Logo.scale-$scale.png" ([int](310*$scale/100)) ([int](150*$scale/100))
        Write-Icon "SplashScreen.scale-$scale.png" ([int](620*$scale/100)) ([int](300*$scale/100))
    }
    foreach ($size in @(16,24,32,48,256)) {
        Write-Icon "Square44x44Logo.targetsize-$size.png" $size $size
        Write-Icon "Square44x44Logo.targetsize-${size}_altform-unplated.png" $size $size
    }
    Write-Icon 'StoreLogo.png' 50 50
    Write-Icon 'LockScreenLogo.scale-200.png' 48 48
} finally { $source.Dispose() }
