using System.Drawing;
using System.IO;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;

namespace PetWindow.Services;

/// <summary>
/// 未配置云端 API 时的本地「伪二次元」处理：对比度/饱和度、轻微色调分离、描边感。
/// </summary>
public static class OfflineAnimeStyleProcessor
{
    public static byte[] Process(ReadOnlySpan<byte> imageBytes)
    {
        using var msIn = new MemoryStream(imageBytes.ToArray());
        using var src = new Bitmap(msIn);
        using var work = ResizeLimit(src, 480);
        using var saturated = ApplyColorMatrix(work, SaturationContrastMatrix(1.45f, 1.12f));
        using var poster = Posterize(saturated, 42);
        using var edges = SobelMagnitude(saturated);
        using var blended = BlendEdgeTint(poster, edges, 0.22f);
        using var msOut = new MemoryStream();
        blended.Save(msOut, ImageFormat.Png);
        return msOut.ToArray();
    }

    private static Bitmap ResizeLimit(Bitmap src, int maxSide)
    {
        var w = src.Width;
        var h = src.Height;
        var scale = Math.Min(1.0, maxSide / (double)Math.Max(w, h));
        if (scale >= 1.0 - 1e-6)
            return new Bitmap(src);

        var nw = Math.Max(1, (int)(w * scale));
        var nh = Math.Max(1, (int)(h * scale));
        var bmp = new Bitmap(nw, nh, PixelFormat.Format32bppArgb);
        using var g = Graphics.FromImage(bmp);
        g.InterpolationMode = InterpolationMode.HighQualityBicubic;
        g.PixelOffsetMode = PixelOffsetMode.HighQuality;
        g.DrawImage(src, 0, 0, nw, nh);
        return bmp;
    }

    private static Bitmap ApplyColorMatrix(Bitmap src, ColorMatrix matrix)
    {
        var bmp = new Bitmap(src.Width, src.Height, PixelFormat.Format32bppArgb);
        using var g = Graphics.FromImage(bmp);
        using var ia = new ImageAttributes();
        ia.SetColorMatrix(matrix);
        g.DrawImage(src, new Rectangle(0, 0, src.Width, src.Height), 0, 0, src.Width, src.Height, GraphicsUnit.Pixel, ia);
        return bmp;
    }

    private static ColorMatrix SaturationContrastMatrix(float saturation, float contrast)
    {
        var t = (1f - contrast) / 2f;
        var sr = (1f - saturation) * 0.3086f;
        var sg = (1f - saturation) * 0.6094f;
        var sb = (1f - saturation) * 0.0820f;
        return new ColorMatrix(new[]
        {
            new[] { contrast * (sr + saturation), contrast * sr, contrast * sr, 0, 0 },
            new[] { contrast * sg, contrast * (sg + saturation), contrast * sg, 0, 0 },
            new[] { contrast * sb, contrast * sb, contrast * (sb + saturation), 0, 0 },
            new[] { 0f, 0, 0, 1f, 0 },
            new[] { t, t, t, 0, 1f }
        });
    }

    private static Bitmap Posterize(Bitmap src, int step)
    {
        var bmp = new Bitmap(src.Width, src.Height, PixelFormat.Format32bppArgb);
        var bdSrc = src.LockBits(new Rectangle(0, 0, src.Width, src.Height), ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);
        var bdDst = bmp.LockBits(new Rectangle(0, 0, bmp.Width, bmp.Height), ImageLockMode.WriteOnly, PixelFormat.Format32bppArgb);
        try
        {
            unsafe
            {
                var pS = (byte*)bdSrc.Scan0.ToPointer();
                var pD = (byte*)bdDst.Scan0.ToPointer();
                var strideS = bdSrc.Stride;
                var strideD = bdDst.Stride;
                for (var y = 0; y < src.Height; y++)
                {
                    var rowS = pS + y * strideS;
                    var rowD = pD + y * strideD;
                    for (var x = 0; x < src.Width; x++)
                    {
                        var i = x * 4;
                        rowD[i + 0] = (byte)(rowS[i + 0] / step * step + step / 2);
                        rowD[i + 1] = (byte)(rowS[i + 1] / step * step + step / 2);
                        rowD[i + 2] = (byte)(rowS[i + 2] / step * step + step / 2);
                        rowD[i + 3] = rowS[i + 3];
                    }
                }
            }
        }
        finally
        {
            src.UnlockBits(bdSrc);
            bmp.UnlockBits(bdDst);
        }

        return bmp;
    }

