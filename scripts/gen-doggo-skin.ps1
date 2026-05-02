# 生成 Skins/Doggo 四帧：金毛系卡通小狗。项目根: powershell -File scripts/gen-doggo-skin.ps1
Add-Type -AssemblyName System.Drawing
$root = Split-Path -Parent (Split-Path -Parent $MyInvocation.MyCommand.Path)
$dir = Join-Path $root "Skins\Doggo"
New-Item -ItemType Directory -Force -Path $dir | Out-Null

$w = 128
$h = 160

function New-DogFrame([int]$index) {
    $bmp = New-Object System.Drawing.Bitmap $w, $h, ([System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
    $g = [System.Drawing.Graphics]::FromImage($bmp)
    $g.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::AntiAlias
    $g.Clear([System.Drawing.Color]::Transparent)

    # 阴影
    $shadowPath = New-Object System.Drawing.Drawing2D.GraphicsPath
    $shadowPath.AddEllipse(28, 118, 72, 20)
    $sb = New-Object System.Drawing.Drawing2D.PathGradientBrush($shadowPath)
    $sb.CenterColor = [System.Drawing.Color]::FromArgb(70, 60, 50, 40)
    $sb.SurroundColors = @([System.Drawing.Color]::FromArgb(0, 255, 255, 255))
    $g.FillPath($sb, $shadowPath)
    $sb.Dispose(); $shadowPath.Dispose()

    # 耳朵（深一点的粽）
    $earBrush = New-Object System.Drawing.SolidBrush ([System.Drawing.Color]::FromArgb(255, 196, 142, 88))
    $earPen = New-Object System.Drawing.Pen ([System.Drawing.Color]::FromArgb(220, 160, 110, 60)), 1.5
    $g.FillEllipse($earBrush, 18, 44, 34, 46)
    $g.FillEllipse($earBrush, 76, 44, 34, 46)
    $g.DrawEllipse($earPen, 18, 44, 34, 46)
    $g.DrawEllipse($earPen, 76, 44, 34, 46)
    $earPen.Dispose(); $earBrush.Dispose()

    # 头部主球（暖金渐变）
    $headPath = New-Object System.Drawing.Drawing2D.GraphicsPath
    $headPath.AddEllipse(24, 38, 80, 86)
    $hb = New-Object System.Drawing.Drawing2D.PathGradientBrush($headPath)
    $hb.CenterPoint = New-Object System.Drawing.PointF 64, 72
    $hb.CenterColor = [System.Drawing.Color]::FromArgb(255, 255, 224, 170)
    $hb.SurroundColors = @([System.Drawing.Color]::FromArgb(255, 230, 176, 108))
    $g.FillPath($hb, $headPath)
    $outline = New-Object System.Drawing.Pen ([System.Drawing.Color]::FromArgb(200, 200, 140, 70)), 2
    $g.DrawEllipse($outline, 24, 38, 80, 86)
    $outline.Dispose(); $hb.Dispose(); $headPath.Dispose()

    # 脸部浅色区（口吻）
    $muzzle = New-Object System.Drawing.SolidBrush ([System.Drawing.Color]::FromArgb(255, 252, 236, 210))
    $g.FillEllipse($muzzle, 38, 72, 52, 42)

    # 鼻头
    $nose = New-Object System.Drawing.SolidBrush ([System.Drawing.Color]::FromArgb(255, 45, 38, 36))
    $g.FillEllipse($nose, 56, 94 + ($index % 2), 16, 12)

    # 眼睛
    $ey = 58 + [Math]::Floor($index / 2)
    if ($index -eq 3) {
        # 眯眼笑
        $lp = New-Object System.Drawing.Pen ([System.Drawing.Color]::FromArgb(255, 55, 45, 35)), 2.2
        $lp.StartCap = [System.Drawing.Drawing2D.LineCap]::Round
        $lp.EndCap = [System.Drawing.Drawing2D.LineCap]::Round
        $g.DrawArc($lp, 38, $ey + 4, 20, 14, 200, 140)
        $g.DrawArc($lp, 70, $ey + 4, 20, 14, 200, 140)
        $lp.Dispose()
    }
    else {
        $eyeW = New-Object System.Drawing.SolidBrush ([System.Drawing.Color]::White)
        $g.FillEllipse($eyeW, 40, $ey, 16, 20)
        $g.FillEllipse($eyeW, 72, $ey, 16, 20)
        $iris = New-Object System.Drawing.SolidBrush ([System.Drawing.Color]::FromArgb(255, 70, 120, 90))
        $g.FillEllipse($iris, 43, $ey + 5, 10, 12)
        $g.FillEllipse($iris, 75, $ey + 5, 10, 12)
        $pup = New-Object System.Drawing.SolidBrush ([System.Drawing.Color]::FromArgb(255, 30, 35, 40))
        $g.FillEllipse($pup, 46, $ey + 8, 6, 7)
        $g.FillEllipse($pup, 78, $ey + 8, 6, 7)
        $hi = New-Object System.Drawing.SolidBrush ([System.Drawing.Color]::FromArgb(230, 255, 255, 255))
        $g.FillEllipse($hi, 44, $ey + 6, 4, 4)
        $g.FillEllipse($hi, 76, $ey + 6, 4, 4)
        $hi.Dispose(); $pup.Dispose(); $iris.Dispose(); $eyeW.Dispose()
    }

    # 嘴线 + 舌头（1、4 帧吐舌）
    $mouthPen = New-Object System.Drawing.Pen ([System.Drawing.Color]::FromArgb(200, 180, 120, 80)), 1.8
    $mouthPen.StartCap = [System.Drawing.Drawing2D.LineCap]::Round
    $g.DrawArc($mouthPen, 48, 102, 32, 22, 40, 80)
    if ($index -eq 1 -or $index -eq 4) {
        $tongue = New-Object System.Drawing.SolidBrush ([System.Drawing.Color]::FromArgb(255, 255, 150, 160))
        $g.FillEllipse($tongue, 54, 108 + ($index % 2), 20, 14)
    }

    # 眉毛俏皮弧（第2帧）
    if ($index -eq 2) {
        $bp = New-Object System.Drawing.Pen ([System.Drawing.Color]::FromArgb(180, 200, 140, 70)), 1.5
        $g.DrawArc($bp, 38, 50, 18, 12, 180, 80)
        $g.DrawArc($bp, 72, 50, 18, 12, 260, 80)
        $bp.Dispose()
    }

    $mouthPen.Dispose()
    $nose.Dispose()
    $muzzle.Dispose()
    $g.Dispose()

    $path = Join-Path $dir ("woof_{0:D2}.png" -f $index)
    $bmp.Save($path, [System.Drawing.Imaging.ImageFormat]::Png)
    $bmp.Dispose()
}

for ($i = 1; $i -le 4; $i++) { New-DogFrame $i }
Write-Host "Doggo skin -> $dir"
