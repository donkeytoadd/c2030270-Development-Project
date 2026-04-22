namespace DevProject.Data.Entities
{
    public class JsonConversionConfig
    {
        public string ApiId { get; set; } = "TMF634";
        public string? SheetName { get; set; }
        public string? NameColumn { get; set; }
        public string? VersionColumn { get; set; }
        public string? DescriptionColumn { get; set; }
        public string? GroupByColumn { get; set; }        public Dictionary<string, string>? FieldMappings { get; set; }
        public string? LifecycleStatusColumn { get; set; }
        public string? ResourceTypeOverride { get; set; }
    }
}