namespace DevProject.Business.Getters
{
    using System.Net.Http.Headers;
    using System.Text.Json;
    using System.Text.RegularExpressions;
    using Data.Entities;
    using Infrastructure.GitHub;
    using Interfaces;
    using Microsoft.Extensions.Hosting;
    using Microsoft.Extensions.Logging;
    using Microsoft.Extensions.Options;

    public class TmForumGitHubRegistryGetter : ITmForumGitHubRegistryGetter
    {
        private const string HttpClientName = "TmForumGitHub";
        private const string ManifestFileName = "manifest.json";

        private readonly IHttpClientFactory _httpClientFactory;
        private readonly TmForumGitHubSettings _settings;
        private readonly string _cacheDir;
        private readonly ILogger<TmForumGitHubRegistryGetter> _logger;
        private static readonly SemaphoreSlim Semaphore = new(1, 1);
        private static IReadOnlyList<TmForumApiDefinition>? _memoryCache;
        private static DateTime _memoryCachedAt;

        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            PropertyNamingPolicy        = JsonNamingPolicy.CamelCase,
            PropertyNameCaseInsensitive = true,
            WriteIndented               = true
        };

        public TmForumGitHubRegistryGetter(
            IHttpClientFactory httpClientFactory,
            IOptions<TmForumGitHubSettings> settings,
            IHostEnvironment environment,
            ILogger<TmForumGitHubRegistryGetter> logger)
        {
            _httpClientFactory = httpClientFactory;
            _settings          = settings.Value;
            _logger            = logger;
            _cacheDir          = Path.Combine(environment.ContentRootPath, _settings.CacheDirectoryName);
        }

        public async Task<IReadOnlyList<TmForumApiDefinition>> GetAllApisAsync(CancellationToken ct = default)
        {            if (_memoryCache is not null && !IsCacheStale(_memoryCachedAt))
                return _memoryCache;

            await Semaphore.WaitAsync(ct);
            try
            {                if (_memoryCache is not null && !IsCacheStale(_memoryCachedAt))
                    return _memoryCache;
                var manifest = await TryLoadManifestAsync(ct);
                if (manifest is not null && !IsCacheStale(manifest.FetchedAt))
                {
                    _memoryCache    = manifest.Apis;
                    _memoryCachedAt = manifest.FetchedAt;
                    _logger.LogInformation(
                        "TM Forum registry loaded from disk cache ({Count} APIs, fetched {FetchedAt:u}).",
                        manifest.Apis.Count, manifest.FetchedAt);
                    return _memoryCache;
                }
                _logger.LogInformation("Fetching TM Forum API registry from GitHub ({Org})…",
                    _settings.Organization);

                var apis = await FetchFromGitHubAsync(ct);

                await SaveManifestAsync(apis, ct);

                _memoryCache    = apis;
                _memoryCachedAt = DateTime.UtcNow;

                _logger.LogInformation(
                    "TM Forum registry refreshed from GitHub — {Count} APIs discovered.", apis.Count);

                return _memoryCache;
            }
            finally
            {
                Semaphore.Release();
            }
        }

        public async Task<string?> GetValidationSchemaAsync(string apiId, CancellationToken ct = default)
        {
            var schemaPath = SchemaPath(apiId);
            if (File.Exists(schemaPath))
                return await File.ReadAllTextAsync(schemaPath, ct);
            await GetAllApisAsync(ct);

            return File.Exists(schemaPath)
                ? await File.ReadAllTextAsync(schemaPath, ct)
                : null;
        }

        private async Task<IReadOnlyList<TmForumApiDefinition>> FetchFromGitHubAsync(CancellationToken ct)
        {
            var client = CreateHttpClient();
            var repos  = await ListTmForumReposAsync(client, ct);

            Directory.CreateDirectory(_cacheDir);

            var apis = new List<TmForumApiDefinition>();

            foreach (var repo in repos)
            {
                ct.ThrowIfCancellationRequested();

                var apiId = ExtractApiId(repo.Name);
                if (apiId is null) continue;

                try
                {
                    var swaggerJson = await DownloadLatestSwaggerAsync(client, repo, ct);
                    if (swaggerJson is null)
                    {
                        _logger.LogDebug("No swagger file found in {Repo}.", repo.Name);
                        continue;
                    }

                    var apiDef = SwaggerApiExtractor.ExtractDefinition(apiId, swaggerJson);
                    if (apiDef is null)
                    {
                        _logger.LogDebug("Could not parse API definition from {Repo}.", repo.Name);
                        continue;
                    }
                    var schemaJson = SwaggerApiExtractor.BuildValidationSchema(swaggerJson, apiDef.ResourceType);
                    if (schemaJson is not null)
                        await File.WriteAllTextAsync(SchemaPath(apiId), schemaJson, ct);

                    apis.Add(apiDef);
                    _logger.LogDebug("Registered {ApiId} – {Name} ({ResourceType}).",
                        apiId, apiDef.Name, apiDef.ResourceType);
                }
                catch (OperationCanceledException) { throw; }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to process GitHub repo {Repo}.", repo.Name);
                }
            }

