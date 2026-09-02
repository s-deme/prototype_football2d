param(
    [string]$Workspace = (Resolve-Path (Join-Path $PSScriptRoot '..')).Path
)

$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.Drawing

$masterRoot = Join-Path $Workspace 'Steam\StoreAssets\Masters'
$outputRoot = Join-Path $Workspace 'Steam\StoreAssets\Capsules'
$libraryRoot = Join-Path $Workspace 'Steam\StoreAssets\Library'
$communityRoot = Join-Path $Workspace 'Steam\StoreAssets\Community'
$screenshotsRoot = Join-Path $Workspace 'Steam\StoreAssets\Screenshots'
$landscape = Join-Path $masterRoot 'key-art-landscape.png'
$vertical = Join-Path $masterRoot 'key-art-vertical.png'
$hero = Join-Path $masterRoot 'key-art-library-hero.png'
$icon = Join-Path $Workspace 'Assets\Brand\SparkStrikersIcon.png'
foreach ($path in @($landscape, $vertical, $hero, $icon)) {
    if (-not (Test-Path -LiteralPath $path)) { throw "Missing key art: $path" }
}
foreach ($path in @($outputRoot, $libraryRoot, $communityRoot)) {
    New-Item -ItemType Directory -Path $path -Force | Out-Null
}

function Draw-Title {
    param(
        [System.Drawing.Graphics]$Graphics,
        [int]$CanvasWidth,
        [int]$X,
        [int]$Y,
        [int]$SparkSize,
        [int]$StrikersSize,
        [bool]$Centered
    )

    $sparkFont = [System.Drawing.Font]::new('Arial', $SparkSize, [System.Drawing.FontStyle]::Bold, [System.Drawing.GraphicsUnit]::Pixel)
    $strikersFont = [System.Drawing.Font]::new('Arial', $StrikersSize, [System.Drawing.FontStyle]::Bold, [System.Drawing.GraphicsUnit]::Pixel)
    $format = [System.Drawing.StringFormat]::GenericTypographic
    $sparkWidth = [int][Math]::Ceiling($Graphics.MeasureString('SPARK', $sparkFont, 2000, $format).Width)
    $strikersWidth = [int][Math]::Ceiling($Graphics.MeasureString('STRIKERS', $strikersFont, 2000, $format).Width)
    if ($Centered) {
        $sparkX = [int](($CanvasWidth - $sparkWidth) / 2)
        $strikersX = [int](($CanvasWidth - $strikersWidth) / 2)
    } else {
        $sparkX = $X
        $strikersX = $X
    }

    $shadow = [System.Drawing.SolidBrush]::new([System.Drawing.Color]::FromArgb(230, 0, 3, 14))
    $cyan = [System.Drawing.SolidBrush]::new([System.Drawing.Color]::FromArgb(255, 45, 220, 255))
    $white = [System.Drawing.SolidBrush]::new([System.Drawing.Color]::White)
    $orange = [System.Drawing.SolidBrush]::new([System.Drawing.Color]::FromArgb(255, 255, 84, 38))
    try {
        $gap = [Math]::Max(2, [int]($SparkSize * 0.05))
        $secondY = $Y + $SparkSize - $gap
        $shadowOffset = [Math]::Max(2, [int]($StrikersSize * 0.055))
        $Graphics.DrawString('SPARK', $sparkFont, $shadow, $sparkX + $shadowOffset, $Y + $shadowOffset, $format)
        $Graphics.DrawString('STRIKERS', $strikersFont, $shadow, $strikersX + $shadowOffset, $secondY + $shadowOffset, $format)
        $Graphics.DrawString('SPARK', $sparkFont, $cyan, $sparkX, $Y, $format)
        $Graphics.DrawString('STRIKERS', $strikersFont, $white, $strikersX, $secondY, $format)
        $barWidth = [Math]::Max(30, [int]($strikersWidth * 0.42))
        $barX = if ($Centered) { [int](($CanvasWidth - $barWidth) / 2) } else { $X }
        $Graphics.FillRectangle($orange, $barX, $secondY + $StrikersSize + $gap, $barWidth, [Math]::Max(3, [int]($StrikersSize * 0.07)))
    } finally {
        $sparkFont.Dispose()
        $strikersFont.Dispose()
        $shadow.Dispose()
        $cyan.Dispose()
        $white.Dispose()
        $orange.Dispose()
    }
}

