using System.Diagnostics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using System.Net.Http.Json;
using Azure.Monitor.OpenTelemetry.Exporter;

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
            opts.Endpoint = new Uri("http://otelcol:4317");
            opts.Protocol = OpenTelemetry.Exporter.OtlpExportProtocol.Grpc;
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

app.MapPost("/hello", async (Request req) =>
{
    Console.WriteLine(req);
    await ProcessRequest(req);
});


app.Run();

async Task Work()
{
    using var a = MyActivitySource!.StartActivity("Working");
    a?.SetTag("baz", new int[] { 1, 2, 3 });
    
    await Task.Delay(500);
}

async Task ProcessRequest(Request req)
{
    var prev = Activity.Current;
    Activity.Current = null;

    var text = "some-text";

    var ctx = new ActivityContext(ActivityTraceId.CreateFromString(req.TraceId), ActivitySpanId.CreateFromString(req.SpanId), ActivityTraceFlags.Recorded);

    using (var a = MyActivitySource?.StartActivity("ProcessRequest", ActivityKind.Server, ctx))
    {
        a.AddEvent(new ActivityEvent("waiting"));
        a?.SetTag("text", text);
        await Task.Delay(200);
        a.AddEvent(new ActivityEvent("done"));

    }

    Activity.Current = prev;
}

public record Request(string SpanId, string TraceId, string Text);
