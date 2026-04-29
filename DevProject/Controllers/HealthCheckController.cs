namespace DevProject.Controllers
{
    using System.Net;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.Extensions.Diagnostics.HealthChecks;

    [ApiController]
    [Route("api/[controller]/")]
    public class HealthCheckController : ControllerBase
    {
        private readonly ILogger<HealthCheckController> logger;
        private readonly HealthCheckService healthCheckService;

        public HealthCheckController(
            ILogger<HealthCheckController> logger,
            HealthCheckService healthCheckService)
        {
            this.logger = logger;
            this.healthCheckService = healthCheckService;
        }

        [HttpGet]
        public async Task<IActionResult> GetHealthCheckReport()
        {
            var report = await healthCheckService.CheckHealthAsync();

            logger.LogInformation($"Performing health check: {report}");

            return report.Status == HealthStatus.Healthy ? Ok(report) : StatusCode((int)HttpStatusCode.ServiceUnavailable, report);
        }
    }
}
