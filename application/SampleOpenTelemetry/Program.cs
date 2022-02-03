using System.Diagnostics.Metrics;
using System.Diagnostics;
using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;
using OpenTelemetry;
using OpenTelemetry.Logs;

// This is required if the collector doesn't expose an https endpoint
AppContext.SetSwitch("System.Net.Http.SocketsHttpHandler.Http2UnencryptedSupport", true);

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddOpenTelemetryMetrics(builder =>
{
    builder.AddHttpClientInstrumentation();
    builder.AddAspNetCoreInstrumentation();
    builder.AddMeter("MyApplicationMetrics");
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

var activitySource = new ActivitySource("MyApplicationActivitySource");
var meter = new Meter("MyApplicationMetrics");
var requestCounter = meter.CreateCounter<int>("compute_requests");
var newCounter = meter.CreateCounter<double>("new_double_counter");
var httpClient = new HttpClient();

app.MapGet("/error", async (ILogger<Program> logger) =>
{
    requestCounter.Add(1);

    using (var activity = activitySource.StartActivity("Get data"))
    {
        activity?.AddTag("sample", "value");

        var str1 = await httpClient.GetStringAsync("https://example.com");
        var str2 = await httpClient.GetStringAsync("https://www.meziantou.net");
        await httpClient.GetStringAsync("https://localhost:7259/compute");

        logger.LogInformation("Response1 length: {Length}", str1.Length);
        logger.LogInformation("Response2 length: {Length}", str2.Length);

        await httpClient.GetStringAsync("https://localhost:7120/error");
    }

    return Results.Ok();
});

app.MapGet("/success", async (ILogger<Program> logger) =>
{
    newCounter.Add(0.5);
    using (var activity = activitySource.StartActivity("New Activity"))
    {
        activity?.AddTag("sample2", "value2");

        var str1 = await httpClient.GetStringAsync("https://example.com");
        await Task.Delay(2000);
        await httpClient.GetStringAsync("https://localhost:7259/compute");
        await Task.Delay(100);
        activity?.AddTag("sample3", "value2");
        var response = await httpClient.GetStringAsync("https://localhost:7120/success");

        logger.LogInformation("Response received: {value}", response);
    }

    return Results.Ok();
});

app.Run();