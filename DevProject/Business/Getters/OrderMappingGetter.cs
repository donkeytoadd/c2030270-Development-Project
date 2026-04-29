namespace DevProject.Business.Getters
{
    using Data.Entities;
    using Data.Enums;
    using Helpers;
    using Interfaces;
    using System.Text.Json.Nodes;
    public class OrderMappingGetter : IResourceMappingGetter
    {
        public bool Supports(MappingStrategy strategy) => strategy == MappingStrategy.Order;

        public List<JsonNode> GetAll(ParsedExcelData data, TmForumApiDefinition apiDef, JsonConversionConfig config)
        {
            var nonEmptyRows = data.Rows
                .Where(row => row.Values.Any(cell => !cell.IsEmpty))
                .ToList();

            var mappings = NormaliseMappings(config.FieldMappings);

            if (string.IsNullOrWhiteSpace(config.GroupByColumn))
            {
                return nonEmptyRows
                    .Select((row, index) => BuildOrder(
                        externalId: CellValueJsonHelper.ResolveResourceName(row, config, index, "Order"),
                        rows:       new List<Dictionary<string, CellValue>> { row },
                        apiDef:     apiDef,
                        config:     config,
                        mappings:   mappings))
                    .ToList();
            }

            return nonEmptyRows
                .GroupBy(row =>
                    row.TryGetValue(config.GroupByColumn, out var cell) && !cell.IsEmpty
                        ? cell.AsString()
                        : string.Empty)
                .Select(group => BuildOrder(
                    externalId: group.Key,
                    rows:       group.ToList(),
                    apiDef:     apiDef,
                    config:     config,
                    mappings:   mappings))
                .Cast<JsonNode>()
                .ToList();
        }

        private static JsonNode BuildOrder(
            string externalId,
            List<Dictionary<string, CellValue>> rows,
            TmForumApiDefinition apiDef,
            JsonConversionConfig config,
            Dictionary<string, string> mappings)
        {
            var id   = Guid.NewGuid().ToString();
            var type = config.ResourceTypeOverride ?? apiDef.ResourceType;

            var order = new JsonObject
            {
                ["id"]         = id,
                ["href"]       = $"{apiDef.BasePath}/{apiDef.ResourceCollectionPath}/{id}",
                ["@type"]      = type,
                ["externalId"] = externalId
            };

            var firstRow = rows.FirstOrDefault();
            if (firstRow is not null)
            {
                foreach (var (col, target) in mappings)
                {
                    if (!target.StartsWith("order.", StringComparison.OrdinalIgnoreCase)) continue;
                    var fieldName = target["order.".Length..];
                    if (firstRow.TryGetValue(col, out var cell) && !cell.IsEmpty)
                        order[fieldName] = CellValueJsonHelper.ToJsonNode(cell);
                }
            }

            var itemType = char.ToUpperInvariant(apiDef.CharacteristicArrayKey[0])
                + apiDef.CharacteristicArrayKey[1..];

            var itemsArray = new JsonArray();
            for (var i = 0; i < rows.Count; i++)
                itemsArray.Add(BuildOrderItem(rows[i], apiDef, config, i, itemType, mappings));

            order[apiDef.CharacteristicArrayKey] = itemsArray;
            return order;
        }

        private static JsonObject BuildOrderItem(
            Dictionary<string, CellValue> row,
            TmForumApiDefinition apiDef,
            JsonConversionConfig config,
            int itemIndex,
            string itemType,
            Dictionary<string, string> mappings)
        {
            var action = "add";
            foreach (var (col, target) in mappings)
            {
                if (!string.Equals(target, "item.action", StringComparison.OrdinalIgnoreCase)) continue;
                if (row.TryGetValue(col, out var cell) && !cell.IsEmpty)
                {
                    action = cell.AsString().Trim().ToLowerInvariant();
                    break;
                }
            }

            var item = new JsonObject
            {
                ["id"]     = (itemIndex + 1).ToString(),
                ["@type"]  = itemType,
                ["action"] = action
            };

            foreach (var (col, target) in mappings)
            {
                if (!target.StartsWith("item.", StringComparison.OrdinalIgnoreCase)) continue;
                if (string.Equals(target, "item.action", StringComparison.OrdinalIgnoreCase)) continue;
                var fieldName = target["item.".Length..];
                if (row.TryGetValue(col, out var cell) && !cell.IsEmpty)
                    item[fieldName] = CellValueJsonHelper.ToJsonNode(cell);
            }

            var nestedObj = new JsonObject
            {
                ["@type"] = apiDef.OrderItemNestedObjectType
            };

            var nestedPrefix = $"{apiDef.OrderItemNestedObjectKey}.";
            foreach (var (col, target) in mappings)
            {
                if (!target.StartsWith(nestedPrefix, StringComparison.OrdinalIgnoreCase)) continue;
                var fieldName = target[nestedPrefix.Length..];
                if (row.TryGetValue(col, out var cell) && !cell.IsEmpty)
                    nestedObj[fieldName] = CellValueJsonHelper.ToJsonNode(cell);
            }

            var charArray = new JsonArray();
            foreach (var (col, cell) in row)
            {
                if (cell.IsEmpty) continue;
                if (col == config.GroupByColumn) continue;
                if (CellValueJsonHelper.IsMetadataColumn(col, config)) continue;

                if (mappings.TryGetValue(col, out var target) &&
                    !string.Equals(target, "characteristic", StringComparison.OrdinalIgnoreCase) &&
                    !string.IsNullOrWhiteSpace(target))
                    continue;

                charArray.Add(new JsonObject
                {
                    ["name"]      = col,
                    ["value"]     = CellValueJsonHelper.ToJsonNode(cell),
                    ["valueType"] = CellValueJsonHelper.MapValueType(cell.Type)
                });
            }

            if (charArray.Count > 0)
                nestedObj[apiDef.OrderItemCharacteristicKey] = charArray;

            item[apiDef.OrderItemNestedObjectKey] = nestedObj;
            return item;
        }

        private static Dictionary<string, string> NormaliseMappings(Dictionary<string, string>? raw) =>
            raw is null
                ? new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                : new Dictionary<string, string>(raw, StringComparer.OrdinalIgnoreCase);
    }
}
