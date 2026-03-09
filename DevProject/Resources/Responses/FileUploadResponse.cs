namespace DevProject.Resources.Responses
{
    using Data.Entities;

    public class FileUploadResponse
    {
        public string FileId { get; set; }
        public string SheetName { get; set; }
        public List<String> ColumnNames { get; set; }
        public List<Dictionary<string, CellValue>> Rows { get; set; }
        public int? TotalRows { get; set; }
    }
}