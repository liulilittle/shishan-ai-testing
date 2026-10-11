using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Shishan.Api.Data;
using Shishan.Api.Models;
using Shishan.Api.Services;

namespace Shishan.Api.Controllers;

[ApiController]
[Route("api/topics")]
public class TopicsController : ControllerBase
{
    private static readonly JsonSerializerOptions SseJson = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    private readonly TestRunnerService _runner;

    public TopicsController(TestRunnerService runner)
    {
        _runner = runner;
    }

    [HttpGet]
    public ActionResult<List<TopicMeta>> GetAll() => TopicCatalog.All;

    [HttpGet("{code}")]
    public ActionResult<TopicMeta> Get(string code)
    {
        var topic = TopicCatalog.All.FirstOrDefault(t => t.Code.Equals(code, StringComparison.OrdinalIgnoreCase));
        if (topic is null) return NotFound(new { message = $"Topic {code} not found" });
        return topic;
    }

    [HttpPost("{code}/verify")]
    public async Task<ActionResult<VerifyResult>> Verify(string code)
    {
        var topic = TopicCatalog.All.FirstOrDefault(t => t.Code.Equals(code, StringComparison.OrdinalIgnoreCase));
        if (topic is null) return NotFound(new { message = $"Topic {code} not found" });

        var result = await Task.Run(() => _runner.Run(code));
        return result with { TopicCode = topic.Code, TopicTitle = topic.Title };
    }

    [HttpGet("{code}/verify/stream")]
    public async Task VerifyStream(string code)
    {
        var topic = TopicCatalog.All.FirstOrDefault(t => t.Code.Equals(code, StringComparison.OrdinalIgnoreCase));
        var ct = HttpContext.RequestAborted;

        Response.StatusCode = 200;
        Response.ContentType = "text/event-stream; charset=utf-8";
        Response.Headers["Cache-Control"] = "no-cache";
        Response.Headers["Connection"] = "keep-alive";
        Response.Headers["X-Accel-Buffering"] = "no";

        if (topic is null)
        {
            await SendEventAsync("error", new { message = $"Topic {code} not found" }, ct);
            return;
        }

        try
        {
            await Response.WriteAsync(": connected\n\n", ct);
            await Response.Body.FlushAsync(ct);

            // onCase is invoked synchronously by the runner while the stream is in flight and all
            // callbacks complete before StreamAsync returns, so writes never interleave with the
            // summary written below. No extra synchronization is required.
            var onCase = new Action<TestCaseResult>(c =>
            {
                try { SendEventAsync("case", new { name = c.Name, passed = c.Passed }, ct).GetAwaiter().GetResult(); }
                catch { /* client disconnected */ }
            });

            var outcome = await _runner.StreamAsync(code, onCase, ct);

            if (outcome.Error is not null)
            {
                try { await SendEventAsync("error", new { message = outcome.Error }, ct); } catch { }
                return;
            }

            var r = outcome.Result!;
            await SendEventAsync("summary", new
            {
                total = r.Total,
                passed = r.Passed,
                failed = r.Failed,
                allGreen = r.AllGreen,
                outcome = r.Outcome
            }, ct);
        }
        catch (OperationCanceledException)
        {
            // client went away; nothing to do
        }
        catch (Exception ex)
        {
            try { await SendEventAsync("error", new { message = ex.Message }, ct); } catch { }
        }
    }

    private async Task SendEventAsync(string evt, object data, CancellationToken ct)
    {
        string json = JsonSerializer.Serialize(data, SseJson);
        await Response.WriteAsync($"event: {evt}\ndata: {json}\n\n", ct);
        await Response.Body.FlushAsync(ct);
    }
}
