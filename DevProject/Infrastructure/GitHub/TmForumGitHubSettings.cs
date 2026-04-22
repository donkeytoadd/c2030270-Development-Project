namespace DevProject.Infrastructure.GitHub
{
    public class TmForumGitHubSettings
    {
        public const string Section = "TmForumGitHub";

        public string Organization { get; set; } = "tmforum-apis";
        public string? PersonalAccessToken { get; set; }
        public int CacheTtlHours { get; set; } = 24;
        public string CacheDirectoryName { get; set; } = "tmforum-cache";
        public int RequestTimeoutSeconds { get; set; } = 30;
    }
}