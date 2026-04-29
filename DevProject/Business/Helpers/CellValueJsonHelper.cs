namespace DevProject.Business.Helpers
{
    using System.Globalization;
    using System.Text.Json.Nodes;
    using System.Text.RegularExpressions;
    using Data.Entities;
    using Data.Enums;
    internal static class CellValueJsonHelper
    { private static readonly Regex UnitPattern = new(
            @"^\s*(\d+(?:\.\d+)?)\s+([A-Za-z][A-Za-z/Â°%]*)\s*$",
            RegexOptions.Compiled);

        internal static JsonNode? ToJsonNode(CellValue cell) => cell switch
        {
            NumberCellValue n => JsonValue.Create(n.Value),
            BooleanCellValue b => JsonValue.Create(b.Value),
            DateTimeCellValue dt => JsonValue.Create(dt.Value.ToString("o")),
            TextCellValue t => JsonValue.Create(t.Value),
            _ => null
        }; internal static JsonNode? TryParseJson(CellValue cell)
        {
            if (cell is not TextCellValue t) return null;
            if (string.IsNullOrWhiteSpace(t.Value)) return null;
            try
            {
                return JsonNode.Parse(t.Value);
            }
            catch
            {
                return null;
            }
        }

        internal static string MapValueType(CellValueType type) => type switch
        {
            CellValueType.Number => "number",
            CellValueType.Boolean => "boolean",
            CellValueType.DateTime => "dateTime",
            _ => "string"
        }; internal static string ResolveResourceName(
            Dictionary<string, CellValue> row,
            JsonConversionConfig config,
            int index,
            string prefix = "Resource")
        {
            if (config.NameColumn is not null
                && row.TryGetValue(config.NameColumn, out var namedCell)
                && !namedCell.IsEmpty)
                return namedCell.AsString();

            foreach (var cell in row.Values)
            {
                if (!cell.IsEmpty)
                    return cell.AsString();
            }

            return $"{prefix}_{index + 1}";
        }
        internal static bool IsMetadataColumn(string column, JsonConversionConfig config)
        {
            if (config.NameColumn is not null && config.NameColumn == column) return true;
            if (config.VersionColumn is not null && config.VersionColumn == column) return true;
            if (config.DescriptionColumn is not null && config.DescriptionColumn == column) return true;
            return false;
        }
        internal static bool IsExcludedFromCharacteristics(string column, JsonConversionConfig config)
        {
            if (IsMetadataColumn(column, config)) return true;
            if (config.LifecycleStatusColumn is not null && config.LifecycleStatusColumn == column) return true;
            if (config.FieldMappings is not null && config.FieldMappings.ContainsKey(column)) return true;
            return false;
        }
        internal static (JsonNode? jsonValue, string valueType, string? unitOfMeasure) ParseCellValue(CellValue cell)
        {
            if (cell is TextCellValue textCell)
            {
                var match = UnitPattern.Match(textCell.Value);
                if (match.Success
                    && double.TryParse(match.Groups[1].Value, NumberStyles.Number, CultureInfo.InvariantCulture, out var num))
                {
                    return (JsonValue.Create(num), "number", match.Groups[2].Value);
                }
            }

            return (ToJsonNode(cell), MapValueType(cell.Type), null);
        }
    }
}
