# 生成 Skins/Cyberpunk 四帧 PNG（透明底）。在项目根目录执行: powershell -File scripts/gen-cyberpunk-skin.ps1
Add-Type -AssemblyName System.Drawing
$root = Split-Path -Parent (Split-Path -Parent $MyInvocation.MyCommand.Path)
$dir = Join-Path $root "Skins\Cyberpunk"
New-Item -ItemType Directory -Force -Path $dir | Out-Null

$w = 128
$h = 160

function New-CyberFrame($index) {
    $bmp = New-Object System.Drawing.Bitmap $w, $h, ([System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
    $g = [System.Drawing.Graphics]::FromImage($bmp)
    $g.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::AntiAlias
    $g.Clear([System.Drawing.Color]::Transparent)

    # 机体暗紫半透明核
    $bodyFill = New-Object System.Drawing.SolidBrush ([System.Drawing.Color]::FromArgb(220, 26, 12, 46))
    $g.FillEllipse($bodyFill, 22, 38, 84, 94)

    # 外层霓虹青描边
    $neonCyan = [System.Drawing.Color]::FromArgb(255, 0, 245, 255)
    $neonMagenta = [System.Drawing.Color]::FromArgb(255, 255, 0, 170)
    $penOuter = New-Object System.Drawing.Pen $neonCyan, 3
    $g.DrawEllipse($penOuter, 22, 38, 84, 94)

    # 内圈微光
    $penInner = New-Object System.Drawing.Pen ([System.Drawing.Color]::FromArgb(180, 0, 200, 220)), 1
    $g.DrawEllipse($penInner, 28, 44, 72, 82)

    # 电路短线
    $circuit = New-Object System.Drawing.Pen ([System.Drawing.Color]::FromArgb(200, 255, 0, 200)), 1
    $g.DrawLine($circuit, 30, 95, 45, 88)
    $g.DrawLine($circuit, 83, 88, 98, 95)
    $g.DrawLine($circuit, 52, 118, 76, 118)

    # 六角点缀（简化成小三角）
    $tri = New-Object System.Drawing.Drawing2D.GraphicsPath
    $tri.AddPolygon(@(
        [System.Drawing.Point]::new(96, 48),
        [System.Drawing.Point]::new(104, 58),
        [System.Drawing.Point]::new(88, 58)
    ))
    $g.FillPath((New-Object System.Drawing.SolidBrush ([System.Drawing.Color]::FromArgb(160, 0, 255, 200))), $tri)

    # 眼部 / 面罩
    $ey = 54 + ($index % 2)
    if ($index -eq 3) {
        # 闭眼：两道霓虹缝
        $lp = New-Object System.Drawing.Pen $neonCyan, 2
        $g.DrawLine($lp, 38, $ey + 8, 52, $ey + 4)
        $g.DrawLine($lp, 76, $ey + 4, 90, $ey + 8)
        $lp.Dispose()
    }
    else {
        # 发光眼
        $core = New-Object System.Drawing.SolidBrush ([System.Drawing.Color]::FromArgb(255, 200, 255, 255))
        $g.FillEllipse($core, 40, $ey, 14, 16)
        $g.FillEllipse($core, 74, $ey, 14, 16)
        $glow = New-Object System.Drawing.Pen ([System.Drawing.Color]::FromArgb(120 + $index * 30, 0, 255, 255)), 2
        $g.DrawEllipse($glow, 38, $ey - 1, 18, 20)
        $g.DrawEllipse($glow, 72, $ey - 1, 18, 20)
        $glow.Dispose()
        $core.Dispose()
        if ($index -eq 2) {
            # 第二帧：加一点品红高光
            $g.FillEllipse((New-Object System.Drawing.SolidBrush ([System.Drawing.Color]::FromArgb(200, 255, 0, 200))), 46, $ey + 4, 5, 5)
            $g.FillEllipse((New-Object System.Drawing.SolidBrush ([System.Drawing.Color]::FromArgb(200, 255, 0, 200))), 80, $ey + 4, 5, 5)
        }
        if ($index -eq 4) {
            # 第四帧：眼更「亮」外圈品红
            $pm = New-Object System.Drawing.Pen ([System.Drawing.Color]::FromArgb(200, 255, 0, 170)), 1
            $g.DrawEllipse($pm, 36, $ey - 3, 22, 24)
            $g.DrawEllipse($pm, 70, $ey - 3, 22, 24)
            $pm.Dispose()
        }
    }

    # 嘴部：霓虹微笑线段
    $mouthPen = New-Object System.Drawing.Pen $neonMagenta, 2
    $mouthPen.EndCap = [System.Drawing.Drawing2D.LineCap]::Round
    $g.DrawArc($mouthPen, 48, 86 + ($index % 2), 32, 20, 20, 140)

    # 扫描线感（稀疏横线，半透明）
    $scan = New-Object System.Drawing.Pen ([System.Drawing.Color]::FromArgb(40, 0, 255, 255)), 1
    for ($y = 36; $y -lt 132; $y += 8) {
        $g.DrawLine($scan, 24, $y, 104, $y)
    }

    $scan.Dispose()
    $mouthPen.Dispose()
    $circuit.Dispose()
    $penInner.Dispose()
    $penOuter.Dispose()
    $bodyFill.Dispose()
    $g.Dispose()

    $path = Join-Path $dir ("cyber_{0:D2}.png" -f $index)
    $bmp.Save($path, [System.Drawing.Imaging.ImageFormat]::Png)
    $bmp.Dispose()
}

for ($i = 1; $i -le 4; $i++) { New-CyberFrame $i }
Write-Host "Cyberpunk skin -> $dir"
