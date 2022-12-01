using Azure.Messaging.ServiceBus;

namespace worker;

public class Worker : BackgroundService
{
    private readonly ILogger<Worker> _logger;
    private IServiceBusQueueReceiver _queueReceiver;
    private ActivitySource _activitySource;
    private Counter<int> _requestCounter;

    public Worker(ILogger<Worker> logger, IServiceBusQueueReceiver queueReceiver, ActivitySource activitySource, Meter meter)
    {
        _logger = logger;
        _queueReceiver = queueReceiver;
        _activitySource = activitySource;
        _requestCounter = meter.CreateCounter<int>("message_count");
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await _queueReceiver.StartProcessingAsync(OnReceiveAsync, OnReceiveErrorAsync);

        while (!stoppingToken.IsCancellationRequested)
        {
            _logger.LogInformation("Worker running at: {time}", DateTimeOffset.Now);
            await Task.Delay(30000, stoppingToken);
        }
    }

    private async Task OnReceiveAsync(ReceiverHandlerArgs args)
    {
        _requestCounter.Add(1);

        var headers = args.MessageApplicationProperties;

        using var activity = _activitySource.StartActivity("Received message", ActivityKind.Consumer, headers["traceparent"].ToString());
        activity?.AddTag("tag", "value");

        // step 1
        using (var step1 = _activitySource.StartActivity("Process step 1", ActivityKind.Internal))
        {
            step1?.AddEvent(new ActivityEvent("starting"));
            await Task.Delay(200);
            step1?.AddEvent(new ActivityEvent("done"));
        }

        // step 2
        using (var step2 = _activitySource.StartActivity("Process step 2", ActivityKind.Internal))
        {
            step2?.AddEvent(new ActivityEvent("starting"));
            await Task.Delay(200);
            step2?.AddEvent(new ActivityEvent("done"));
        }

        activity?.AddEvent(new ActivityEvent("some information"));
    }

    private async Task OnReceiveErrorAsync(ReceiverErrorHandlerArgs args)
    {
        await Task.Run(() => throw new Exception(args?.Exception?.Message));
    }
}