function New-Capsule {
    param(
        [string]$Name,
        [string]$SourcePath,
        [int]$Width,
        [int]$Height,
        [int]$TitleX,
        [int]$TitleY,
        [int]$SparkSize,
        [int]$StrikersSize,
        [bool]$Centered = $false,
        [bool]$CropFromTop = $false,
        [string]$OutputDirectory = $outputRoot
    )

    $source = [System.Drawing.Bitmap]::FromFile($SourcePath)
    $canvas = [System.Drawing.Bitmap]::new($Width, $Height, [System.Drawing.Imaging.PixelFormat]::Format24bppRgb)
    $graphics = [System.Drawing.Graphics]::FromImage($canvas)
    try {
        $graphics.CompositingQuality = [System.Drawing.Drawing2D.CompositingQuality]::HighQuality
        $graphics.InterpolationMode = [System.Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic
        $graphics.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::AntiAlias
        $graphics.TextRenderingHint = [System.Drawing.Text.TextRenderingHint]::AntiAliasGridFit

        $sourceAspect = $source.Width / [double]$source.Height
        $targetAspect = $Width / [double]$Height
        if ($sourceAspect -gt $targetAspect) {
            $cropHeight = $source.Height
            $cropWidth = [int][Math]::Round($cropHeight * $targetAspect)
            $cropX = [int](($source.Width - $cropWidth) / 2)
            $cropY = 0
        } else {
            $cropWidth = $source.Width
            $cropHeight = [int][Math]::Round($cropWidth / $targetAspect)
            $cropX = 0
            $cropY = if ($CropFromTop) { 0 } else { [int](($source.Height - $cropHeight) / 2) }
        }
        $destination = [System.Drawing.Rectangle]::new(0, 0, $Width, $Height)
        $graphics.DrawImage($source, $destination, $cropX, $cropY, $cropWidth, $cropHeight, [System.Drawing.GraphicsUnit]::Pixel)

        $shadeWidth = if ($Centered) { $Width } else { [int]($Width * 0.58) }
        $shade = [System.Drawing.SolidBrush]::new([System.Drawing.Color]::FromArgb($(if ($Centered) { 55 } else { 72 }), 0, 3, 18))
        try { $graphics.FillRectangle($shade, 0, 0, $shadeWidth, $Height) } finally { $shade.Dispose() }
        Draw-Title -Graphics $graphics -CanvasWidth $Width -X $TitleX -Y $TitleY -SparkSize $SparkSize -StrikersSize $StrikersSize -Centered $Centered

        $output = Join-Path $OutputDirectory $Name
        $canvas.Save($output, [System.Drawing.Imaging.ImageFormat]::Png)
    } finally {
        $graphics.Dispose()
        $canvas.Dispose()
        $source.Dispose()
    }
}

function New-ResizedImage {
    param(
        [string]$SourcePath,
        [string]$OutputPath,
        [int]$Width,
        [int]$Height,
        [int]$Padding = 0,
        [bool]$Transparent = $false
    )

    $source = [System.Drawing.Bitmap]::FromFile($SourcePath)
    $format = if ($Transparent) { [System.Drawing.Imaging.PixelFormat]::Format32bppArgb } else { [System.Drawing.Imaging.PixelFormat]::Format24bppRgb }
    $canvas = [System.Drawing.Bitmap]::new($Width, $Height, $format)
    $graphics = [System.Drawing.Graphics]::FromImage($canvas)
    try {
        $graphics.CompositingQuality = [System.Drawing.Drawing2D.CompositingQuality]::HighQuality
        $graphics.InterpolationMode = [System.Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic
        $graphics.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::AntiAlias
        $graphics.Clear($(if ($Transparent) { [System.Drawing.Color]::Transparent } else { [System.Drawing.Color]::FromArgb(255, 2, 8, 24) }))
        $graphics.DrawImage($source, $Padding, $Padding, $Width - $Padding * 2, $Height - $Padding * 2)
        if ([System.IO.Path]::GetExtension($OutputPath) -eq '.jpg') {
            $canvas.Save($OutputPath, [System.Drawing.Imaging.ImageFormat]::Jpeg)
        } else {
            $canvas.Save($OutputPath, [System.Drawing.Imaging.ImageFormat]::Png)
        }
    } finally {
        $graphics.Dispose()
        $canvas.Dispose()
        $source.Dispose()
    }
}

function New-LibraryLogo {
    $output = Join-Path $libraryRoot 'library_logo.png'
    $canvas = [System.Drawing.Bitmap]::new(1280, 360, [System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
    $graphics = [System.Drawing.Graphics]::FromImage($canvas)
    try {
        $graphics.Clear([System.Drawing.Color]::Transparent)
        $graphics.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::AntiAlias
        $graphics.TextRenderingHint = [System.Drawing.Text.TextRenderingHint]::AntiAliasGridFit
        Draw-Title -Graphics $graphics -CanvasWidth 1280 -X 0 -Y 24 -SparkSize 86 -StrikersSize 122 -Centered $true
        $canvas.Save($output, [System.Drawing.Imaging.ImageFormat]::Png)
    } finally {
        $graphics.Dispose()
        $canvas.Dispose()
    }
}

New-Capsule -Name 'header_capsule.png' -SourcePath $landscape -Width 920 -Height 430 -TitleX 42 -TitleY 112 -SparkSize 54 -StrikersSize 72
New-Capsule -Name 'small_capsule.png' -SourcePath $landscape -Width 462 -Height 174 -TitleX 20 -TitleY 38 -SparkSize 27 -StrikersSize 36
New-Capsule -Name 'main_capsule.png' -SourcePath $landscape -Width 1232 -Height 706 -TitleX 62 -TitleY 205 -SparkSize 76 -StrikersSize 102
New-Capsule -Name 'vertical_capsule.png' -SourcePath $vertical -Width 748 -Height 896 -TitleX 0 -TitleY 76 -SparkSize 74 -StrikersSize 98 -Centered $true -CropFromTop $true
New-Capsule -Name 'library_capsule.png' -SourcePath $vertical -Width 600 -Height 900 -TitleX 0 -TitleY 58 -SparkSize 60 -StrikersSize 80 -Centered $true -CropFromTop $true -OutputDirectory $libraryRoot
Copy-Item -LiteralPath (Join-Path $outputRoot 'header_capsule.png') -Destination (Join-Path $libraryRoot 'library_header.png') -Force
New-ResizedImage -SourcePath $hero -OutputPath (Join-Path $libraryRoot 'library_hero.png') -Width 3840 -Height 1240
New-LibraryLogo
New-ResizedImage -SourcePath $icon -OutputPath (Join-Path $communityRoot 'shortcut_icon.png') -Width 256 -Height 256 -Transparent $true
New-ResizedImage -SourcePath $icon -OutputPath (Join-Path $communityRoot 'app_icon.jpg') -Width 184 -Height 184 -Padding 8

$expected = @{
    'header_capsule.png' = @(920, 430)
    'small_capsule.png' = @(462, 174)
    'main_capsule.png' = @(1232, 706)
    'vertical_capsule.png' = @(748, 896)
}
foreach ($name in $expected.Keys) {
    $path = Join-Path $outputRoot $name
    $image = [System.Drawing.Image]::FromFile($path)
    try {
        if ($image.Width -ne $expected[$name][0] -or $image.Height -ne $expected[$name][1]) {
            throw "Unexpected capsule dimensions for ${name}: $($image.Width)x$($image.Height)"
        }
    } finally {
        $image.Dispose()
    }
}

$additionalExpected = @(
    @{ Path = (Join-Path $libraryRoot 'library_capsule.png'); Width = 600; Height = 900 },
    @{ Path = (Join-Path $libraryRoot 'library_header.png'); Width = 920; Height = 430 },
    @{ Path = (Join-Path $libraryRoot 'library_hero.png'); Width = 3840; Height = 1240 },
    @{ Path = (Join-Path $libraryRoot 'library_logo.png'); Width = 1280; Height = 360 },
    @{ Path = (Join-Path $communityRoot 'shortcut_icon.png'); Width = 256; Height = 256 },
    @{ Path = (Join-Path $communityRoot 'app_icon.jpg'); Width = 184; Height = 184 }
)
$expectedScreenshotNames = @('01-comet-breaker.png', '02-blaze-cannon.png', '03-zero-drive.png', '04-eclipse-arc.png', '05-goal-celebration.png')
$actualScreenshotNames = @(Get-ChildItem -LiteralPath $screenshotsRoot -File -Filter '*.png' | Select-Object -ExpandProperty Name | Sort-Object)
if (@(Compare-Object ($expectedScreenshotNames | Sort-Object) $actualScreenshotNames).Count -ne 0) {
    throw "Unexpected Steam screenshots: $($actualScreenshotNames -join ', ')"
}
foreach ($name in $expectedScreenshotNames) {
    $additionalExpected += @{ Path = (Join-Path $screenshotsRoot $name); Width = 1920; Height = 1080 }
}
foreach ($item in $additionalExpected) {
    $image = [System.Drawing.Image]::FromFile($item.Path)
    try {
        if ($image.Width -ne $item.Width -or $image.Height -ne $item.Height) {
            throw "Unexpected asset dimensions for $($item.Path): $($image.Width)x$($image.Height)"
        }
    } finally {
        $image.Dispose()
    }
}

$logoImage = [System.Drawing.Bitmap]::FromFile((Join-Path $libraryRoot 'library_logo.png'))
try {
    $cornerAlpha = @($logoImage.GetPixel(0, 0).A, $logoImage.GetPixel($logoImage.Width - 1, 0).A, $logoImage.GetPixel(0, $logoImage.Height - 1).A, $logoImage.GetPixel($logoImage.Width - 1, $logoImage.Height - 1).A)
    if (@($cornerAlpha | Where-Object { $_ -ne 0 }).Count -ne 0) { throw 'Library logo background is not transparent.' }
} finally {
    $logoImage.Dispose()
}

Write-Output "Steam graphical assets generated under: $(Join-Path $Workspace 'Steam\StoreAssets')"
