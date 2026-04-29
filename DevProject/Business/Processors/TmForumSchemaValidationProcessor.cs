namespace DevProject.Business.Processors
{
    using System.Collections.Concurrent;
    using System.Reflection;
    using System.Text.Json;
    using System.Text.Json.Nodes;
    using Data.Entities;
    using Data.Enums;
    using Interfaces;
    using Json.Schema;

    public class TmForumSchemaValidationProcessor : ITmForumSchemaValidationProcessor
    {        private static readonly ConcurrentDictionary<string, JsonSchema> SchemaCache =
            new(StringComparer.OrdinalIgnoreCase);

        public ValidationReport Validate(List<JsonNode> resources, string apiId)
        {
            var schema = LoadSchema(apiId);
            if (schema is null)
                return new ValidationReport { IsValid = true };

            var issues      = new List<ValidationIssue>();
            var evalOptions = new EvaluationOptions { OutputFormat = OutputFormat.List };

            for (var i = 0; i < resources.Count; i++)
            {
                var resource     = resources[i];
                var json         = resource?.ToJsonString() ?? "{}";
                var resourceName = resource?["name"]?.GetValue<string>() ?? $"Resource_{i + 1}";

                using var doc = JsonDocument.Parse(json);
                var result    = schema.Evaluate(doc.RootElement, evalOptions);

                if (!result.IsValid)
                {
                    foreach (var detail in (result.Details ?? []).Where(d => !d.IsValid && d.Errors is not null))
                    {
                        foreach (var (_, message) in detail.Errors!)
                        {
                            issues.Add(new ValidationIssue
                            {
                                ResourceIndex = i,
                                ResourceName  = resourceName,
                                Field         = detail.InstanceLocation.ToString(),
                                Message       = message,
                                Severity      = IsFalseSchemaMessage(message)
                                    ? ValidationSeverity.SchemaArtefact
                                    : ValidationSeverity.Error,
                            });
                        }
                    }
                }
            }

            var errorIssues        = issues.Where(i => i.Severity == ValidationSeverity.Error).ToList();
            var distinctErrorPaths = errorIssues.Select(i => i.Field).Distinct(StringComparer.Ordinal).Count();
            var totalPaths         = resources.Sum(r => CountJsonLeafPaths(r));
            var artefactPaths      = issues
                .Where(i => i.Severity == ValidationSeverity.SchemaArtefact)
                .Select(i => i.Field)
                .Distinct(StringComparer.Ordinal)
                .Count();
            var effectivePaths = totalPaths - artefactPaths;
            var compliance     = effectivePaths <= 0
                ? 100.0
                : Math.Round(Math.Max(0, (effectivePaths - distinctErrorPaths) / (double)effectivePaths * 100), 1);

            return new ValidationReport
            {
                IsValid              = errorIssues.Count == 0,
                TotalIssues          = errorIssues.Count,
                ArtefactCount        = issues.Count(i => i.Severity == ValidationSeverity.SchemaArtefact),
                CompliancePercentage = compliance,
                Issues               = issues
            };
        }        private static bool IsFalseSchemaMessage(string message) =>
            message.Contains("All values fail against the false schema",
                StringComparison.OrdinalIgnoreCase);

        private static int CountJsonLeafPaths(JsonNode? node)
        {
            return node switch
            {
                JsonObject obj => obj.Sum(kv =>
                    kv.Value is JsonObject || kv.Value is JsonArray
                        ? CountJsonLeafPaths(kv.Value)
                        : 1),
                JsonArray arr  => arr.Sum(CountJsonLeafPaths),
                _              => 1
            };
        }

        private static JsonSchema? LoadSchema(string apiId)
        {
            return SchemaCache.GetOrAdd(apiId, id =>
            {
                var schemaText = TryLoadEmbedded(id);
                return schemaText is null ? null! : JsonSchema.FromText(schemaText);
            });
        }

        private static string? TryLoadEmbedded(string apiId)
        {
            var resourceName = $"DevProject.Schemas.{apiId}.json";
            using var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(resourceName);
            if (stream is null) return null;

            using var reader = new StreamReader(stream);
            return reader.ReadToEnd();
        }
    }
}