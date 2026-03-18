using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo 
    { 
        Title = "Dev Project API"
    });
});

builder.Services.AddControllers().AddJsonOptions(options =>
{
    options.JsonSerializerOptions.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter());
});

//check currently validates the local directory.
// when migrating to cloud, will replace this with a connectivity check for the chosen provider (e.g Azure Blob Storage, AWS S3)
builder.Services.AddHealthChecks()
    .AddCheck("file-storage", () =>
    {
        var uploadsPath = Path.Combine("wwwroot", "uploads");
        return Directory.Exists(uploadsPath)
            ? HealthCheckResult.Healthy()
            : HealthCheckResult.Unhealthy("Uploads directory is missing.");
    });

builder.Services.Scan(scan => scan
    .FromAssemblies(typeof(Program).Assembly)
    .AddClasses(c => c.Where(t =>
        t.Name.EndsWith("Getter") ||
        t.Name.EndsWith("Creator") ||
        t.Name.EndsWith("Updater") ||
        t.Name.EndsWith("Query") ||
        t.Name.EndsWith("Middleware") ||
        t.Name.EndsWith("Processor")
    ))
    .AsImplementedInterfaces()
    .WithScopedLifetime()
);

var myAllowSpecificOrigins = "myAllowSpecificOrigins";

builder.Services.AddCors(options =>
{
    options.AddPolicy(name: myAllowSpecificOrigins,
        policyBuilder =>
        {
            policyBuilder.WithOrigins("http://localhost:4200")
                .AllowAnyHeader()
                .AllowAnyMethod()
                .AllowCredentials();
        });
});

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "Dev Project API v1");
    c.RoutePrefix = "swagger";
});

app.UseStaticFiles();

app.UseRouting();
app.UseCors(myAllowSpecificOrigins);

app.UseAuthorization();

app.MapControllers();

app.Run();