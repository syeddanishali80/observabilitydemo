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

app.MapGet("/compute", async () =>
{
    using (var activity = activitySource.StartActivity("Compute data"))
    {
        await Task.Delay(500);
    }

    return Results.Ok();
});

app.Run();