using System.Diagnostics;
using System.Diagnostics.Metrics;
using System.Net;
using Grpc.Core;

namespace AnotherSampleGrpcServer.Services;

public class WeatherService : Weather.WeatherBase
{
    private readonly Random _random = new Random();
    private readonly ILogger<WeatherService> _logger;
    private readonly Meter _meter = new Meter("GRPC-ApplicationMetrics");

    private readonly ActivitySource _activitySource = new ActivitySource("GRPC-ApplicationActivitySource");

    // private readonly Histogram<float> _histogram;
    // private readonly Counter<int> _weatherRequestsCounter;
    public WeatherService(ILogger<WeatherService> logger)
    {
        _logger = logger;
    }
    public override async Task<WeatherResponse> GetWeather(WeatherRequest request, ServerCallContext context)
    {
        var _weatherRequestsCounter = _meter.CreateCounter<int>("weather_requests");
        var _histogram = _meter.CreateHistogram<float>("weather_changes", unit: "c");
        var _responseHistogram = _meter.CreateHistogram<int>("some_other_service", unit: "ms");

        _weatherRequestsCounter.Add(1);
        _logger.LogInformation("Get Weather Request from {token}", request.Meta?.Token);

        var httpClient = new HttpClient();
        var response = await httpClient.GetAsync("https://localhost:7259/compute");

        _logger.LogInformation("Received response from localhost:7259/compute");

        using var activity = _activitySource?.CreateActivity("Get Weather", ActivityKind.Client);
        activity?.SetTag("token", request.Meta?.Token);
        activity?.SetTag("Latitude", request.Lat);
        activity?.SetTag("Longitude", request.Long);
        activity?.SetTag("City", request.City);
        activity?.SetTag("Country", request.Country);

        var temperature = _random.Next(10, 50);

        _histogram.Record(temperature, tag1: KeyValuePair.Create<string, object?>("City", request.City), tag2: KeyValuePair.Create<string, object?>("Country", request.Country));
        _responseHistogram.Record((int)response.StatusCode, tag1: KeyValuePair.Create<string, object?>("City", request.City), tag2: KeyValuePair.Create<string, object?>("Country", request.Country));

        return new WeatherResponse()
        {
            Meta = new ResponseMeta()
            {
                Code = 200,
                Success = true,
                Message = "Weather fetched successfully!"
            },
            Temperature = temperature,
            Unit = "centigrade"
        };
    }
}
