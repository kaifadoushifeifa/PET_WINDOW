using System.IO;
using System.Windows.Media.Imaging;

namespace PetWindow.Services;

public sealed class SkinLoader
{
    private readonly string _skinsRoot;

    public SkinLoader(string skinsRoot)
    {
        _skinsRoot = skinsRoot;
    }

    public IReadOnlyList<string> ListSkinNames()
    {
        if (!Directory.Exists(_skinsRoot))
            return Array.Empty<string>();
        return Directory.GetDirectories(_skinsRoot)
            .Select(Path.GetFileName)
            .Where(n => !string.IsNullOrEmpty(n))
            .Cast<string>()
            .OrderBy(n => n, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    public IReadOnlyList<BitmapImage> LoadFrames(string skinName)
    {
        var dir = Path.Combine(_skinsRoot, skinName);
        if (!Directory.Exists(dir))
            return Array.Empty<BitmapImage>();

        // 包含子目录（例如 Skins\mytest\images\*.png），仅根目录时行为与原来一致
        var files = Directory.GetFiles(dir, "*.*", SearchOption.AllDirectories)
            .Where(f =>
            {
                var ext = Path.GetExtension(f).ToLowerInvariant();
                return ext is ".png" or ".jpg" or ".jpeg" or ".gif" or ".webp";
            })
            .OrderBy(f => f, StringComparer.OrdinalIgnoreCase)
            .ToList();

        var list = new List<BitmapImage>();
        foreach (var path in files)
        {
            try
            {
                var bmp = new BitmapImage();
                bmp.BeginInit();
                bmp.CacheOption = BitmapCacheOption.OnLoad;
                bmp.UriSource = new Uri(path, UriKind.Absolute);
                bmp.EndInit();
                bmp.Freeze();
                list.Add(bmp);
            }
            catch
            {
                // skip broken image
            }
        }

        return list;
    }
}
