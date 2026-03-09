namespace DevProject.Controllers
{
    using Business.Processors.Interfaces;
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
        
        private readonly string[] allowedExtensions = { ".xlsx", ".xls"};
        private readonly long maxFileSize = 10 * 1024 * 1024;
        
        public FileUploadController(
            ILogger<FileUploadController> logger,
            ISpreadsheetProcessor spreadsheetProcessor)
        {
            this.logger = logger;
            this.spreadsheetProcessor = spreadsheetProcessor;
        }

        [HttpPost("UploadFile")]
        public async Task<IActionResult> UploadFile(FileUploadRequest fileUploadRequest)
        {
            try
            {
                if (fileUploadRequest.File.Length == 0)
                {
                    return BadRequest("No File Selected");
                }
                
                var fileExtension = Path.GetExtension(fileUploadRequest.File.FileName).ToLower();
                
                if (!allowedExtensions.Contains(fileExtension))
                {
                    return BadRequest($"Only {string.Join(",", allowedExtensions)} file extensions are allowed");
                }

                if (fileUploadRequest.File.Length > maxFileSize)
                {
                    return BadRequest($"File size cannot be larger than {maxFileSize} MB");
                }
                
                var uploadsDirectory =  Path.Combine(Directory.GetCurrentDirectory(),"wwwroot", "uploads");
                Directory.CreateDirectory(uploadsDirectory);

                var fileName = $"{Guid.NewGuid()}{fileExtension}";
                var filePath = Path.Combine(uploadsDirectory, fileName);

                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await fileUploadRequest.File.CopyToAsync(stream);
                }

                try
                {
                    using (var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read))
                    {
                        var parse = this.spreadsheetProcessor.Process(stream,  fileUploadRequest.File.FileName);
                        logger.LogInformation($"Parsed {fileUploadRequest.File.FileName}: {parse.Rows} rows, {parse.ColumnNames.Count} columns from {parse.SpreadsheetName}");

                        var response = new FileUploadResponse()
                        {
                            FileId = fileName,
                            SheetName = parse.SpreadsheetName,
                            ColumnNames = parse.ColumnNames,
                            Rows = parse.Rows,
                            TotalRows = parse.TotalRows,
                        };

                        return Ok(response);
                    }
                }
                catch (ProcessExcelException ex)
                {
                    logger.LogWarning($"Parse failed for {fileUploadRequest.File.FileName}, : {ex.Message}");
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