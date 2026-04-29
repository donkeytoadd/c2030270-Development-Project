namespace DevProject.Business.Getters
{
    using System.Text.Json.Nodes;
    using Data.Entities;
    using Data.Enums;
    using Helpers;
    using Interfaces;    public class SpecificationMappingGetter : IResourceMappingGetter
    {
        public bool Supports(MappingStrategy strategy) => strategy == MappingStrategy.Specification;

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
            var id              = Guid.NewGuid().ToString();
            var name            = CellValueJsonHelper.ResolveResourceName(row, config, index);
            var version         = ResolveMetadataValue(row, config.VersionColumn) ?? "1.0";
            var baseType        = apiDef.BaseType ?? "EntitySpecification";
            var type            = config.ResourceTypeOverride ?? apiDef.ResourceType;
            var lifecycleStatus = config.LifecycleStatusColumn is not null
                ? ResolveMetadataValue(row, config.LifecycleStatusColumn) ?? "Active"
                : "Active";

            var resource = new JsonObject
            {
                ["id"]              = id,
                ["href"]            = $"{apiDef.BasePath}/{apiDef.ResourceCollectionPath}/{id}",
                ["@type"]           = type,
                ["@baseType"]       = baseType,
                ["name"]            = name,
                ["version"]         = version,
                ["lifecycleStatus"] = lifecycleStatus,
                ["isBundle"]        = false,
                ["lastUpdate"]      = DateTime.UtcNow.ToString("o")
            };

            var description = ResolveMetadataValue(row, config.DescriptionColumn);
            if (description is not null)
                resource["description"] = description;

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
                charArray.Add(BuildCharacteristic(col, cell, apiDef.CharacteristicValueArrayKey));
            }

            resource[apiDef.CharacteristicArrayKey] = charArray;
            return resource;
        }

        private static JsonObject BuildCharacteristic(string col, CellValue cell, string charValueKey)
        {
            var (jsonValue, valueType, unitOfMeasure) = CellValueJsonHelper.ParseCellValue(cell);

            var charValue = new JsonObject
            {
                ["value"]     = jsonValue,
                ["valueType"] = valueType,
                ["isDefault"] = true
            };

            if (unitOfMeasure is not null)
                charValue["unitOfMeasure"] = unitOfMeasure;

            return new JsonObject
            {
                ["name"]           = col,
                ["valueType"]      = valueType,
                ["minCardinality"] = 0,
                ["maxCardinality"] = 1,
                ["isUnique"]       = false,
                [charValueKey]     = new JsonArray { charValue }
            };
        }

        private static string? ResolveMetadataValue(Dictionary<string, CellValue> row, string? columnName)
        {
            if (columnName is null) return null;
            if (!row.TryGetValue(columnName, out var cell)) return null;
            if (cell.IsEmpty) return null;
            return cell.AsString();
        }
    }
}
