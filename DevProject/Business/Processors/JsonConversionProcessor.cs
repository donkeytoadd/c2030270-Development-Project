namespace DevProject.Business.Processors
{
    using Data.Entities;
    using Getters.Interfaces;
    using Interfaces;

    public class JsonConversionProcessor : IJsonConversionProcessor
    {
        private readonly ITmForumResourceGetter tmForumResourceGetter;
        private readonly ITmForumApiCatalogGetter tmForumApiCatalogGetter;
        private readonly ITmForumSchemaValidationProcessor tmForumSchemaValidationProcessor;
        private readonly IJsonConversionRowValidationProcessor jsonConversionRowValidationProcessor;

        public JsonConversionProcessor(
            ITmForumResourceGetter tmForumResourceGetter,
            ITmForumApiCatalogGetter tmForumApiCatalogGetter,
            ITmForumSchemaValidationProcessor tmForumSchemaValidationProcessor,
            IJsonConversionRowValidationProcessor jsonConversionRowValidationProcessor)
        {
            this.tmForumResourceGetter = tmForumResourceGetter;
            this.tmForumApiCatalogGetter = tmForumApiCatalogGetter;
            this.tmForumSchemaValidationProcessor = tmForumSchemaValidationProcessor;
            this.jsonConversionRowValidationProcessor = jsonConversionRowValidationProcessor;
        }

        public JsonConversionResult Convert(ParsedExcelData parsedData, JsonConversionConfig config)
        {
            var apiDef = this.tmForumApiCatalogGetter.GetById(config.ApiId);

            var warnings   = this.jsonConversionRowValidationProcessor.Validate(parsedData.Rows, config);
            var resources  = this.tmForumResourceGetter.GetAll(parsedData, apiDef, config);
            var validation = this.tmForumSchemaValidationProcessor.Validate(resources, apiDef.ApiId);

            return new JsonConversionResult
            {
                SheetName      = parsedData.SpreadsheetName,
                ApiId          = apiDef.ApiId,
                ApiName        = apiDef.Name,
                ResourceType   = apiDef.ResourceType,
                TotalResources = resources.Count,
                Resources      = resources,
                Validation     = validation,
                Warnings       = warnings
            };
        }
    }
}