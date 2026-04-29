namespace DevProject.Business.Processors
{
    using Data.Entities;
    using Data.Enums;
    using Interfaces; public class JsonConversionRowValidationProcessor : IJsonConversionRowValidationProcessor
    {
        public List<string> Validate(List<Dictionary<string, CellValue>> rows, JsonConversionConfig config)
        {
            var warnings = new List<string>();

            for (var i = 0; i < rows.Count; i++)
            {
                var row = rows[i];
                var rowNum = i + 1;

                var hasData = row.Values.Any(cell => !cell.IsEmpty);
                if (!hasData)
                {
                    warnings.Add($"Row {rowNum} is completely empty and will be skipped.");
                    continue;
                }

                if (config.NameColumn is not null)
                {
                    var hasName = row.TryGetValue(config.NameColumn, out var nameCell)
                        && !nameCell.IsEmpty;

                    if (!hasName)
                        warnings.Add(
                            $"Row {rowNum}: name column '{config.NameColumn}' is missing or empty â€” a fallback name will be used.");
                }
            }

            return warnings;
        }
    }
}
