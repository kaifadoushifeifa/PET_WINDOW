using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;

namespace PetWindow.Services;

public static class HuggingFaceAnimeClient
{
    public static async Task<byte[]> StylizeAsync(
        byte[] imageBytes,
        string imageMime,
        string bearerToken,
        string modelId,
        CancellationToken ct)
    {
        using var client = new HttpClient { Timeout = TimeSpan.FromMinutes(3) };
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", bearerToken.Trim());

        var url = $"https://api-inference.huggingface.co/models/{modelId.Trim().Trim('/')}";
        const int attempts = 4;
        for (var attempt = 0; attempt < attempts; attempt++)
        {
            using var content = new ByteArrayContent(imageBytes);
            content.Headers.ContentType = new MediaTypeHeaderValue(imageMime);

            using var resp = await client.PostAsync(url, content, ct).ConfigureAwait(false);
            var bytes = await resp.Content.ReadAsByteArrayAsync(ct).ConfigureAwait(false);

            if ((int)resp.StatusCode == 503 && attempt < attempts - 1)
            {
                await Task.Delay(TimeSpan.FromSeconds(5 + attempt * 3), ct).ConfigureAwait(false);
                continue;
            }

            if (!resp.IsSuccessStatusCode)
            {
                var hint = TryDecodeUtf8(bytes);
                throw new InvalidOperationException($"{(int)resp.StatusCode} {resp.ReasonPhrase}: {hint}");
            }

            var media = resp.Content.Headers.ContentType?.MediaType ?? "";
            if (media.Contains("json", StringComparison.OrdinalIgnoreCase) ||
                (bytes.Length > 2 && bytes[0] == '{' && bytes[1] == '"'))
            {
                throw new InvalidOperationException(TryDecodeUtf8(bytes));
            }

            return bytes;
        }

        throw new InvalidOperationException("云端多次重试仍不可用。");
    }

    private static string TryDecodeUtf8(byte[] bytes)
    {
        if (bytes.Length == 0)
            return "";
        try
        {
            var s = Encoding.UTF8.GetString(bytes);
            return s.Length > 800 ? s[..800] + "…" : s;
        }
        catch
        {
            return "(binary)";
        }
    }
}
