using System.Net.Http.Headers;
using System.Reflection;
using System.Text.Json;

namespace BHelper.App.Utils;

public static class UpdateChecker
{
    public const string LatestReleaseUrl =
        "https://github.com/longvoquy/Lecco-helper/releases/latest";

    private const string ApiUrl =
        "https://api.github.com/repos/longvoquy/Lecco-helper/releases/latest";

    public static async Task<Version?> GetLatestVersionAsync()
    {
        try
        {
            using var client = new HttpClient();
            client.DefaultRequestHeaders.UserAgent.Add(
                new ProductInfoHeaderValue(AppBranding.AppId, CurrentVersion.ToString()));

            var json = await client.GetStringAsync(ApiUrl);
            using var doc = JsonDocument.Parse(json);

            var tag = doc.RootElement
                .GetProperty("tag_name")
                .GetString()?
                .TrimStart('v', 'V');

            return Version.TryParse(tag, out var version)
                ? version
                : null;
        }
        catch
        {
            return null;
        }
    }

    public static Version CurrentVersion =>
        Version.Parse(
            Assembly
                .GetExecutingAssembly()
                .GetName()
                .Version!
                .ToString(3));
}
