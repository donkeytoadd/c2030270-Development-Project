namespace DevProject.Resources.Responses
{
    using Data.Entities;

    public class FileUploadResponse
    {
        public required string FileId { get; set; }
        public required string SheetName { get; set; }
        public required List<String> ColumnNames { get; set; }
        public required List<Dictionary<string, CellValue>> Rows { get; set; }
        public int? TotalRows { get; set; }
    }
}