    private static Bitmap SobelMagnitude(Bitmap graySource)
    {
        var w = graySource.Width;
        var h = graySource.Height;
        var gBuf = new int[w * h];
        var bd = graySource.LockBits(new Rectangle(0, 0, w, h), ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);
        try
        {
            unsafe
            {
                var p = (byte*)bd.Scan0.ToPointer();
                var stride = bd.Stride;
                for (var y = 0; y < h; y++)
                {
                    var row = p + y * stride;
                    for (var x = 0; x < w; x++)
                    {
                        var i = x * 4;
                        var lum = (row[i + 2] * 77 + row[i + 1] * 151 + row[i + 0] * 28) >> 8;
                        gBuf[y * w + x] = lum;
                    }
                }
            }
        }
        finally
        {
            graySource.UnlockBits(bd);
        }

        var edges = new Bitmap(w, h, PixelFormat.Format32bppArgb);
        var bdE = edges.LockBits(new Rectangle(0, 0, w, h), ImageLockMode.WriteOnly, PixelFormat.Format32bppArgb);
        try
        {
            unsafe
            {
                var p = (byte*)bdE.Scan0.ToPointer();
                var stride = bdE.Stride;
                for (var y = 1; y < h - 1; y++)
                {
                    for (var x = 1; x < w - 1; x++)
                    {
                        var gx =
                            -gBuf[(y - 1) * w + x - 1] + gBuf[(y - 1) * w + x + 1]
                            - 2 * gBuf[y * w + x - 1] + 2 * gBuf[y * w + x + 1]
                            - gBuf[(y + 1) * w + x - 1] + gBuf[(y + 1) * w + x + 1];
                        var gy =
                            -gBuf[(y - 1) * w + x - 1] - 2 * gBuf[(y - 1) * w + x] - gBuf[(y - 1) * w + x + 1]
                            + gBuf[(y + 1) * w + x - 1] + 2 * gBuf[(y + 1) * w + x] + gBuf[(y + 1) * w + x + 1];
                        var mag = Math.Min(255, (int)Math.Sqrt(gx * gx + gy * gy));
                        var o = y * stride + x * 4;
                        p[o + 0] = (byte)mag;
                        p[o + 1] = (byte)mag;
                        p[o + 2] = (byte)mag;
                        p[o + 3] = 255;
                    }
                }
            }
        }
        finally
        {
            edges.UnlockBits(bdE);
        }

        return edges;
    }

    private static Bitmap BlendEdgeTint(Bitmap baseBmp, Bitmap edges, float edgeWeight)
    {
        var bmp = new Bitmap(baseBmp.Width, baseBmp.Height, PixelFormat.Format32bppArgb);
        var bdB = baseBmp.LockBits(new Rectangle(0, 0, baseBmp.Width, baseBmp.Height), ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);
        var bdE = edges.LockBits(new Rectangle(0, 0, edges.Width, edges.Height), ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);
        var bdD = bmp.LockBits(new Rectangle(0, 0, bmp.Width, bmp.Height), ImageLockMode.WriteOnly, PixelFormat.Format32bppArgb);
        try
        {
            unsafe
            {
                var pb = (byte*)bdB.Scan0.ToPointer();
                var pe = (byte*)bdE.Scan0.ToPointer();
                var pd = (byte*)bdD.Scan0.ToPointer();
                var sb = bdB.Stride;
                var se = bdE.Stride;
                var sd = bdD.Stride;
                var h = baseBmp.Height;
                var w = baseBmp.Width;
                for (var y = 0; y < h; y++)
                {
                    for (var x = 0; x < w; x++)
                    {
                        var ib = y * sb + x * 4;
                        var ie = y * se + x * 4;
                        var od = y * sd + x * 4;
                        var e = pe[ie] / 255f * edgeWeight;
                        pd[od + 0] = ClampByte(pb[ib + 0] * (1 - e) + 40 * e);
                        pd[od + 1] = ClampByte(pb[ib + 1] * (1 - e) + 70 * e);
                        pd[od + 2] = ClampByte(pb[ib + 2] * (1 - e) + 120 * e);
                        pd[od + 3] = pb[ib + 3];
                    }
                }
            }
        }
        finally
        {
            baseBmp.UnlockBits(bdB);
            edges.UnlockBits(bdE);
            bmp.UnlockBits(bdD);
        }

        return bmp;
    }

    private static byte ClampByte(float v) => (byte)Math.Clamp((int)v, 0, 255);
}
