namespace DevProject.Data.Entities
{
    public class ParsedExcelData
    {
         public required string SpreadsheetName { get; set; }
         public required List<string> ColumnNames { get; set; }
         public required List<Dictionary<string, CellValue>> Rows { get; set; }
         public int? TotalRows { get; set; }
    }
}