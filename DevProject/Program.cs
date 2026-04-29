using Azure.Storage.Blobs;
using DevProject.Business.Storage;
using DevProject.Business.Storage.Interfaces;
using DevProject.Infrastructure;
using DevProject.Workers;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
        options.JsonSerializerOptions.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter());
    });

builder.Services.AddEndpointsApiExplorer();

if (builder.Environment.IsDevelopment())
{
    builder.Services.AddSwaggerGen(c =>
    {
        c.SwaggerDoc("v1", new OpenApiInfo { Title = "Dev Project API" });
    });

    builder.Services.AddCors(options =>
    {
        options.AddPolicy("devCors", policy =>
        {
            policy.WithOrigins("http://localhost:4200")
                  .AllowAnyHeader()
                  .AllowAnyMethod()
                  .AllowCredentials();
        });
    });
}

builder.Services.AddHostedService<FileCleanupWorker>();

if (builder.Environment.IsDevelopment())
{
    builder.Services.AddSingleton<IFileStorage, LocalFileStorage>();

    var hostEnv = builder.Environment;
    builder.Services.AddHealthChecks()
        .AddCheck("file-storage", () =>
        {
            var webRoot     = hostEnv.WebRootPath ?? Path.Combine(hostEnv.ContentRootPath, "wwwroot");
            var uploadsPath = Path.Combine(webRoot, "uploads");
            return Directory.Exists(uploadsPath)
                ? HealthCheckResult.Healthy()
                : HealthCheckResult.Unhealthy("Uploads directory is missing.");
        });
}
else
{

    builder.Services.AddSingleton(sp =>
    {
        var config         = sp.GetRequiredService<IConfiguration>();
        var connectionString = config["BlobStorage:ConnectionString"]
                               ?? throw new InvalidOperationException("BlobStorage:ConnectionString is required in production.");
        var containerName  = config["BlobStorage:ContainerName"] ?? "uploads";

        return new BlobContainerClient(connectionString, containerName);
    });

    builder.Services.AddSingleton<IFileStorage, AzureBlobFileStorage>();

    builder.Services.AddHealthChecks()
        .AddCheck<BlobStorageHealthCheck>("file-storage");
}

builder.Services.Scan(scan => scan
    .FromAssemblies(typeof(Program).Assembly)
    .AddClasses(c => c.Where(t =>
        t.Name.EndsWith("Getter")    ||
        t.Name.EndsWith("Creator")   ||
        t.Name.EndsWith("Updater")   ||
        t.Name.EndsWith("Query")     ||
        t.Name.EndsWith("Middleware")||
        t.Name.EndsWith("Processor")
    ))
    .AsImplementedInterfaces()
    .WithScopedLifetime()
);

builder.Services.AddSingleton<IConversionResultStore, ConversionResultStore>();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var storage = scope.ServiceProvider.GetRequiredService<IFileStorage>();
    app.Logger.LogInformation(
        "Environment: {Env}. IFileStorage implementation: {Type}",
        app.Environment.EnvironmentName,
        storage.GetType().Name);
}

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "Dev Project API v1");
        c.RoutePrefix = "swagger";
    });
}

app.UseDefaultFiles();
app.UseStaticFiles();
app.UseRouting();

if (app.Environment.IsDevelopment())
{
    app.UseCors("devCors");
}

app.UseAuthorization();
app.MapControllers();

app.MapFallbackToFile("{*path:regex(^(?!api).*$)}", "index.html");

app.Run();