using System.Net.Http;
using System.Text.Json;

namespace PetWindow.Services;

public sealed class WeatherClient
{
    private readonly HttpClient _http = new() { Timeout = TimeSpan.FromSeconds(8) };

    public async Task<string?> GetBriefAsync(double lat, double lon, CancellationToken ct = default)
    {
        var url =
            $"https://api.open-meteo.com/v1/forecast?latitude={lat:F4}&longitude={lon:F4}&current_weather=true";
        try
        {
            await using var stream = await _http.GetStreamAsync(url, ct).ConfigureAwait(false);
            using var doc = await JsonDocument.ParseAsync(stream, cancellationToken: ct).ConfigureAwait(false);
            if (!doc.RootElement.TryGetProperty("current_weather", out var cw))
                return null;
            if (!cw.TryGetProperty("temperature", out var t))
                return null;
            if (!cw.TryGetProperty("weathercode", out var code))
                return $"{t.GetDouble():F0}°C";
            return $"{t.GetDouble():F0}°C W{code.GetInt32()}";
        }
        catch
        {
            return null;
        }
    }
}
