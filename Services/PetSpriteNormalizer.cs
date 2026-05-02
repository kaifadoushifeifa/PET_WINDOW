using System.Drawing;
using System.IO;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;

namespace PetWindow.Services;

public static class PetSpriteNormalizer
{
    public static byte[][] BuildFourFrames(byte[] styledImageBytes, int targetW, int targetH)
    {
        using var ms = new MemoryStream(styledImageBytes);
        using var src = new Bitmap(ms);
        var frames = new byte[4][];
        for (var i = 0; i < 4; i++)
        {
            using var frame = RenderFrame(src, targetW, targetH, i);
            using var mso = new MemoryStream();
            frame.Save(mso, ImageFormat.Png);
            frames[i] = mso.ToArray();
        }

        return frames;
    }

    private static Bitmap RenderFrame(Bitmap src, int tw, int th, int frameIndex)
    {
        using var canvas = new Bitmap(tw, th, PixelFormat.Format32bppArgb);
        using (var g = Graphics.FromImage(canvas))
        {
            g.Clear(Color.Transparent);
            g.InterpolationMode = InterpolationMode.HighQualityBicubic;
            g.PixelOffsetMode = PixelOffsetMode.HighQuality;
            g.CompositingQuality = CompositingQuality.HighQuality;
            var scale = Math.Max(tw / (double)src.Width, th / (double)src.Height);
            var sw = (float)(src.Width * scale);
            var sh = (float)(src.Height * scale);
            var dx = (tw - sw) / 2f;
            var dy = (th - sh) / 2f;
            g.DrawImage(src, dx, dy, sw, sh);
        }

        var delta = (frameIndex - 1.5f) * 0.045f;
        return ApplyBrightness(canvas, delta);
    }

    private static Bitmap ApplyBrightness(Bitmap src, float delta)
    {
        var bmp = new Bitmap(src.Width, src.Height, PixelFormat.Format32bppArgb);
        using var g = Graphics.FromImage(bmp);
        var m = new ColorMatrix(new[]
        {
            new float[] { 1, 0, 0, 0, 0 },
            new float[] { 0, 1, 0, 0, 0 },
            new float[] { 0, 0, 1, 0, 0 },
            new float[] { 0, 0, 0, 1, 0 },
            new float[] { delta, delta, delta, 0, 1 }
        });
        using var ia = new ImageAttributes();
        ia.SetColorMatrix(m);
        g.DrawImage(src, new Rectangle(0, 0, src.Width, src.Height), 0, 0, src.Width, src.Height, GraphicsUnit.Pixel, ia);
        return bmp;
    }
}
