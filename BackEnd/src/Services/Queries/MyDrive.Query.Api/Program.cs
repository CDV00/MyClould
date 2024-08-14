using Carter;
using MyDrive.Query.API.DependencyInjection.Extensions;
using MyDrive.Query.API.Middleware;
using Serilog;
using MyDrive.Query.Persistence.DependencyInjection.Extensions;
using MyDrive.Query.Infrastructure.DependencyInjection.Extensions;
using MyDrive.Query.Application.DependencyInjection.Extensions;

var builder = WebApplication.CreateBuilder(args);

// Add configuration

Log.Logger = new LoggerConfiguration().ReadFrom
    .Configuration(builder.Configuration)
    .CreateLogger();

builder.Logging
    .ClearProviders()
    .AddSerilog();

builder.Host.UseSerilog();

// Add Carter module
builder.Services.AddCarter();


// Add Swagger
builder.Services
        .AddSwaggerGenNewtonsoftSupport()
        .AddEndpointsApiExplorer()
        .AddSwaggerAPI();

builder.Services
    .AddApiVersioning(options => options.ReportApiVersions = true)
    .AddApiExplorer(options =>
    {
        options.GroupNameFormat = "'v'VVV";
        options.SubstituteApiVersionInUrl = true;
    });

builder.Services.AddCors(p => p.AddPolicy("MyDrive", build =>
{
    build.WithOrigins("*").AllowAnyMethod().AllowAnyHeader();
}));


builder.Services.AddMediatRApplication();

builder.Services.ConfigureServicesInfrastructure(builder.Configuration);
builder.Services.AddMasstransitRabbitMQInfrastructure(builder.Configuration);

builder.Services.AddServicesPersistence();

// Add Middleware => Remember using middleware
builder.Services.AddTransient<ExceptionHandlingMiddleware>();

var app = builder.Build();

app.UseCors("MyDrive");

// Using middleware
app.UseMiddleware<ExceptionHandlingMiddleware>();

// Add API Endpoint with carter module
app.MapCarter();

// Configure the HTTP request pipeline. 
//if (builder.Environment.IsDevelopment() || builder.Environment.IsStaging())
app.UseSwaggerAPI(); // => After MapCarter => Show Version

try
{
    await app.RunAsync();
    Log.Information("Stopped cleanly");
}
catch (Exception ex)
{
    Log.Fatal(ex, "An unhandled exception occured during bootstrapping");
    await app.StopAsync();
}
finally
{
    Log.CloseAndFlush();
    await app.DisposeAsync();
}

public partial class Program { }


