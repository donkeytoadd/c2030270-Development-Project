namespace DevProject.Controllers
{
    using Microsoft.AspNetCore.Mvc;

    [ApiController]
    [Route("api/[controller]")]
    public class FileUploadController : ControllerBase
    {
        private readonly ILogger<FileUploadController> logger;
        
        private readonly string[] allowedExtensions = { ".xlsx", ".xls"};
        private readonly long maxFileSize = 10 * 1024 * 1024;
        
        public FileUploadController(
            ILogger<FileUploadController> logger)
        {
            this.logger = logger;
        }

        [HttpPost("UploadFile")]
        public async Task<IActionResult> UploadFile(IFormFile file)
        {
            try
            {
                if (file.Length == 0)
                {
                    return BadRequest("No File Selected");
                }
                
                var fileExtension = Path.GetExtension(file.FileName).ToLower();
                
                if (!allowedExtensions.Contains(fileExtension))
                {
                    return BadRequest($"Only {string.Join(",", allowedExtensions)} are allowed");
                }

                if (file.Length > maxFileSize)
                {
                    return BadRequest($"File size cannot be larger than {maxFileSize} MB");
                }
                
                var uploadsDirectory =  Path.Combine(Directory.GetCurrentDirectory(),"wwwroot", "uploads");
                Directory.CreateDirectory(uploadsDirectory);

                var fileName = $"{Guid.NewGuid()}{fileExtension}";
                var filePath = Path.Combine(uploadsDirectory, fileName);

                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await file.CopyToAsync(stream);
                }
                
                return Ok(fileName);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }
    }
}