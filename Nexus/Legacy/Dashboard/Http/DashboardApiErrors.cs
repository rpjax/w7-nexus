using System.Net.Http.Json;
using System.Text.Json;

namespace Nexus.Legacy.Dashboard.Http;

public static class DashboardApiErrors
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public static async Task<string> ReadMessageAsync(HttpResponseMessage response, string fallback)
    {
        try
        {
            var raw = await response.Content.ReadAsStringAsync();
            if (string.IsNullOrWhiteSpace(raw))
                return fallback;

            using var json = JsonDocument.Parse(raw);
            if (json.RootElement.TryGetProperty("errors", out var errorsNode) && errorsNode.ValueKind == JsonValueKind.Array)
            {
                var messages = errorsNode.EnumerateArray()
                    .Select(item => item.TryGetProperty("message", out var msg) ? msg.GetString() : null)
                    .Where(msg => !string.IsNullOrWhiteSpace(msg))
                    .ToArray();
                if (messages.Length > 0)
                    return string.Join("; ", messages);
            }

            if (json.RootElement.TryGetProperty("detail", out var detailNode))
            {
                var detail = detailNode.GetString();
                if (!string.IsNullOrWhiteSpace(detail))
                    return detail;
            }

            if (json.RootElement.TryGetProperty("title", out var titleNode))
            {
                var title = titleNode.GetString();
                if (!string.IsNullOrWhiteSpace(title))
                    return title;
            }
        }
        catch
        {
        }

        return fallback;
    }

    public static async Task<T?> ReadJsonAsync<T>(HttpResponseMessage response)
        => await response.Content.ReadFromJsonAsync<T>(JsonOptions);
}
