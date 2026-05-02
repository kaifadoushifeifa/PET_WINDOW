# 生成 Skins/Mochi 四帧（ pastel 渐变 + 腮红 + 眼光）。项目根: powershell -File scripts/gen-mochi-skin.ps1
Add-Type -AssemblyName System.Drawing
$root = Split-Path -Parent (Split-Path -Parent $MyInvocation.MyCommand.Path)
$dir = Join-Path $root "Skins\Mochi"
New-Item -ItemType Directory -Force -Path $dir | Out-Null

$w = 128
$h = 160

function New-MochiFrame([int]$index) {
    $bmp = New-Object System.Drawing.Bitmap $w, $h, ([System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
    $g = [System.Drawing.Graphics]::FromImage($bmp)
    $g.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::AntiAlias
    $g.Clear([System.Drawing.Color]::Transparent)

    # 柔和落地阴影
    $shadowPath = New-Object System.Drawing.Drawing2D.GraphicsPath
    $shadowPath.AddEllipse(26, 118, 76, 22)
    $shadowBrush = New-Object System.Drawing.Drawing2D.PathGradientBrush($shadowPath)
    $shadowBrush.CenterColor = [System.Drawing.Color]::FromArgb(90, 80, 60, 90)
    $shadowBrush.SurroundColors = @([System.Drawing.Color]::FromArgb(0, 255, 255, 255))
    $g.FillPath($shadowBrush, $shadowPath)
    $shadowBrush.Dispose()
    $shadowPath.Dispose()

    # 身体渐变（蜜桃糯米）
    $bodyRect = New-Object System.Drawing.Rectangle 22, 38, 84, 92
    $bodyPath = New-Object System.Drawing.Drawing2D.GraphicsPath
    $bodyPath.AddEllipse($bodyRect)
    $bodyBrush = New-Object System.Drawing.Drawing2D.PathGradientBrush($bodyPath)
    $bodyBrush.CenterPoint = New-Object System.Drawing.PointF 64, 78
    $bodyBrush.CenterColor = [System.Drawing.Color]::FromArgb(255, 255, 218, 205)
    $bodyBrush.SurroundColors = @([System.Drawing.Color]::FromArgb(255, 255, 155, 155))
    $g.FillPath($bodyBrush, $bodyPath)

    # 边缘柔和勾边
    $edgePen = New-Object System.Drawing.Pen ([System.Drawing.Color]::FromArgb(200, 220, 120, 130)), 2
    $g.DrawEllipse($edgePen, 22, 38, 84, 92)

    # 顶部高光（奶油条）
    $hiPath = New-Object System.Drawing.Drawing2D.GraphicsPath
    $hiPath.AddEllipse(38, 44, 52, 28)
    $hiBrush = New-Object System.Drawing.Drawing2D.PathGradientBrush($hiPath)
    $hiBrush.CenterColor = [System.Drawing.Color]::FromArgb(140, 255, 255, 255)
    $hiBrush.SurroundColors = @([System.Drawing.Color]::FromArgb(0, 255, 255, 255))
    $g.FillPath($hiBrush, $hiPath)
    $hiBrush.Dispose()
    $hiPath.Dispose()

    $edgePen.Dispose()
    $bodyBrush.Dispose()
    $bodyPath.Dispose()

    # 小耳朵（同色略深）
    $ear = New-Object System.Drawing.SolidBrush ([System.Drawing.Color]::FromArgb(255, 255, 175, 175))
    $earOutline = New-Object System.Drawing.Pen ([System.Drawing.Color]::FromArgb(180, 230, 130, 145)), 1.5
    $g.FillEllipse($ear, 34, 22, 22, 28)
    $g.FillEllipse($ear, 72, 22, 22, 28)
    $g.DrawEllipse($earOutline, 34, 22, 22, 28)
    $g.DrawEllipse($earOutline, 72, 22, 22, 28)
    $earOutline.Dispose()
    $ear.Dispose()

    # 腮红
    $blush = New-Object System.Drawing.SolidBrush ([System.Drawing.Color]::FromArgb(110, 255, 140, 160))
    $by = 82 + ($index % 2)
    $g.FillEllipse($blush, 28, $by, 18, 12)
    $g.FillEllipse($blush, 82, $by, 18, 12)
    $blush.Dispose()

    $eyBase = 56 + [Math]::Floor($index / 2)

    if ($index -eq 3) {
        # 闭眼：弯弯笑眼
        $lp = New-Object System.Drawing.Pen ([System.Drawing.Color]::FromArgb(255, 55, 45, 50)), 2.5
        $lp.StartCap = [System.Drawing.Drawing2D.LineCap]::Round
        $lp.EndCap = [System.Drawing.Drawing2D.LineCap]::Round
        $g.DrawArc($lp, 36, $eyBase + 2, 22, 18, 200, 140)
        $g.DrawArc($lp, 70, $eyBase + 2, 22, 18, 200, 140)
        $lp.Dispose()
    }
    else {
        # 大眼睛 + 渐变瞳孔感
        $eyeWhite = New-Object System.Drawing.SolidBrush ([System.Drawing.Color]::White)
        $g.FillEllipse($eyeWhite, 36, $eyBase, 22, 26)
        $g.FillEllipse($eyeWhite, 70, $eyBase, 22, 26)

        $iris = New-Object System.Drawing.Drawing2D.GraphicsPath
        $iris.AddEllipse(40, $eyBase + 6, 14, 18)
        $irisBrush = New-Object System.Drawing.Drawing2D.PathGradientBrush($iris)
        $irisBrush.CenterColor = [System.Drawing.Color]::FromArgb(255, 120, 210, 255)
        $irisBrush.SurroundColors = @([System.Drawing.Color]::FromArgb(255, 40, 90, 180))
        $g.FillPath($irisBrush, $iris)

        $iris2 = New-Object System.Drawing.Drawing2D.GraphicsPath
        $iris2.AddEllipse(74, $eyBase + 6, 14, 18)
        $irisBrush2 = New-Object System.Drawing.Drawing2D.PathGradientBrush($iris2)
        $irisBrush2.CenterColor = [System.Drawing.Color]::FromArgb(255, 120, 210, 255)
        $irisBrush2.SurroundColors = @([System.Drawing.Color]::FromArgb(255, 40, 90, 180))
        $g.FillPath($irisBrush2, $iris2)

        $pupil = New-Object System.Drawing.SolidBrush ([System.Drawing.Color]::FromArgb(255, 30, 35, 55))
        $g.FillEllipse($pupil, 44, $eyBase + 10, 8, 10)
        $g.FillEllipse($pupil, 78, $eyBase + 10, 8, 10)

        # 高光：位置随帧微移
        $sx = if ($index -eq 1) { 0 } elseif ($index -eq 2) { 2 } else { -1 }
        $shine = New-Object System.Drawing.SolidBrush ([System.Drawing.Color]::FromArgb(240, 255, 255, 255))
        $g.FillEllipse($shine, 42 + $sx, $eyBase + 7, 6, 6)
        $g.FillEllipse($shine, 76 + $sx, $eyBase + 7, 6, 6)
        $tiny = New-Object System.Drawing.SolidBrush ([System.Drawing.Color]::FromArgb(200, 255, 255, 255))
        $g.FillEllipse($tiny, 48 + $sx, $eyBase + 14, 3, 3)
        $g.FillEllipse($tiny, 82 + $sx, $eyBase + 14, 3, 3)

        $shine.Dispose()
        $tiny.Dispose()
        $pupil.Dispose()
        $irisBrush2.Dispose()
        $iris2.Dispose()
        $irisBrush.Dispose()
        $iris.Dispose()
        $eyeWhite.Dispose()
    }

    # 小嘴（略上扬）
    $mouthPen = New-Object System.Drawing.Pen ([System.Drawing.Color]::FromArgb(230, 200, 90, 110)), 2
    $mouthPen.StartCap = [System.Drawing.Drawing2D.LineCap]::Round
    $mouthPen.EndCap = [System.Drawing.Drawing2D.LineCap]::Round
    $off = if ($index -eq 2) { 2 } else { 0 }
    $g.DrawArc($mouthPen, 50, 98 + $off, 28, 22, 30, 120)

    # 星星点缀（第 4 帧稍明显）
    if ($index -eq 4) {
        $starBrush = New-Object System.Drawing.SolidBrush ([System.Drawing.Color]::FromArgb(230, 255, 220, 120))
        $pts1 = @(
            [System.Drawing.PointF]::new(18, 52), [System.Drawing.PointF]::new(20, 58), [System.Drawing.PointF]::new(26, 58),
            [System.Drawing.PointF]::new(21, 62), [System.Drawing.PointF]::new(23, 68), [System.Drawing.PointF]::new(18, 64),
            [System.Drawing.PointF]::new(13, 68), [System.Drawing.PointF]::new(15, 62), [System.Drawing.PointF]::new(10, 58),
            [System.Drawing.PointF]::new(16, 58)
        )
        $g.FillPolygon($starBrush, $pts1)
        $pts2 = @(
            [System.Drawing.PointF]::new(102, 46), [System.Drawing.PointF]::new(104, 52), [System.Drawing.PointF]::new(110, 52),
            [System.Drawing.PointF]::new(105, 56), [System.Drawing.PointF]::new(107, 62), [System.Drawing.PointF]::new(102, 58),
            [System.Drawing.PointF]::new(97, 62), [System.Drawing.PointF]::new(99, 56), [System.Drawing.PointF]::new(94, 52),
            [System.Drawing.PointF]::new(100, 52)
        )
        $g.FillPolygon($starBrush, $pts2)
        $starBrush.Dispose()
    }

    $mouthPen.Dispose()
    $g.Dispose()

    $pathOut = Join-Path $dir ("mochi_{0:D2}.png" -f $index)
    $bmp.Save($pathOut, [System.Drawing.Imaging.ImageFormat]::Png)
    $bmp.Dispose()
}

for ($i = 1; $i -le 4; $i++) { New-MochiFrame $i }
Write-Host "Mochi skin -> $dir"
