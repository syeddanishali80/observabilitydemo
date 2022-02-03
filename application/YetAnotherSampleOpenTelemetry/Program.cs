using OpenTelemetry.Trace;
using OpenTelemetry;
using OpenTelemetry.Logs;
using OpenTelemetry.Metrics;
using System.Diagnostics;
using System.Diagnostics.Metrics;

// This is required if the collector doesn't expose an https endpoint
AppContext.SetSwitch("System.Net.Http.SocketsHttpHandler.Http2UnencryptedSupport", true);

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenTelemetryMetrics(builder =>
{
    builder.AddHttpClientInstrumentation();
    builder.AddAspNetCoreInstrumentation();
    builder.AddMeter("SomeApplicationMetrics");
    builder.AddOtlpExporter(options => options.Endpoint = new Uri("http://localhost:4317"));
});

builder.Services.AddOpenTelemetryTracing(builder =>
{
    builder.AddAspNetCoreInstrumentation();
    builder.AddHttpClientInstrumentation();
    builder.AddSource("MyApplicationActivitySource");
    builder.AddOtlpExporter(options => options.Endpoint = new Uri("http://localhost:4317"));
});

builder.Logging.AddOpenTelemetry(builder =>
{
    builder.IncludeFormattedMessage = true;
    builder.IncludeScopes = true;
    builder.ParseStateValues = true;
    builder.AddOtlpExporter(options => options.Endpoint = new Uri("http://localhost:4317"));
});

var app = builder.Build();

var activitySource = new ActivitySource("SomeApplicationActivitySource1");
var meter = new Meter("SomeApplicationMetrics");
var successCounter = meter.CreateCounter<int>("success_count");
var failureCounter = meter.CreateCounter<int>("failure_count");
var httpClient = new HttpClient();

app.MapGet("/error", async (ILogger<Program> logger) =>
{
    var randomResult = new { Id = 1, Name = "Test", For = "Testing Project" };

    failureCounter.Add(1);
    using (var activity = activitySource.StartActivity("Appointment Creation - Error"))
    {
        //simple event
        var beforeWaitEvent = new ActivityEvent("Before Wait");
        activity?.AddEvent(beforeWaitEvent);
        await Task.Delay(500);

        var tagsCollection = new ActivityTagsCollection();
        tagsCollection.Add("SOME_EVENT_TAG", 1);
        tagsCollection.Add("ANOTHER_RANDOM_EVENT_TAG", ("{@value}", new { Id = 1, Name = "Test", For = "Testing Project" }));
        //adding another event with tags
        var afterWaitEvent = new ActivityEvent("After Wait", DateTimeOffset.UtcNow, tagsCollection);
        activity?.AddEvent(afterWaitEvent);

        activity?.SetTag("otel.status_code", "ERROR");
        activity?.SetTag("otel.status_description", "Use this text give more information about the error");
    }

    return Results.BadRequest(randomResult);
});

app.MapGet("/success", async (ILogger<Program> logger) =>
{

    var randomResult = new { Id = 1, Name = "Test", For = "Testing Project" };

    successCounter.Add(1);
    using (var activity = activitySource.StartActivity("Appointment Creation - Success"))
    {
        //simple event
        var beforeWaitEvent = new ActivityEvent("Before Wait");
        activity?.AddEvent(beforeWaitEvent);
        await Task.Delay(500);

        var tagsCollection = new ActivityTagsCollection();
        tagsCollection.Add("SOME_EVENT_TAG", 1);
        tagsCollection.Add("ANOTHER_RANDOM_EVENT_TAG", ("{@value}", randomResult));
        //adding another event with tags
        var afterWaitEvent = new ActivityEvent("After Wait", DateTimeOffset.UtcNow, tagsCollection);
        activity?.AddEvent(afterWaitEvent);

        activity?.SetTag("otel.status_code", "OK");
        activity?.SetTag("otel.status_description", "Use this text give more information about the success");
    }

    return Results.Ok(randomResult);
});

app.Run();
