using Microsoft.AspNetCore.Mvc;

namespace frontend.Controllers;

[ApiController]
[Route("[controller]")]
public class HelloController : ControllerBase
{
    private readonly ILogger<HelloController> _logger;
    private IServiceBusQueueSender _queueSender;

    public HelloController(ILogger<HelloController> logger, IServiceBusQueueSender queueSender)
    {
        _logger = logger;
        _queueSender = queueSender;
    }

    [HttpGet()]
    public async Task<string> GetAsync()
    {
        SendSomeLogs();
        await SendMessageAsync();
        var result = await CallBackendApiAsync();
        return $"result from backend: {result}";
    }

    private void SendSomeLogs()
    {
        _logger.LogInformation("message: {0}", "info");
        _logger.LogDebug("debug message");
        _logger.LogError("error message", new Exception("exception"));
    }

    private async Task<string> CallBackendApiAsync()
    {
        using (var http = new HttpClient())
        {
            var result = await http.GetAsync("http://backend/hello");
            return await result.Content.ReadAsStringAsync();
        }
    }

    private async Task SendMessageAsync()
    {
        await _queueSender.SendMessageAsync("hello", Guid.NewGuid().ToString());
    }
}
