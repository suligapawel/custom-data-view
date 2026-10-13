namespace CustomDataView.Bootstrapper.Development;

public static class FakeWeatherEndpoints
{
    private const string WeatherUrl = "/api/fake/weather";
    private const int MaxDays = 14;
    private const string Metric = "metric";
    private const string Imperial = "imperial";

    private static readonly string[] Summaries =
        ["Słonecznie", "Pochmurno", "Deszczowo", "Burzowo", "Śnieżnie", "Mglisto", "Wietrznie"];

    extension(IEndpointRouteBuilder endpoints)
    {
        public IEndpointRouteBuilder MapFakeWeatherEndpoints()
        {
            return endpoints
                .GetForecast()
                .SearchForecasts();
        }

        private IEndpointRouteBuilder GetForecast()
        {
            endpoints.MapGet(
                    $"{WeatherUrl}/{{city}}/forecast",
                    (string city, int? days, string units) =>
                    {
                        if (!IsValid(days, units, out var error))
                        {
                            return Results.BadRequest(new { error });
                        }

                        return Results.Ok(Forecast(city, days ?? 3, units ?? Metric));
                    })
                .WithTags("Fake weather");
            return endpoints;
        }

        private IEndpointRouteBuilder SearchForecasts()
        {
            endpoints.MapPost(
                    $"{WeatherUrl}/search",
                    (ForecastSearch search) =>
                    {
                        if (search.Cities is not { Count: > 0 })
                        {
                            return Results.BadRequest(new { error = "Podaj co najmniej jedno miasto w \"cities\"." });
                        }

                        if (!IsValid(search.Days, search.Units, out var error))
                        {
                            return Results.BadRequest(new { error });
                        }

                        return Results.Ok(new
                        {
                            results = search.Cities
                                .Select(city => Forecast(city, search.Days ?? 3, search.Units ?? Metric))
                                .ToList()
                        });
                    })
                .WithTags("Fake weather");
            return endpoints;
        }
    }

    private static bool IsValid(int? days, string units, out string error)
    {
        error = null;
        if (days is < 1 or > MaxDays)
        {
            error = $"Parametr \"days\" musi mieć wartość od 1 do {MaxDays}.";
        }
        else if (units is not (null or Metric or Imperial))
        {
            error = $"Parametr \"units\" musi mieć wartość \"{Metric}\" albo \"{Imperial}\".";
        }

        return error is null;
    }

    private static object Forecast(string city, int days, string units)
    {
        var seed = city.ToLowerInvariant().Aggregate(17, (hash, letter) => hash * 31 + letter);
        var random = new Random(seed);
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        return new
        {
            city,
            units,
            generatedAt = DateTime.UtcNow,
            forecast = Enumerable.Range(0, days)
                .Select(day =>
                {
                    var celsius = random.Next(-10, 35);
                    return new
                    {
                        date = today.AddDays(day),
                        temperature = units == Imperial ? Math.Round(celsius * 9 / 5.0 + 32, 1) : celsius,
                        humidity = random.Next(20, 100),
                        windSpeed = Math.Round(random.NextDouble() * 20, 1),
                        summary = Summaries[random.Next(Summaries.Length)]
                    };
                })
                .ToList()
        };
    }

    private sealed record ForecastSearch(IReadOnlyList<string> Cities, int? Days, string Units);
}
