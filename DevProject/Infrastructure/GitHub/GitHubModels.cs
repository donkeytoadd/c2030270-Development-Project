namespace DevProject.Infrastructure.GitHub
{
    using System.Text.Json.Serialization;

    internal record GitHubRepo(
        [property: JsonPropertyName("name")]           string Name,
        [property: JsonPropertyName("default_branch")] string DefaultBranch
    );

    internal record GitHubContentItem(
        [property: JsonPropertyName("name")]         string  Name,
        [property: JsonPropertyName("type")]         string  Type,
        [property: JsonPropertyName("download_url")] string? DownloadUrl
    );
}