            return apis.OrderBy(a => a.ApiId).ToList();
        }
        private async Task<List<GitHubRepo>> ListTmForumReposAsync(HttpClient client, CancellationToken ct)
        {
            var all  = new List<GitHubRepo>();
            var page = 1;

            while (true)
            {
                var url   = $"orgs/{_settings.Organization}/repos?type=public&per_page=100&page={page}";
                var repos = await GetJsonAsync<List<GitHubRepo>>(client, url, ct);

                if (repos is null || repos.Count == 0) break;

                all.AddRange(repos.Where(r => IsApiRepo(r.Name)));
                if (repos.Count < 100) break;
                page++;
            }

            _logger.LogInformation("Found {Count} TMF API repositories.", all.Count);
            return all;
        }        private async Task<string?> DownloadLatestSwaggerAsync(
            HttpClient client, GitHubRepo repo, CancellationToken ct)
        {
            var contents = await GetJsonAsync<List<GitHubContentItem>>(
                client, $"repos/{_settings.Organization}/{repo.Name}/contents/", ct);

            if (contents is null) return null;

            var swaggerFile = PickBestSwaggerFile(contents);
            if (swaggerFile is null)
            {
                var swaggerDir = contents.FirstOrDefault(
                    c => c.Type == "dir" && c.Name.Equals("swagger", StringComparison.OrdinalIgnoreCase));

                if (swaggerDir is not null)
                {
                    var subContents = await GetJsonAsync<List<GitHubContentItem>>(
                        client, $"repos/{_settings.Organization}/{repo.Name}/contents/swagger", ct);

                    swaggerFile = PickBestSwaggerFile(subContents ?? []);
                }
            }

            if (swaggerFile?.DownloadUrl is null) return null;

            return await client.GetStringAsync(swaggerFile.DownloadUrl, ct);
        }        private static GitHubContentItem? PickBestSwaggerFile(IEnumerable<GitHubContentItem> items)
        {
            return items
                .Where(c => c.Type == "file"
                         && c.DownloadUrl is not null
                         && (c.Name.EndsWith(".swagger.json", StringComparison.OrdinalIgnoreCase)
                          || c.Name.Equals("swagger.json",   StringComparison.OrdinalIgnoreCase)
                          || c.Name.Equals("openapi.json",   StringComparison.OrdinalIgnoreCase)))
                .OrderByDescending(c => ExtractVersion(c.Name))
                .FirstOrDefault();
        }

        private HttpClient CreateHttpClient()
        {
            var client = _httpClientFactory.CreateClient(HttpClientName);
            client.Timeout = TimeSpan.FromSeconds(_settings.RequestTimeoutSeconds);
            return client;
        }

        private static async Task<T?> GetJsonAsync<T>(HttpClient client, string url, CancellationToken ct)
        {
            using var response = await client.GetAsync(url, ct);

            if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
                return default;

            if (response.StatusCode == System.Net.HttpStatusCode.Forbidden
             || response.StatusCode == System.Net.HttpStatusCode.TooManyRequests)
            {
                throw new HttpRequestException(
                    $"GitHub API rate limit reached (HTTP {(int)response.StatusCode}). " +
                    "Configure a PersonalAccessToken in TmForumGitHub settings to increase the limit.");
            }

            response.EnsureSuccessStatusCode();

            await using var stream = await response.Content.ReadAsStreamAsync(ct);
            return await JsonSerializer.DeserializeAsync<T>(stream,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true }, ct);
        }

        private async Task<RegistryManifest?> TryLoadManifestAsync(CancellationToken ct)
        {
            var path = Path.Combine(_cacheDir, ManifestFileName);
            if (!File.Exists(path)) return null;

            try
            {
                var json = await File.ReadAllTextAsync(path, ct);
                return JsonSerializer.Deserialize<RegistryManifest>(json, JsonOptions);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Could not read TM Forum registry manifest.");
                return null;
            }
        }

        private async Task SaveManifestAsync(IReadOnlyList<TmForumApiDefinition> apis, CancellationToken ct)
        {
            Directory.CreateDirectory(_cacheDir);
            var manifest = new RegistryManifest { FetchedAt = DateTime.UtcNow, Apis = apis.ToList() };
            var json     = JsonSerializer.Serialize(manifest, JsonOptions);
            await File.WriteAllTextAsync(Path.Combine(_cacheDir, ManifestFileName), json, ct);
        }

        private string SchemaPath(string apiId) =>
            Path.Combine(_cacheDir, $"{apiId}.schema.json");

        private bool IsCacheStale(DateTime since) =>
            DateTime.UtcNow - since >= TimeSpan.FromHours(_settings.CacheTtlHours);

        private static bool IsApiRepo(string repoName) =>
            Regex.IsMatch(repoName, @"^TMF\d+", RegexOptions.IgnoreCase);

        private static string? ExtractApiId(string repoName)
        {
            var match = Regex.Match(repoName, @"^(TMF\d+)", RegexOptions.IgnoreCase);
            return match.Success ? match.Groups[1].Value.ToUpperInvariant() : null;
        }        private static (int Major, int Minor, int Patch) ExtractVersion(string filename)
        {
            var m = Regex.Match(filename, @"[vV](\d+)\.(\d+)(?:\.(\d+))?");
            if (!m.Success) return (0, 0, 0);
            return (
                int.Parse(m.Groups[1].Value),
                int.Parse(m.Groups[2].Value),
                m.Groups[3].Success ? int.Parse(m.Groups[3].Value) : 0
            );
        }

        private class RegistryManifest
        {
            public DateTime FetchedAt { get; set; }
            public List<TmForumApiDefinition> Apis { get; set; } = new();
        }
    }
}
