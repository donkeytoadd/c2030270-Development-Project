namespace DevProject.Data.Entities
{
    public class ParsedWorkbook
    {
        public required string WorkbookName { get; init; }
        public required List<ParsedExcelData> Sheets { get; init; }
        public int TotalSheets => Sheets.Count;
    }
}