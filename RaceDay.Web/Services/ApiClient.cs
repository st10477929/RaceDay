using System.Net;
using System.Text;
using System.Text.Json;

namespace RaceDay.Web.Services;

public record ApiResult<T>(bool Ok, T? Data, string? Error, HttpStatusCode Status);

public interface IApiClient
{
    Task<ApiResult<T>> GetAsync<T>(string path);
    Task<ApiResult<T>> PostAsync<T>(string path, object? body = null);
    Task<ApiResult<T>> PutAsync<T>(string path, object body);
    Task<ApiResult<bool>> DeleteAsync(string path);
    /// <summary>Sends a file to the API as multipart/form-data (form field name "file").</summary>
    Task<ApiResult<T>> UploadAsync<T>(string path, IFormFile file);
}

/// <summary>
/// The only class that talks to the Part 2 API. The API authenticates with a session cookie, so this
/// client saves that cookie in the MVC session at login and sends it back on every later call.
/// </summary>
public class ApiClient(HttpClient http, IHttpContextAccessor accessor) : IApiClient
{
    private const string ApiCookieName = ".RaceDay.Session";
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    private ISession Session => accessor.HttpContext!.Session;

    public Task<ApiResult<T>> GetAsync<T>(string path) =>
        SendAsync<T>(new HttpRequestMessage(HttpMethod.Get, path));

    public Task<ApiResult<T>> PostAsync<T>(string path, object? body = null) =>
        SendAsync<T>(WithJson(new HttpRequestMessage(HttpMethod.Post, path), body));

    public Task<ApiResult<T>> PutAsync<T>(string path, object body) =>
        SendAsync<T>(WithJson(new HttpRequestMessage(HttpMethod.Put, path), body));

    public async Task<ApiResult<bool>> DeleteAsync(string path)
    {
        var r = await SendAsync<object>(new HttpRequestMessage(HttpMethod.Delete, path));
        return new ApiResult<bool>(r.Ok, r.Ok, r.Error, r.Status);
    }

    public Task<ApiResult<T>> UploadAsync<T>(string path, IFormFile file)
    {
        var content = new MultipartFormDataContent();
        content.Add(new StreamContent(file.OpenReadStream()), "file", file.FileName);
        return SendAsync<T>(new HttpRequestMessage(HttpMethod.Post, path) { Content = content });
    }

    private static HttpRequestMessage WithJson(HttpRequestMessage req, object? body)
    {
        if (body is not null)
            req.Content = new StringContent(JsonSerializer.Serialize(body, Json), Encoding.UTF8, "application/json");
        return req;
    }

    private async Task<ApiResult<T>> SendAsync<T>(HttpRequestMessage req)
    {
        // Forward the API session cookie saved at login.
        var cookie = Session.GetString(SessionKeys.ApiCookie);
        if (!string.IsNullOrEmpty(cookie))
            req.Headers.TryAddWithoutValidation("Cookie", $"{ApiCookieName}={cookie}");

        HttpResponseMessage resp;
        try
        {
            resp = await http.SendAsync(req);
        }
        catch (HttpRequestException)
        {
            return new ApiResult<T>(false, default, "The RaceDay service is unavailable. Please try again shortly.", HttpStatusCode.ServiceUnavailable);
        }

        // The API forgot our session (idle timeout or restart): flag it so SessionExpiredFilter sends the user to Login.
        if (resp.StatusCode == HttpStatusCode.Unauthorized && !string.IsNullOrEmpty(cookie))
            accessor.HttpContext!.Items[SessionKeys.ApiExpired] = true;

        // Keep the API cookie up to date (set on login).
        if (resp.Headers.TryGetValues("Set-Cookie", out var setCookies))
        {
            foreach (var sc in setCookies)
            {
                if (!sc.StartsWith(ApiCookieName + "=", StringComparison.Ordinal)) continue;
                var value = sc[(ApiCookieName.Length + 1)..].Split(';')[0];
                Session.SetString(SessionKeys.ApiCookie, value);
            }
        }

        var text = await resp.Content.ReadAsStringAsync();

        if (!resp.IsSuccessStatusCode)
            return new ApiResult<T>(false, default, FriendlyError(text, resp.StatusCode), resp.StatusCode);

        if (string.IsNullOrWhiteSpace(text))
            return new ApiResult<T>(true, default, null, resp.StatusCode);

        try
        {
            return new ApiResult<T>(true, JsonSerializer.Deserialize<T>(text, Json), null, resp.StatusCode);
        }
        catch (JsonException)
        {
            return new ApiResult<T>(false, default, "The RaceDay service returned an unexpected response.", resp.StatusCode);
        }
    }

    /// <summary>Turns the API's error body into a message that is safe to show to a user.</summary>
    private static string FriendlyError(string body, HttpStatusCode status)
    {
        try
        {
            using var doc = JsonDocument.Parse(body);
            var root = doc.RootElement;
            if (root.TryGetProperty("message", out var m) && m.ValueKind == JsonValueKind.String)
                return m.GetString()!;
            if (root.TryGetProperty("errors", out var errs) && errs.ValueKind == JsonValueKind.Object)
            {
                var msgs = errs.EnumerateObject()
                    .SelectMany(p => p.Value.EnumerateArray().Select(x => x.GetString()))
                    .Where(s => !string.IsNullOrWhiteSpace(s));
                var joined = string.Join(" ", msgs);
                if (joined.Length > 0) return joined;
            }
        }
        catch (JsonException) { }

        return status switch
        {
            HttpStatusCode.Unauthorized => "Please log in to continue.",
            HttpStatusCode.Forbidden => "You do not have permission to do that.",
            HttpStatusCode.NotFound => "That item could not be found.",
            _ => "Something went wrong. Please try again."
        };
    }
}
