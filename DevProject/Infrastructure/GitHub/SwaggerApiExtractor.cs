namespace DevProject.Infrastructure.GitHub
{
    using System.Text.Json;
    using System.Text.Json.Nodes;
    using System.Text.RegularExpressions;
    using Data.Entities;

    internal static class SwaggerApiExtractor
    {
        private static readonly JsonSerializerOptions WriteIndented = new() { WriteIndented = true };
        
        public static TmForumApiDefinition? ExtractDefinition(string apiId, string swaggerJson)
        {
            try
            {
                using var doc = JsonDocument.Parse(swaggerJson);
                var root = doc.RootElement;
                var isOas3 = root.TryGetProperty("openapi", out _);

                if (!root.TryGetProperty("info", out var info)) return null;
                var name    = info.TryGetProperty("title",   out var t) ? t.GetString() ?? apiId : apiId;
                var version = info.TryGetProperty("version", out var v) ? NormaliseVersion(v.GetString() ?? "1.0") : "1.0";

                var basePath = isOas3 ? ExtractOas3BasePath(root)
                                      : root.TryGetProperty("basePath", out var bp) ? bp.GetString() ?? "/" : "/";

                var (resourceType, collectionPath) = FindMainResource(root, isOas3);
                if (resourceType is null) return null;

                var definitions = GetDefinitions(root, isOas3);

                var charArrayKey  = "characteristic";
                var charValueKey  = "characteristicValue";

                if (definitions.HasValue
                    && definitions.Value.TryGetProperty(resourceType, out var resourceDef))
                {
                    var charKey = FindCharacteristicArrayKey(resourceDef);
                    if (charKey is not null)
                    {
                        charArrayKey = charKey;

                        var charTypeName = FindRefedTypeName(resourceDef, charKey);
                        if (charTypeName is not null
                            && definitions.Value.TryGetProperty(charTypeName, out var charTypeDef))
                        {
                            charValueKey = FindCharacteristicValueKey(charTypeDef) ?? charValueKey;
                        }
                    }
                }

                return new TmForumApiDefinition
                {
                    ApiId                    = apiId,
                    Name                     = name,
                    ResourceType             = resourceType,
                    BasePath                 = basePath.TrimEnd('/'),
                    ResourceCollectionPath   = collectionPath ?? ToCamelCase(resourceType),
                    Version                  = version,
                    CharacteristicArrayKey   = charArrayKey,
                    CharacteristicValueArrayKey = charValueKey
                };
            }
            catch
            {
                return null;
            }
        }

        public static string? BuildValidationSchema(string swaggerJson, string resourceType)
        {
            try
            {
                var swaggerNode = JsonNode.Parse(swaggerJson)?.AsObject();
                if (swaggerNode is null) return null;

                var isOas3 = swaggerNode.ContainsKey("openapi");

                JsonObject? rawDefs = isOas3
                    ? swaggerNode["components"]?.AsObject()?["schemas"]?.AsObject()
                    : swaggerNode["definitions"]?.AsObject();

                if (rawDefs is null) return null;

                var defs = JsonNode.Parse(rawDefs.ToJsonString())!.AsObject();

                if (isOas3)
                    RewriteRefs(defs, "#/components/schemas/", "#/definitions/");
                
                ApplyAdditionalPropertiesFalse(defs, resourceType);

                var schema = new JsonObject
                {
                    ["$schema"]     = "http://json-schema.org/draft-07/schema#",
                    ["$ref"]        = $"#/definitions/{resourceType}",
                    ["definitions"] = JsonNode.Parse(defs.ToJsonString())
                };

                return schema.ToJsonString(WriteIndented);
            }
            catch
            {
                return null;
            }
        }

        private static JsonElement? GetDefinitions(JsonElement root, bool isOas3)
        {
            if (isOas3)
            {
                if (root.TryGetProperty("components", out var comp)
                    && comp.TryGetProperty("schemas", out var schemas))
                    return schemas;
                return null;
            }
            return root.TryGetProperty("definitions", out var defs) ? defs : null;
        }

        private static string ExtractOas3BasePath(JsonElement root)
        {
            if (!root.TryGetProperty("servers", out var servers)) return "/";

            foreach (var server in servers.EnumerateArray())
            {
                if (!server.TryGetProperty("url", out var urlProp)) continue;
                var url = urlProp.GetString() ?? "";

                if (Uri.TryCreate(url, UriKind.Absolute, out var uri))
                    return uri.AbsolutePath;

                if (url.StartsWith('/'))
                    return url;
            }
            return "/";
        }

        private static (string? ResourceType, string? CollectionPath) FindMainResource(
            JsonElement root, bool isOas3)
        {
            if (!root.TryGetProperty("paths", out var paths))
                return (null, null);

            var candidates = new List<(string TypeName, string CollectionPath)>();

            foreach (var path in paths.EnumerateObject())
            {
                if (path.Name.Contains('{')) continue;
                if (!path.Value.TryGetProperty("post", out var post)) continue;

                var schemaRef = isOas3
                    ? ExtractOas3ResponseRef(post)
                    : ExtractSwagger2ResponseRef(post);

                if (schemaRef is null) continue;

                var typeName = schemaRef.Split('/').Last();

                if (typeName.Contains('_') || typeName.EndsWith("FVO", StringComparison.Ordinal)
                                          || typeName.EndsWith("MVO", StringComparison.Ordinal))
                    continue;

                var collectionPath = path.Name.TrimStart('/').Split('/').Last();
                candidates.Add((typeName, collectionPath));
            }

            if (candidates.Count == 0)
                return (null, null);

            static int Rank(string typeName)
            {
                if (typeName.EndsWith("Specification", StringComparison.OrdinalIgnoreCase)) return 0;
                if (typeName.EndsWith("Order",         StringComparison.OrdinalIgnoreCase)) return 1;
                if (typeName.EndsWith("Inventory",     StringComparison.OrdinalIgnoreCase)) return 2;
                if (typeName.EndsWith("Account",       StringComparison.OrdinalIgnoreCase)) return 3;
                if (typeName.EndsWith("Agreement",     StringComparison.OrdinalIgnoreCase)) return 4;
                if (typeName.EndsWith("Party",         StringComparison.OrdinalIgnoreCase)) return 5;
                if (typeName.EndsWith("Individual",    StringComparison.OrdinalIgnoreCase)) return 5;
                if (typeName.EndsWith("Organization",  StringComparison.OrdinalIgnoreCase)) return 5;
                if (typeName.EndsWith("Customer",      StringComparison.OrdinalIgnoreCase)) return 5;
                if (typeName.EndsWith("Product",       StringComparison.OrdinalIgnoreCase)) return 6;
                if (typeName.EndsWith("Service",       StringComparison.OrdinalIgnoreCase)) return 6;
                if (typeName.EndsWith("Resource",      StringComparison.OrdinalIgnoreCase)) return 6;
                if (typeName.Equals("Catalog",  StringComparison.OrdinalIgnoreCase)) return 90;
                if (typeName.Equals("Hub",      StringComparison.OrdinalIgnoreCase)) return 91;
                if (typeName.Equals("Register", StringComparison.OrdinalIgnoreCase)) return 92;
                return 50;
            }

            var best = candidates.OrderBy(c => Rank(c.TypeName)).First();
            return (best.TypeName, best.CollectionPath);
        }

        private static string? ExtractSwagger2ResponseRef(JsonElement post)
        {
            if (!post.TryGetProperty("responses", out var responses)) return null;

            foreach (var statusCode in new[] { "201", "200" })
            {
                if (!responses.TryGetProperty(statusCode, out var response)) continue;
                if (response.TryGetProperty("schema", out var schema)
                    && schema.TryGetProperty("$ref", out var refVal))
                    return refVal.GetString();
            }
            return null;
        }

        private static string? ExtractOas3ResponseRef(JsonElement post)
        {
            if (!post.TryGetProperty("responses", out var responses)) return null;

            foreach (var statusCode in new[] { "201", "200" })
            {
                if (!responses.TryGetProperty(statusCode, out var response)) continue;
                if (!response.TryGetProperty("content", out var content)) continue;

                foreach (var mediaType in content.EnumerateObject())
                {
                    if (mediaType.Value.TryGetProperty("schema", out var schema)
                        && schema.TryGetProperty("$ref", out var refVal))
                        return refVal.GetString();
                }
            }
            return null;
        }
        
        private static string? FindCharacteristicArrayKey(JsonElement definition)
        {
            if (!definition.TryGetProperty("properties", out var props)) return null;

            foreach (var prop in props.EnumerateObject())
            {
                if (!IsArrayWithRef(prop.Value, out var refName)) continue;

                if (refName.Contains("Characteristic", StringComparison.OrdinalIgnoreCase)
                    && !refName.Contains("Value",        StringComparison.OrdinalIgnoreCase)
                    && !refName.Contains("Relationship", StringComparison.OrdinalIgnoreCase)
                    && !refName.EndsWith("Ref",          StringComparison.OrdinalIgnoreCase))
                {
                    return prop.Name;
                }
            }
            return null;
        }
        
        private static string? FindCharacteristicValueKey(JsonElement definition)
        {
            if (!definition.TryGetProperty("properties", out var props)) return null;

            foreach (var prop in props.EnumerateObject())
            {
                if (!IsArrayWithRef(prop.Value, out var refName)) continue;

                if (refName.Contains("CharacteristicValue", StringComparison.OrdinalIgnoreCase)
                    || refName.Contains("SpecCharacteristicValue", StringComparison.OrdinalIgnoreCase))
                {
                    return prop.Name;
                }
            }
            return null;
        }

        private static string? FindRefedTypeName(JsonElement definition, string propertyKey)
        {
            if (!definition.TryGetProperty("properties", out var props)) return null;
            if (!props.TryGetProperty(propertyKey, out var prop)) return null;
            if (!prop.TryGetProperty("items", out var items)) return null;
            if (!items.TryGetProperty("$ref", out var refVal)) return null;
            return refVal.GetString()?.Split('/').Last();
        }

        private static bool IsArrayWithRef(JsonElement prop, out string refName)
        {
            refName = string.Empty;
            if (!prop.TryGetProperty("type", out var type) || type.GetString() != "array") return false;
            if (!prop.TryGetProperty("items", out var items)) return false;
            if (!items.TryGetProperty("$ref", out var refVal)) return false;
            refName = refVal.GetString()?.Split('/').Last() ?? string.Empty;
            return refName.Length > 0;
        }

        private static void RewriteRefs(JsonNode node, string from, string to)
        {
            if (node is JsonObject obj)
            {
                foreach (var key in obj.Select(p => p.Key).ToList())
                {
                    if (key == "$ref"
                        && obj[key] is JsonValue val
                        && val.TryGetValue<string>(out var str)
                        && str.StartsWith(from, StringComparison.Ordinal))
                    {
                        obj[key] = to + str[from.Length..];
                    }
                    else if (obj[key] is JsonNode child)
                    {
                        RewriteRefs(child, from, to);
                    }
                }
            }
            else if (node is JsonArray arr)
            {
                foreach (var item in arr)
                    if (item is not null) RewriteRefs(item, from, to);
            }
        }

        private static void ApplyAdditionalPropertiesFalse(JsonObject definitions, string resourceType)
        {
            foreach (var (key, value) in definitions)
            {
                if (value is not JsonObject defObj) continue;

                var isTarget = key.Equals(resourceType, StringComparison.OrdinalIgnoreCase)
                            || key.Contains("Characteristic", StringComparison.OrdinalIgnoreCase);

                if (!isTarget) continue;

                var hasObjectType = defObj["type"]?.GetValue<string>() == "object"
                                    || !defObj.ContainsKey("type");
                if (!hasObjectType) continue;
                if (!defObj.ContainsKey("properties")) continue;
                if (defObj.ContainsKey("additionalProperties")) continue;
                if (defObj.ContainsKey("allOf") || defObj.ContainsKey("anyOf")) continue;

                defObj["additionalProperties"] = false;
            }
        }

        private static string NormaliseVersion(string raw)
        {
            raw = raw.TrimStart('v', 'V');
            var parts = raw.Split('.');
            return parts.Length >= 2 ? $"{parts[0]}.{parts[1]}" : parts[0];
        }

        private static string ToCamelCase(string s) =>
            string.IsNullOrEmpty(s) ? s : char.ToLowerInvariant(s[0]) + s[1..];
    }
}