namespace DevProject.Controllers
{
    using Business.Processors.Interfaces;
    using Business.Storage.Interfaces;
    using Data.Exceptions;
    using Microsoft.AspNetCore.Mvc;
    using Resources.Requests;
    using Resources.Responses;

    [ApiController]
    [Route("api/[controller]")]
    public class FileUploadController : ControllerBase
    {
        private readonly ILogger<FileUploadController> logger;
        private readonly ISpreadsheetProcessor spreadsheetProcessor;
        private readonly IFileStorage fileStorage;

        private readonly string[] allowedExtensions = { ".xlsx", ".xls" };
        private readonly long maxFileSize = 10 * 1024 * 1024;

        public FileUploadController(
            ILogger<FileUploadController> logger,
            ISpreadsheetProcessor spreadsheetProcessor,
            IFileStorage fileStorage)
        {
            this.logger             = logger;
            this.spreadsheetProcessor = spreadsheetProcessor;
            this.fileStorage        = fileStorage;
        }

        [HttpPost("UploadFile")]
        public async Task<IActionResult> UploadFile(
            FileUploadRequest fileUploadRequest,
            CancellationToken ct = default)
        {
            try
            {
                if (fileUploadRequest.File.Length == 0)
                    return BadRequest("No File Selected");

                var fileExtension = Path.GetExtension(fileUploadRequest.File.FileName).ToLower();

                if (!allowedExtensions.Contains(fileExtension))
                    return BadRequest($"Only {string.Join(",", allowedExtensions)} file extensions are allowed");

                if (fileUploadRequest.File.Length > maxFileSize)
                    return BadRequest($"File size cannot be larger than {maxFileSize} MB");                using var memoryStream = new MemoryStream();
                await fileUploadRequest.File.CopyToAsync(memoryStream, ct);
                memoryStream.Position = 0;

                var fileId = await fileStorage.SaveAsync(memoryStream, fileExtension, ct);
                memoryStream.Position = 0;

                try
                {
                    var workbook = spreadsheetProcessor.Process(memoryStream, fileUploadRequest.File.FileName);

                    logger.LogInformation(
                        "Parsed '{FileName}': {TotalSheets} sheet(s).",
                        fileUploadRequest.File.FileName, workbook.TotalSheets);

                    var response = new FileUploadResponse
                    {
                        FileId       = fileId,
                        WorkbookName = workbook.WorkbookName,
                        Sheets       = workbook.Sheets.Select(s => new ParsedSheetResponse
                        {
                            SheetName   = s.SpreadsheetName,
                            ColumnNames = s.ColumnNames,
                            TotalRows   = s.TotalRows ?? 0
                        }).ToList()
                    };

                    return Ok(response);
                }
                catch (ProcessExcelException ex)
                {
                    logger.LogWarning("Parse failed for {FileName}: {Message}", fileUploadRequest.File.FileName, ex.Message);
                    return UnprocessableEntity(new { message = ex.Message });
                }
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }
    }
}