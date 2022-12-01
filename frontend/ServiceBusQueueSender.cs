using Azure.Messaging.ServiceBus;
using System.Diagnostics;

namespace frontend;

public interface IServiceBusQueueSender : IAsyncDisposable
{
    Task SendMessageAsync(string body, string correlationId);
}

public class ServiceBusQueueSender : IServiceBusQueueSender
{
    private ServiceBusSender _sender;
    private ActivitySource _activitySource;

    public ServiceBusQueueSender(string connectionString, string queueName, ActivitySource activitySource)
    {
        try
        {
            var sbClient = new ServiceBusClient(connectionString);
            _sender = sbClient.CreateSender(queueName);
            _activitySource = activitySource;
        }
        catch (Exception e)
        {
            throw new Exception($"failed to initialise Service Bus clients", e);
        }
    }

    public async Task SendMessageAsync(string body, string correlationId)
    {
        var msg = new ServiceBusMessage(body);

        try
        {
            using (var a = _activitySource.StartActivity("Send message", ActivityKind.Producer))
            {
                a?.AddTag("service_bus_entity", _sender.EntityPath);
                var msgProps = msg.ApplicationProperties;
                msgProps.Add("traceparent", a?.Id);
                await _sender.SendMessageAsync(msg);
            }
        }
        catch (Exception e)
        {
            throw new Exception($"failed to send service bus message to queue {_sender.EntityPath}", e);
        }
    }

    public async ValueTask DisposeAsync()
    {
        await _sender.DisposeAsync();
    }
}
