namespace DevProject.Business.Getters
{
    using Data.Entities;
    using Data.Enums;
    using Helpers;
    using Interfaces;
    using System.Text.Json.Nodes; public class InventoryMappingGetter : IResourceMappingGetter
    {
        public bool Supports(MappingStrategy strategy) => strategy == MappingStrategy.Inventory;

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
            var id = Guid.NewGuid().ToString();
            var name = CellValueJsonHelper.ResolveResourceName(row, config, index);
            var type = config.ResourceTypeOverride ?? apiDef.ResourceType;

            var resource = new JsonObject
            {
                ["id"] = id,
                ["href"] = $"{apiDef.BasePath}/{apiDef.ResourceCollectionPath}/{id}",
                ["@type"] = type,
                ["name"] = name
            };

            if (config.LifecycleStatusColumn is not null)
            {
                var status = ResolveColumnValue(row, config.LifecycleStatusColumn);
                if (status is not null)
                    resource["lifecycleStatus"] = status;
            }

            if (config.FieldMappings is not null)
            {
                foreach (var (col, fieldName) in config.FieldMappings)
                {
                    if (!row.TryGetValue(col, out var mappedCell)) continue;
                    if (mappedCell.IsEmpty) continue;
                    resource[fieldName] = CellValueJsonHelper.ToJsonNode(mappedCell);
                }
            }

            var charArray = new JsonArray();
            foreach (var (col, cell) in row)
            {
                if (cell.IsEmpty) continue;
                if (CellValueJsonHelper.IsExcludedFromCharacteristics(col, config)) continue;

                var (jsonValue, valueType, unitOfMeasure) = CellValueJsonHelper.ParseCellValue(cell);

                var charObj = new JsonObject
                {
                    ["name"] = col,
                    ["value"] = jsonValue,
                    ["valueType"] = valueType
                };
                if (unitOfMeasure is not null)
                    charObj["unitOfMeasure"] = unitOfMeasure;

                charArray.Add(charObj);
            }

            if (charArray.Count > 0)
                resource[apiDef.CharacteristicArrayKey] = charArray;

            return resource;
        }

        private static string? ResolveColumnValue(Dictionary<string, CellValue> row, string columnName)
        {
            if (!row.TryGetValue(columnName, out var cell)) return null;
            if (cell.IsEmpty) return null;
            return cell.AsString();
        }
    }
}
