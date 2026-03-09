namespace DevProject.Data.Entities
{
    public class ParsedExcelData
    {
         public string SpreadsheetName { get; set; }
         public List<string> ColumnNames { get; set; }
         public List<Dictionary<string, CellValue>> Rows { get; set; }
         public int? TotalRows { get; set; }
    }
}