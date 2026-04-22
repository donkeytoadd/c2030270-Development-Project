namespace DevProject.Business.Processors
{
    using System.Text.Json.Nodes;
    using Data.Entities;
    using Data.Enums;
    using Getters;
    using Interfaces;
    public class WorkbookConversionProcessor : IWorkbookConversionProcessor
    {
        public WorkbookConversionResult Convert(ParsedWorkbook workbook, MappingTemplate template)
        {
            var id = Guid.NewGuid().ToString();

            var root = new JsonObject
            {
                ["id"]    = id,
                ["href"]  = $"{template.BasePath}/{template.ResourceCollectionPath}/{id}",
                ["@type"] = template.RootResourceType,
            };

            if (template.EmitLastUpdate)
                root["lastUpdate"] = DateTime.UtcNow.ToString("o");

            if (template.RootBaseType is not null)
                root["@baseType"] = template.RootBaseType;

            var warnings = new List<string>();

            foreach (var sheetMapping in template.SheetMappings)
            {
                var sheet = workbook.Sheets.FirstOrDefault(s =>
                    s.SpreadsheetName.Equals(sheetMapping.SheetName, StringComparison.OrdinalIgnoreCase));

                if (sheet is null)
                {
                    warnings.Add($"Sheet '{sheetMapping.SheetName}' not found in workbook — mapping skipped.");
                    continue;
                }

                switch (sheetMapping.Pattern)
                {
                    case SheetMappingPattern.TopLevelFields:
                        ApplyTopLevelFields(root, sheet, sheetMapping, warnings);
                        break;

                    case SheetMappingPattern.FlatArray:
                        root[sheetMapping.TargetKey] = BuildFlatArray(sheet, sheetMapping);
                        break;

                    case SheetMappingPattern.NestedArray:
                        root[sheetMapping.TargetKey] = BuildNestedArray(sheet, sheetMapping);
                        break;
                }
            }

            return new WorkbookConversionResult
            {
                WorkbookName = workbook.WorkbookName,
                ApiId        = template.ApiId,
                ApiName      = template.ApiName,
                ResourceType = template.RootResourceType,
                Output       = root,
                Warnings     = warnings
            };
        }

        private static void ApplyTopLevelFields(
            JsonObject root,
            ParsedExcelData sheet,
            SheetMapping sheetMapping,
            List<string> warnings)
        {
            var firstRow = sheet.Rows.FirstOrDefault(r => r.Values.Any(c => !c.IsEmpty));

            if (firstRow is null)
            {
                warnings.Add($"Sheet '{sheetMapping.SheetName}' contains no data rows — no top-level fields written.");
                return;
            }

            foreach (var mapping in sheetMapping.ColumnMappings)
            {
                if (!firstRow.TryGetValue(mapping.SourceColumn, out var cell)) continue;
                if (cell.IsEmpty) continue;
                var value = ResolveValue(cell, mapping.ParseJson);
                if (value is null) continue;
                SetNestedProperty(root, mapping.TargetField, value);
            }
        }

        private static JsonArray BuildFlatArray(ParsedExcelData sheet, SheetMapping sheetMapping)
        {
            var array = new JsonArray();

            foreach (var row in sheet.Rows)
            {
                if (row.Values.All(c => c.IsEmpty)) continue;

                var obj = BuildObjectFromMappings(row, sheetMapping.ColumnMappings);
                if (obj.Count > 0)
                    array.Add(obj);
            }

            return array;
        }

        private static JsonArray BuildNestedArray(ParsedExcelData sheet, SheetMapping sheetMapping)
        {            if (sheetMapping.GroupByColumn is not null)
                return BuildGroupedNestedArray(sheet, sheetMapping);
            if (sheetMapping.ParentIdColumn is not null
                && sheetMapping.ChildParentRefColumn is not null
                && sheetMapping.ChildArrayKey is not null)
                return BuildParentChildArray(sheet, sheetMapping);
            return BuildFlatArray(sheet, sheetMapping);
        }

        private static JsonArray BuildGroupedNestedArray(ParsedExcelData sheet, SheetMapping sheetMapping)
        {
            var groupByCol  = sheetMapping.GroupByColumn!;
            var subArrayDefs = sheetMapping.SubArrayDefinitions;            var subArrayColSets = subArrayDefs
                .Select(d => d.SubArrayColumns.ToHashSet(StringComparer.OrdinalIgnoreCase))
                .ToList();

            var allSubArrayCols = subArrayDefs
                .SelectMany(d => d.SubArrayColumns)
                .ToHashSet(StringComparer.OrdinalIgnoreCase);
            var groups     = new Dictionary<string, JsonObject>(StringComparer.Ordinal);
            var subArrays  = new Dictionary<string, List<JsonArray>>(StringComparer.Ordinal);
            var orderedKeys = new List<string>();

            foreach (var row in sheet.Rows)
            {
                if (row.Values.All(c => c.IsEmpty)) continue;

                if (!row.TryGetValue(groupByCol, out var groupKeyCell) || groupKeyCell.IsEmpty)
                    continue;

                var groupKey = groupKeyCell.AsString();

                if (!groups.TryGetValue(groupKey, out var parent))
                {
                    parent = new JsonObject();
                    groups[groupKey]    = parent;
                    orderedKeys.Add(groupKey);
                    var arrays = subArrayDefs.Select(_ => new JsonArray()).ToList();
                    subArrays[groupKey] = arrays;
                }
                foreach (var mapping in sheetMapping.ColumnMappings)
                {
                    if (allSubArrayCols.Contains(mapping.SourceColumn)) continue;
                    if (!row.TryGetValue(mapping.SourceColumn, out var cell)) continue;
                    if (cell.IsEmpty) continue;                    if (!ContainsNestedProperty(parent, mapping.TargetField))
                    {
                        var value = ResolveValue(cell, mapping.ParseJson);
                        if (value is not null)
                            SetNestedProperty(parent, mapping.TargetField, value);
                    }
                }
                var rowSubArrays = subArrays[groupKey];
                for (var i = 0; i < subArrayDefs.Count; i++)
                {
                    var def    = subArrayDefs[i];
                    var colSet = subArrayColSets[i];
                    var entry  = new JsonObject();

                    foreach (var mapping in sheetMapping.ColumnMappings)
                    {
                        if (!colSet.Contains(mapping.SourceColumn)) continue;
                        if (!row.TryGetValue(mapping.SourceColumn, out var cell)) continue;
                        if (cell.IsEmpty) continue;
                        var value = ResolveValue(cell, mapping.ParseJson);
                        if (value is null) continue;
                        SetNestedProperty(entry, mapping.TargetField, value);
                    }

                    if (entry.Count > 0)
                        rowSubArrays[i].Add(entry);
                }
            }
            var result = new JsonArray();
            foreach (var key in orderedKeys)
            {
                var parent      = groups[key];
                var rowSubArrays = subArrays[key];

                for (var i = 0; i < subArrayDefs.Count; i++)
                    parent[subArrayDefs[i].SubArrayKey] = rowSubArrays[i];

                result.Add(parent);
            }

            return result;
        }

        private static JsonArray BuildParentChildArray(ParsedExcelData sheet, SheetMapping sheetMapping)
        {
            var parentIdCol   = sheetMapping.ParentIdColumn!;
            var childRefCol   = sheetMapping.ChildParentRefColumn!;
            var childArrayKey = sheetMapping.ChildArrayKey!;

            var parents      = new Dictionary<string, JsonObject>(StringComparer.Ordinal);
            var parentOrder  = new List<string>();
            var pendingChildren = new List<(string parentRef, JsonObject child)>();

            foreach (var row in sheet.Rows)
            {
                if (row.Values.All(c => c.IsEmpty)) continue;

                var obj = BuildObjectFromMappings(row, sheetMapping.ColumnMappings);

                var hasParentRef = row.TryGetValue(childRefCol, out var refCell) && !refCell.IsEmpty;
                var hasOwnId     = row.TryGetValue(parentIdCol, out var idCell)  && !idCell.IsEmpty;

                if (hasParentRef)
                {
                    pendingChildren.Add((refCell!.AsString(), obj));
                }
                else if (hasOwnId)
                {
                    var ownId = idCell!.AsString();
                    parents[ownId] = obj;
                    parentOrder.Add(ownId);
                }
            }

            foreach (var (parentRef, child) in pendingChildren)
            {
                if (!parents.TryGetValue(parentRef, out var parent)) continue;

                if (!parent.TryGetPropertyValue(childArrayKey, out var existingNode)
                    || existingNode is not JsonArray arr)
                {
                    arr = new JsonArray();
                    parent[childArrayKey] = arr;
                }

                arr.Add(child);
            }

            var result = new JsonArray();
            foreach (var key in parentOrder)
                result.Add(parents[key]);
            return result;
        }

        private static JsonObject BuildObjectFromMappings(
            Dictionary<string, CellValue> row,
            List<ColumnMapping> mappings)
        {
            var obj = new JsonObject();
            foreach (var mapping in mappings)
            {                if (string.IsNullOrEmpty(mapping.SourceColumn))
                {
                    if (mapping.DefaultValue is not null && !ContainsNestedProperty(obj, mapping.TargetField))
                        SetNestedProperty(obj, mapping.TargetField, JsonValue.Create(mapping.DefaultValue));
                    continue;
                }

                if (!row.TryGetValue(mapping.SourceColumn, out var cell)) continue;
                if (cell.IsEmpty) continue;
                if (mapping.SplitToArray is { } split)
                {
                    var parts = cell.AsString().Split(split.Delimiter, StringSplitOptions.RemoveEmptyEntries);
                    if (parts.Length == 0) continue;
                    var arr = new JsonArray();
                    foreach (var part in parts)
                    {
                        var item = new JsonObject { [split.ItemField] = JsonValue.Create(part.Trim()) };
                        foreach (var (k, v) in split.ConstantFields)
                            item[k] = JsonValue.Create(v);
                        arr.Add(item);
                    }
                    SetNestedProperty(obj, mapping.TargetField, arr);
                    continue;
                }

                var value = ResolveValue(cell, mapping.ParseJson);
                if (value is null) continue;
                SetNestedProperty(obj, mapping.TargetField, value);
            }
            return obj;
        }
        private static JsonNode? ResolveValue(CellValue cell, bool parseJson) =>
            parseJson
                ? CellValueJsonHelper.TryParseJson(cell)
                : CellValueJsonHelper.ToJsonNode(cell);
        private static void SetNestedProperty(JsonObject obj, string path, JsonNode? value)
        {
            if (value is null) return;

            var dotIndex = path.IndexOf('.');
            if (dotIndex < 0)
            {
                obj[path] = value;
                return;
            }

            var key  = path[..dotIndex];
            var rest = path[(dotIndex + 1)..];

            if (!obj.TryGetPropertyValue(key, out var existing) || existing is not JsonObject nested)
            {
                nested   = new JsonObject();
                obj[key] = nested;
            }

            SetNestedProperty(nested, rest, value);
        }        private static bool ContainsNestedProperty(JsonObject obj, string path)
        {
            var dotIndex = path.IndexOf('.');
            if (dotIndex < 0)
                return obj.ContainsKey(path);

            var key = path[..dotIndex];
            if (!obj.TryGetPropertyValue(key, out var child) || child is not JsonObject nested)
                return false;

            return ContainsNestedProperty(nested, path[(dotIndex + 1)..]);
        }
    }
}
