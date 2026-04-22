namespace DevProject.Business.Getters
{
    using Data.Entities;
    using Data.Enums;
    using Interfaces;
    using System.Text.Json.Nodes;    public class FieldMappedMappingGetter : IResourceMappingGetter
    {
        public bool Supports(MappingStrategy strategy) => strategy == MappingStrategy.FieldMapped;

        public List<JsonNode> GetAll(ParsedExcelData data, TmForumApiDefinition apiDef, JsonConversionConfig config)
        {
            return data.Rows
                .Where(row => row.Values.Any(cell => !cell.IsEmpty))
                .Select((row, index) => BuildResource(row, apiDef, config, index))
                .ToList();
        }

        private static JsonNode BuildResource(
            Dictionary<string, CellValue> row,
            TmForumApiDefinition apiDef,
            JsonConversionConfig config,
            int index)
        {
            var id            = Guid.NewGuid().ToString();
            var name          = CellValueJsonHelper.ResolveResourceName(row, config, index);
            var fieldMappings = config.FieldMappings ?? new Dictionary<string, string>();
            var type          = config.ResourceTypeOverride ?? apiDef.ResourceType;

            var resource = new JsonObject
            {
                ["id"]    = id,
                ["href"]  = $"{apiDef.BasePath}/{apiDef.ResourceCollectionPath}/{id}",
                ["@type"] = type,
                ["name"]  = name
            };

            var charArray = new JsonArray();
            foreach (var (col, cell) in row)
            {
                if (cell.IsEmpty) continue;
                if (CellValueJsonHelper.IsMetadataColumn(col, config)) continue;

                if (fieldMappings.TryGetValue(col, out var fieldName))
                {
                    resource[fieldName] = CellValueJsonHelper.ToJsonNode(cell);
                }
                else
                {
                    charArray.Add(new JsonObject
                    {
                        ["name"]      = col,
                        ["value"]     = CellValueJsonHelper.ToJsonNode(cell),
                        ["valueType"] = CellValueJsonHelper.MapValueType(cell.Type)
                    });
                }
            }

            if (charArray.Count > 0)
                resource[apiDef.CharacteristicArrayKey] = charArray;

            return resource;
        }
    }
}
