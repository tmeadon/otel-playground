using Azure.Messaging.ServiceBus;

namespace worker;

public interface IServiceBusQueueReceiver : IAsyncDisposable
{
    Task StartProcessingAsync(Func<ReceiverHandlerArgs, Task> msgHandler, Func<ReceiverErrorHandlerArgs, Task> errHander);
}

public class ServiceBusQueueReceiver : IServiceBusQueueReceiver
{
    private ServiceBusProcessor _processer;
    private Func<ReceiverHandlerArgs, Task>? _messageHandler;
    private Func<ReceiverErrorHandlerArgs, Task>? _errorHandler;

    public ServiceBusQueueReceiver(string connectionString, string queueName)
    {
        try
        {
            var sbClient = new ServiceBusClient(connectionString);
            _processer = sbClient.CreateProcessor(queueName);
        }
        catch (Exception e)
        {
            throw new Exception($"failed to initialise Service Bus clients", e);
        }
    }

    private async Task MessageHandlerAsync(ProcessMessageEventArgs args)
    {
        if (_messageHandler != null)
        {
            await _messageHandler.Invoke(new ReceiverHandlerArgs(args));
        }
    }

    private async Task ErrorHandlerAsync(ProcessErrorEventArgs args)
    {
        if (_errorHandler != null)
        {
            await _errorHandler.Invoke(new ReceiverErrorHandlerArgs
            {
                Exception = args.Exception
            });
        }
    }

    public async Task StartProcessingAsync(Func<ReceiverHandlerArgs, Task> msgHandler, Func<ReceiverErrorHandlerArgs, Task> errHander)
    {
        if (msgHandler != null)
        {
            _messageHandler = msgHandler;
            _processer.ProcessMessageAsync += MessageHandlerAsync;
        }

        if (errHander != null)
        {
            _errorHandler = errHander;
            _processer.ProcessErrorAsync += ErrorHandlerAsync;
        }

        await _processer.StartProcessingAsync();
    }

    public async ValueTask DisposeAsync()
    {
        if (_processer != null)
            await _processer.DisposeAsync();
    }
}

public interface IReceiverHandlerArgs
{
    string MessageBody { get; }
    string MessageCorrelationId { get; }
    int MessageDeliveryCount { get; }
    IReadOnlyDictionary<string, object> MessageApplicationProperties { get; }
    Task CompleteMessageAsync();
    Task DeadLetterMessageAsync();
    Task AbandonMessageAsync();
}

public class ReceiverHandlerArgs : IReceiverHandlerArgs
{
    private ProcessMessageEventArgs _args;

    public ReceiverHandlerArgs(ProcessMessageEventArgs args)
    {
        _args = args;
    }
    
    public virtual string MessageBody
    {
        get { return _args.Message.Body.ToString(); }
    }

    public string MessageCorrelationId
    {
        get { return _args.Message.CorrelationId; }
    }

    public int MessageDeliveryCount
    {
        get { return _args.Message.DeliveryCount; }
    }

    public IReadOnlyDictionary<string, object> MessageApplicationProperties
    {
        get { return _args.Message.ApplicationProperties; }
    }

    public async Task CompleteMessageAsync()
    {
        await _args.CompleteMessageAsync(_args.Message);
    }

    public async Task DeadLetterMessageAsync()
    {
        await _args.DeadLetterMessageAsync(_args.Message);
    }

    public async Task AbandonMessageAsync()
    {
        await _args.AbandonMessageAsync(_args.Message);
    }
}

public class ReceiverErrorHandlerArgs
{
    public Exception? Exception { get; set; }
}
