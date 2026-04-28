namespace DevProject.Controllers
{
    using Business.Getters.Interfaces;
    using Business.Processors.Interfaces;
    using Business.Storage.Interfaces;
    using Data.Entities;
    using Data.Exceptions;
    using DevProject.Infrastructure;
    using Microsoft.AspNetCore.Mvc;
    using Resources.Requests;
    using Resources.Responses;

    [ApiController]
    [Route("api/[controller]")]
    public class JsonConversionController : ControllerBase
    {
        private readonly ILogger<JsonConversionController> logger;
        private readonly ISpreadsheetProcessor spreadsheetProcessor;
        private readonly IJsonConversionProcessor jsonConversionProcessor;
        private readonly IWorkbookConversionProcessor workbookConversionProcessor;
        private readonly ITmForumSchemaValidationProcessor schemaValidationProcessor;
        private readonly ITmForumApiCatalogGetter tmForumApiCatalogGetter;
        private readonly ITmForumApiDetectorGetter tmForumApiDetectorGetter;
        private readonly IMappingTemplateGetter mappingTemplateGetter;
        private readonly IConversionResultStore conversionResultStore;
        private readonly IFileStorage fileStorage;

        public JsonConversionController(
            ILogger<JsonConversionController> logger,
            ISpreadsheetProcessor spreadsheetProcessor,
            IJsonConversionProcessor jsonConversionProcessor,
            IWorkbookConversionProcessor workbookConversionProcessor,
            ITmForumSchemaValidationProcessor schemaValidationProcessor,
            ITmForumApiCatalogGetter tmForumApiCatalogGetter,
            ITmForumApiDetectorGetter tmForumApiDetectorGetter,
            IMappingTemplateGetter mappingTemplateGetter,
            IConversionResultStore conversionResultStore,
            IFileStorage fileStorage)
        {
            this.logger                     = logger;
            this.spreadsheetProcessor       = spreadsheetProcessor;
            this.jsonConversionProcessor    = jsonConversionProcessor;
            this.workbookConversionProcessor = workbookConversionProcessor;
            this.schemaValidationProcessor  = schemaValidationProcessor;
            this.tmForumApiCatalogGetter    = tmForumApiCatalogGetter;
            this.tmForumApiDetectorGetter   = tmForumApiDetectorGetter;
            this.mappingTemplateGetter      = mappingTemplateGetter;
            this.conversionResultStore      = conversionResultStore;
            this.fileStorage                = fileStorage;
        }

        [HttpPost("ConvertWorkbook")]
        public async Task<IActionResult> ConvertWorkbook(
            [FromBody] WorkbookConversionRequest request,
            CancellationToken ct = default)
        {
            try
            {
                if (!await fileStorage.ExistsAsync(request.FileId, ct))
                    return NotFound($"File '{request.FileId}' not found.");

                var template = request.Template
                    ?? (request.ApiId is not null
                        ? mappingTemplateGetter.GetById(request.ApiId)
                        : null);

                if (template is null)
                    return BadRequest("Provide either 'apiId' (to use a built-in template) or a full 'template' object.");

                await using var stream = await fileStorage.OpenReadAsync(request.FileId, ct);
                var workbook   = spreadsheetProcessor.Process(stream, request.FileId);
                var result     = workbookConversionProcessor.Convert(workbook, template);
                var validation = schemaValidationProcessor.Validate([result.Output], result.ApiId);

                var outputJson = result.Output.ToJsonString();
                var filename   = $"{result.WorkbookName}_{result.ApiId}.json";
                var resultId   = conversionResultStore.Store(filename, outputJson);

                logger.LogInformation(
                    "ConvertWorkbook '{FileId}' via {ApiId} ({ResourceType}): {Sheets} sheets, {Warnings} warnings, valid={Valid} ({Pct}%).",
                    request.FileId, result.ApiId, result.ResourceType,
                    workbook.TotalSheets, result.Warnings.Count,
                    validation.IsValid, validation.CompliancePercentage);

                return Ok(new WorkbookConversionResponse
                {
                    FileId       = request.FileId,
                    ResultId     = resultId,
                    WorkbookName = result.WorkbookName,
                    ApiId        = result.ApiId,
                    ApiName      = result.ApiName,
                    ResourceType = result.ResourceType,
                    Output       = result.Output,
                    Validation   = validation,
                    Warnings     = result.Warnings
                });
            }
            catch (ProcessExcelException ex)
            {
                logger.LogWarning("ConvertWorkbook failed for '{FileId}': {Message}", request.FileId, ex.Message);
                return UnprocessableEntity(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpGet("GetMappingTemplates")]
        public IActionResult GetMappingTemplates()
        {
            return Ok(mappingTemplateGetter.GetAll());
        }

        [HttpGet("Download/{resultId}")]
        public IActionResult Download(string resultId)
        {
            if (!conversionResultStore.TryGet(resultId, out var filename, out var json))
                return NotFound($"Result '{resultId}' not found or has expired.");

            var bytes = System.Text.Encoding.UTF8.GetBytes(json!);
            return File(bytes, "application/json", filename);
        }

        [HttpGet("ApiTypes")]
        public IActionResult GetApiTypes()
        {
            return Ok(tmForumApiCatalogGetter.GetAll());
        }

        [HttpGet("DetectApi/{fileId}")]
        public async Task<IActionResult> DetectApi(string fileId, CancellationToken ct = default)
        {
            if (!await fileStorage.ExistsAsync(fileId, ct))
                return NotFound($"File '{fileId}' not found.");

            try
            {
                await using var stream = await fileStorage.OpenReadAsync(fileId, ct);
                var workbook           = spreadsheetProcessor.Process(stream, fileId);
                var detection          = tmForumApiDetectorGetter.DetectApi(workbook);
                return Ok(detection);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpPost("Convert")]
        public async Task<IActionResult> Convert(
            [FromBody] JsonConversionRequest request,
            CancellationToken ct = default)
        {
            try
            {
                if (!await fileStorage.ExistsAsync(request.FileId, ct))
                    return NotFound($"File '{request.FileId}' not found.");

                var config = request.Config ?? new JsonConversionConfig();

                await using var stream = await fileStorage.OpenReadAsync(request.FileId, ct);
                var workbook = spreadsheetProcessor.Process(stream, request.FileId);

                var parsedData = config.SheetName is not null
                    ? workbook.Sheets.FirstOrDefault(s =>
                          s.SpreadsheetName.Equals(config.SheetName, StringComparison.OrdinalIgnoreCase))
                      ?? throw new ProcessExcelException(
                          $"Sheet '{config.SheetName}' was not found. Available sheets: {string.Join(", ", workbook.Sheets.Select(s => s.SpreadsheetName))}.")
                    : workbook.Sheets.First();

                var result = jsonConversionProcessor.Convert(parsedData, config);

                logger.LogInformation(
                    "Converted '{FileId}' via {ApiId} ({ResourceType}): {Count} resources.",
                    request.FileId, result.ApiId, result.ResourceType, result.TotalResources);

                return Ok(new JsonConversionResponse
                {
                    FileId         = request.FileId,
                    SheetName      = result.SheetName,
                    ApiId          = result.ApiId,
                    ApiName        = result.ApiName,
                    ResourceType   = result.ResourceType,
                    TotalResources = result.TotalResources,
                    Validation     = result.Validation,
                    Resources      = result.Resources,
                    Warnings       = result.Warnings
                });
            }
            catch (ProcessExcelException ex)
            {
                logger.LogWarning("Conversion failed for '{FileId}': {Message}", request.FileId, ex.Message);
                return UnprocessableEntity(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }
    }
}