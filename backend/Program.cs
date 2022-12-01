using System.Diagnostics;
using System.Diagnostics.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using OpenTelemetry.Metrics;
using OpenTelemetry.Logs;
using OpenTelemetry.Exporter;

var builder = WebApplication.CreateBuilder(args);

var serviceName = "backend";
var version = "1.0.0";

builder.Services.AddOpenTelemetryTracing(builder => 
{
    builder.AddConsoleExporter()
        .AddSource(serviceName)
        .SetResourceBuilder(
            ResourceBuilder.CreateDefault()
                .AddService(serviceName: serviceName, serviceVersion: version))
        .AddHttpClientInstrumentation()
        .AddAspNetCoreInstrumentation()
        .AddOtlpExporter(opts => { 
            opts.Endpoint = new Uri("http://otelcol:4317");
            opts.Protocol = OpenTelemetry.Exporter.OtlpExportProtocol.Grpc;
        });
});

builder.Services.AddOpenTelemetryMetrics(builder =>
{
    builder.SetResourceBuilder(ResourceBuilder.CreateDefault().AddService(serviceName: serviceName, serviceVersion: version))
        .AddMeter("toms-meter")
        .AddConsoleExporter()
        .AddAspNetCoreInstrumentation()
        .AddOtlpExporter(opts => { 
            opts.Endpoint = new Uri("http://otelcol:4317");
            opts.Protocol = OpenTelemetry.Exporter.OtlpExportProtocol.Grpc;
            
        });
});

builder.Logging.AddOpenTelemetry(opts =>
{
    opts.AddConsoleExporter();
    opts.ConfigureResource(configure => 
    {
        configure.AddService(serviceName: serviceName, serviceVersion: version);
    });
    opts.AddOtlpExporter(opts =>
    {
        opts.Endpoint = new Uri("http://otelcol:4317");
        opts.Protocol = OpenTelemetry.Exporter.OtlpExportProtocol.Grpc;
    });
});


// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddSingleton<ActivitySource>(sp => new ActivitySource("backend", "1.0.0"));

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
