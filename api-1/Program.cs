using System.Diagnostics;
using System.Diagnostics.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using OpenTelemetry.Metrics;
using OpenTelemetry.Logs;

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

var MyActivitySource = new ActivitySource(serviceName);

builder.Services.AddSingleton<Process>(sp => { return new Process(MyActivitySource); });

var app = builder.Build();

var meter = new Meter("toms-meter");
var reqCounter = meter.CreateCounter<int>("totalRequests");

app.MapGet("/hello", async (Process process, ILogger<Program> logger) =>
{
    // Track work inside of the request
    logger.LogInformation("message: {0}", "blaaaaah");
    await Task.Delay(500);

    reqCounter.Add(1);

    var req = CreateRequest(process);

    var client = new HttpClient();
    await client.PostAsJsonAsync<Request>("http://api-2/hello", req);
    await client.GetAsync("http://api-2/hello");

    return "Hello, World!";
});

app.Run();

Request CreateRequest(Process process)
{
    var prev = Activity.Current;
    Activity.Current = null;

    var text = "some-text";

    Request req;

    using (var a = MyActivitySource.StartActivity("CreateRequest", ActivityKind.Producer, process.Context))
    {
        a?.SetTag("text", text);
        req = new Request(a!.Context.SpanId.ToHexString(), a!.Context.TraceId.ToHexString(), text);
    }
    
    Activity.Current = prev;
    return req;
}

public record Request(string SpanId, string TraceId, string Text);

public class Process
{
    public ActivityContext Context { get; }
    
    public Process(ActivitySource source)
    {
        var prev = Activity.Current;
        Activity.Current = null;

        using (var a = source.StartActivity("LongRunningProcess"))
        {
            a?.SetTag("id", "id123");
            a?.SetBaggage("id", "id321");
            Context = a?.Context ?? new ActivityContext();
        }

        Activity.Current = prev;
    }
}
