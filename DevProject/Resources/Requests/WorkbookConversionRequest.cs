namespace DevProject.Resources.Requests
{
    using Data.Entities;

    public class WorkbookConversionRequest
    {
        public required string FileId { get; set; }        public string? ApiId { get; set; }
        public MappingTemplate? Template { get; set; }
    }
}
