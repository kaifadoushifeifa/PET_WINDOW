using System.IO;
using System.Windows;
using PetWindow.Models;

namespace PetWindow.Services;

public sealed class AnimeSkinGenerator
{
    private readonly string _skinsRoot;

    public AnimeSkinGenerator(string skinsRoot)
    {
        _skinsRoot = skinsRoot;
    }

    public async Task<string> GenerateFromPhotoAsync(string sourcePath, AppSettings settings, CancellationToken ct)
    {
        var bytes = await File.ReadAllBytesAsync(sourcePath, ct).ConfigureAwait(false);
        var ext = Path.GetExtension(sourcePath).ToLowerInvariant();
        var mime = ext switch
        {
            ".png" => "image/png",
            ".jpg" or ".jpeg" => "image/jpeg",
            ".bmp" => "image/bmp",
            _ => "image/jpeg"
        };

        byte[] styled;
        if (!string.IsNullOrWhiteSpace(settings.HuggingFaceApiToken))
        {
            var model = string.IsNullOrWhiteSpace(settings.AnimeStyleModelId)
                ? AnimeSkinDefaults.DefaultModelId
                : settings.AnimeStyleModelId.Trim();
            try
            {
                styled = await HuggingFaceAnimeClient.StylizeAsync(bytes, mime, settings.HuggingFaceApiToken!, model, ct)
                    .ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                var r = System.Windows.MessageBox.Show(
                    $"云端推理失败：{ex.Message}\n\n是否改用本地「二次元滤镜」生成？",
                    "二次元生成",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question,
                    MessageBoxResult.Yes);
                if (r != MessageBoxResult.Yes)
                    throw;
                styled = OfflineAnimeStyleProcessor.Process(bytes);
            }
        }
        else
        {
            styled = OfflineAnimeStyleProcessor.Process(bytes);
        }

        var frames = PetSpriteNormalizer.BuildFourFrames(styled, 128, 160);
        var folder = $"Gen_{DateTime.Now:yyyyMMdd_HHmmss}";
        var dir = Path.Combine(_skinsRoot, folder);
        Directory.CreateDirectory(dir);
        for (var i = 0; i < frames.Length; i++)
            await File.WriteAllBytesAsync(Path.Combine(dir, $"gen_{i + 1:D2}.png"), frames[i], ct).ConfigureAwait(false);

        return folder;
    }
}
