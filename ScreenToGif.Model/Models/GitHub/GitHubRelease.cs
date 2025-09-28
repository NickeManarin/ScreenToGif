using System.Text.Json.Serialization;

namespace ScreenToGif.Domain.Models.GitHub;

public class GitHubRelease
{
    [JsonPropertyName("url")]
    public string Url { get; set; }

    [JsonPropertyName("assets_url")]
    public string AssetsUrl { get; set; }

    [JsonPropertyName("upload_url")]
    public string UploadUrl { get; set; }

    [JsonPropertyName("html_url")]
    public string HtmlUrl { get; set; }

    [JsonPropertyName("id")]
    public long Id { get; set; }

    [JsonPropertyName("author")]
    public GitHubUser Author { get; set; }

    [JsonPropertyName("node_id")]
    public string NodeId { get; set; }

    [JsonPropertyName("tag_name")]
    public string TagName { get; set; }

    [JsonPropertyName("target_commitish")]
    public string TargetCommitish { get; set; }

    [JsonPropertyName("name")]
    public string Name { get; set; }

    [JsonPropertyName("draft")]
    public bool IsDraft { get; set; }

    [JsonPropertyName("prerelease")]
    public bool IsPrerelease { get; set; }

    [JsonPropertyName("created_at")]
    public DateTime CreatedAt { get; set; }

    [JsonPropertyName("published_at")]
    public DateTime PublishedAt { get; set; }

    [JsonPropertyName("assets")]
    public GitHubAsset[] Assets { get; set; }
}
