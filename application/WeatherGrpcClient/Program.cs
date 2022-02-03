// See https://aka.ms/new-console-template for more information
using AnotherSampleGrpcServer;
using Grpc.Net.Client;

Console.WriteLine("Weather GRPC Client");

using var channel = GrpcChannel.ForAddress(@"https://localhost:7276");
var client = new Weather.WeatherClient(channel);

var random = new Random();

var weatherResponse = await client.GetWeatherAsync(new WeatherRequest()
{
    Meta = new RequestMeta()
    {
        Token = Guid.NewGuid().ToString()
    },
    Lat = random.NextDouble() * (50.0 - 10.0) + 10.0,
    Long = random.NextDouble() * (50.0 - 10.0) + 10.0,
    City = "Islamabad",
    Country = "Pakistan"
});

System.Console.WriteLine($"Weather received: {weatherResponse.Meta.Success}");

if (weatherResponse.Meta?.Success ?? false)
    System.Console.WriteLine("Weather now: {0} ({1})", weatherResponse.Temperature, weatherResponse.Unit);

Console.WriteLine("Press any key to exit...");
Console.ReadKey();