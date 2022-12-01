global using System.Diagnostics;
global using System.Diagnostics.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using OpenTelemetry.Metrics;
using OpenTelemetry.Logs;
using worker;

var serviceName = "worker";
var version = "1.0.0";

IHost host = Host.CreateDefaultBuilder(args)
    .ConfigureServices(services =>
    {

        services.AddHostedService<Worker>();
        services.AddOpenTelemetryTracing(builder => 
        {
            builder.AddConsoleExporter()
                .AddSource(serviceName)
                .SetResourceBuilder(
                    ResourceBuilder.CreateDefault()
                        .AddService(serviceName: serviceName, serviceVersion: version))
                .AddHttpClientInstrumentation()
                .AddAspNetCoreInstrumentation()
                .AddOtlpExporter(opts => { 
                    opts.Protocol = OpenTelemetry.Exporter.OtlpExportProtocol.Grpc;
                });
        });
        services.AddOpenTelemetryMetrics(builder =>
        {
            builder.SetResourceBuilder(ResourceBuilder.CreateDefault().AddService(serviceName: serviceName, serviceVersion: version))
                .AddMeter("toms-meter")
                .AddConsoleExporter()
                .AddAspNetCoreInstrumentation()
                .AddOtlpExporter(opts => { 
                    opts.Protocol = OpenTelemetry.Exporter.OtlpExportProtocol.Grpc;
                    
            });
        });
        services.AddSingleton<Meter>(sp => new Meter(serviceName));
        services.AddSingleton<ActivitySource>(sp => new ActivitySource(serviceName, version));
        services.AddSingleton<IServiceBusQueueReceiver>(sp => new ServiceBusQueueReceiver(
            sp.GetRequiredService<IConfiguration>().GetValue<string>("sb_conn"),
            "queue"
        ));
    })
    .ConfigureLogging(log =>
    {
        log.AddOpenTelemetry(opts =>
        {
            opts.AddConsoleExporter();
            opts.ConfigureResource(configure => 
            {
                configure.AddService(serviceName: serviceName, serviceVersion: version);
            });
            opts.AddOtlpExporter(opts =>
            {
                opts.Protocol = OpenTelemetry.Exporter.OtlpExportProtocol.Grpc;
            });
        });
    })
    .Build();



await host.RunAsync();
