namespace DevProject.Resources.Requests
{
    using Data.Entities;

    public class JsonConversionRequest
    {
        public required string FileId { get; set; }
        public JsonConversionConfig? Config { get; set; }
    }
}
