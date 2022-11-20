using System.Diagnostics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using System.Net.Http;

var builder = WebApplication.CreateBuilder(args);

var serviceName = "api-1";
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
        .AddSqlClientInstrumentation()
        .AddOtlpExporter(opts => { 
            opts.Endpoint = new Uri("http://jaeger:4317");
        });
});

var app = builder.Build();

var MyActivitySource = new ActivitySource(serviceName);

app.MapGet("/hello", async () =>
{
    // Track work inside of the request
    await Task.Delay(500);

    var client = new HttpClient();
    await client.GetAsync("http://api-2/hello");

    await Work();

    return "Hello, World!";
});

app.Run();

async Task Work()
{
    using var a = MyActivitySource!.StartActivity("Working");
    a?.SetTag("baz", new int[] { 1, 2, 3 });
    await Task.Delay(1500);
}
