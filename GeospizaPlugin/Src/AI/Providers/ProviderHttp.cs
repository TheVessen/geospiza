#nullable enable
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

namespace GeospizaPlugin.AI;

internal static class ProviderHttp
{
    /// <summary>
    ///     Throws with the service's own error text when the body carries one ("error" as string or
    ///     {message}, "message", or FastAPI's "detail"), falling back to the HTTP status line.
    /// </summary>
    public static async Task EnsureSuccessAsync(HttpResponseMessage resp, string service)
    {
        if (resp.IsSuccessStatusCode) return;

        string? detail = null;
        try
        {
            var body = await resp.Content.ReadAsStringAsync().ConfigureAwait(false);
            detail = ExtractError(JToken.Parse(body));
        }
        catch
        {
            // Not JSON; the status line is all there is.
        }

        throw new HttpRequestException(string.IsNullOrEmpty(detail)
            ? $"{service} returned {(int)resp.StatusCode} {resp.ReasonPhrase}"
            : $"{service}: {detail}");
    }

    private static string? ExtractError(JToken token)
    {
        if (token is not JObject obj) return null;
        var error = obj["error"] ?? obj["detail"] ?? obj["message"];
        return error switch
        {
            null => null,
            JValue value => value.ToString(),
            JObject inner => inner["message"]?.ToString() ?? inner.ToString(Newtonsoft.Json.Formatting.None),
            JArray items => string.Join("; ", items.Select(i => i["msg"]?.ToString() ?? i.ToString())),
            _ => error.ToString()
        };
    }
}
