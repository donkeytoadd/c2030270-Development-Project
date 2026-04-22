namespace DevProject.Resources.Responses
{
    public class FileUploadResponse
    {
        public required string FileId { get; set; }
        public required string WorkbookName { get; set; }
        public required List<ParsedSheetResponse> Sheets { get; set; }
        public int TotalSheets => Sheets.Count;
    }

    public class ParsedSheetResponse
    {
        public required string SheetName { get; set; }
        public required List<string> ColumnNames { get; set; }
        public int TotalRows { get; set; }
    }
}