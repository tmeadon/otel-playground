using System.Diagnostics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using System.Net.Http.Json;

var builder = WebApplication.CreateBuilder(args);

var serviceName = "api-2";
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

app.MapGet("/hello", async (HttpContext c) =>
{
    // Track work inside of the request
    await Task.Delay(500);

    foreach (var h in c.Request.Headers)
    {
        Console.WriteLine($"{h.Key} = {h.Value}");
    }

    byte[] data = new byte[4096];
    await c.Request.Body.ReadAsync(data, 0, data.Length);
    Console.WriteLine(data.ToString());

    await Work();

    return "Hello, World!";
});

app.Run();

async Task Work()
{
    using var a = MyActivitySource!.StartActivity("Working");
    a?.SetTag("baz", new int[] { 1, 2, 3 });
    await Task.Delay(500);
}
