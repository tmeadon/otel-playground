using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;

namespace backend.Controllers;

[ApiController]
[Route("[controller]")]
public class HelloController : ControllerBase
{
    private readonly ILogger<HelloController> _logger;
    private readonly ActivitySource _activitySource;

    public HelloController(ILogger<HelloController> logger, ActivitySource activitySource)
    {
        _logger = logger;
        _activitySource = activitySource;
    }

    [HttpGet()]
    public async Task<string> GetAsync()
    {
        await DoSomeWorkAsync();
        return "hello!";
    }

    private async Task DoSomeWorkAsync()
    {
        using (var activity = _activitySource.StartActivity("working", ActivityKind.Internal))
        {
            activity?.AddTag("some tag", "some value");
            activity?.AddEvent(new ActivityEvent("starting"));
            await Task.Delay(2000);
            activity?.AddEvent(new ActivityEvent("finishing"));
        }
    }
}
