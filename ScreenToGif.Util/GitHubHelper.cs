using ScreenToGif.Domain.Models.GitHub;
using ScreenToGif.Util.Settings;
using System.Text.Json;

namespace ScreenToGif.Util;

public static class GitHubHelper
{
    public static async Task<GitHubRelease> GetLatestRelease(string repository)
    {
        if (string.IsNullOrWhiteSpace(repository))
            throw new ArgumentNullException(nameof(repository));

        using var client = WebHelper.GetHttpClient();
        client.DefaultRequestHeaders.Add("User-Agent", "ScreenToGif/" + UserSettings.All.VersionText);

        using var response = await client.GetAsync($"https://api.github.com/repos/{repository}/releases/latest");

        if (!response.IsSuccessStatusCode)
            throw new Exception("Error while trying to get the latest release: " + Environment.NewLine + response.ReasonPhrase);

        var content = await response.Content.ReadAsStringAsync();

        return JsonSerializer.Deserialize<GitHubRelease>(content);
    }

    public static GitHubAsset GetAsset(this GitHubRelease release, string assetName)
    {
        if (release is null)
            throw new ArgumentNullException(nameof(release));

        if (string.IsNullOrWhiteSpace(assetName))
            throw new ArgumentNullException(nameof(assetName));

        return release.Assets.FirstOrDefault(x => x.Name.Contains(assetName, StringComparison.InvariantCultureIgnoreCase));
    }
}