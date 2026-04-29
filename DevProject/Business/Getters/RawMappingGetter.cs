namespace DevProject.Business.Getters
{
    using Data.Entities;
    using Data.Enums;
    using Helpers;
    using Interfaces;
    using System.Text.Json.Nodes;
    public class RawMappingGetter : IResourceMappingGetter
    {
        public bool Supports(MappingStrategy strategy) => strategy == MappingStrategy.Raw;

        public List<JsonNode> GetAll(ParsedExcelData data, TmForumApiDefinition apiDef, JsonConversionConfig config)
        {
            return data.Rows
                .Where(row => row.Values.Any(cell => !cell.IsEmpty))
                .Select(row => BuildFlatObject(row))
                .ToList();
        }

        private static JsonNode BuildFlatObject(Dictionary<string, CellValue> row)
        {
            var obj = new JsonObject();
            foreach (var (col, cell) in row)
            {
                if (cell.IsEmpty)
                    continue;

                obj[col] = CellValueJsonHelper.ToJsonNode(cell);
            }
            return obj;
        }
    }
}
