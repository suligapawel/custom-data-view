namespace CustomDataView.Bootstrapper.Development;

public static class FakeCityEndpoints
{
    private const string CitiesUrl = "/api/fake/cities";
    private const int MaxPageSize = 100;

    private static readonly City[] Cities =
    [
        new(1, "Kraków", "Polska", 800000),
        new(2, "Krynica-Zdrój", "Polska", 10000),
        new(3, "Kraśnik", "Polska", 33000),
        new(4, "Krotoszyn", "Polska", 28000),
        new(5, "Krosno", "Polska", 45000),
        new(6, "Kristiansand", "Norwegia", 115000),
        new(7, "Kragujevac", "Serbia", 180000),
        new(8, "Krefeld", "Niemcy", 227000),
        new(9, "Kremenczuk", "Ukraina", 210000),
        new(10, "Kranj", "Słowenia", 38000),
        new(11, "Kraljevo", "Serbia", 64000),
        new(12, "Krems", "Austria", 25000),
        new(13, "Gdańsk", "Polska", 486000),
        new(14, "Gdynia", "Polska", 245000),
        new(15, "Berlin", "Niemcy", 3700000),
        new(16, "Wiedeń", "Austria", 1900000)
    ];

    extension(IEndpointRouteBuilder endpoints)
    {
        public IEndpointRouteBuilder MapFakeCityEndpoints()
        {
            endpoints.MapGet(
                    CitiesUrl,
                    (string name, string country, int? pageNumber, int? pageSize) =>
                    {
                        var number = pageNumber ?? 1;
                        var size = pageSize ?? 10;
                        if (number < 1 || size is < 1 or > MaxPageSize)
                        {
                            return Results.BadRequest(new { error = $"pageNumber >= 1, pageSize od 1 do {MaxPageSize}." });
                        }

                        var matches = Cities
                            .Where(city => string.IsNullOrWhiteSpace(name) || city.Name.StartsWith(name.Trim(), StringComparison.CurrentCultureIgnoreCase))
                            .Where(city => string.IsNullOrWhiteSpace(country) || string.Equals(city.Country, country.Trim(), StringComparison.CurrentCultureIgnoreCase))
                            .ToList();
                        return Results.Ok(new
                        {
                            total = matches.Count,
                            cities = matches.Skip((number - 1) * size).Take(size).Select(city => new { id = city.Id, name = city.Name, country = city.Country })
                        });
                    })
                .WithTags("Fake cities");
            endpoints.MapGet(
                    $"{CitiesUrl}/{{cityId:int}}",
                    (int cityId) => Cities.FirstOrDefault(city => city.Id == cityId) is { } city
                        ? Results.Ok(new { id = city.Id, name = city.Name, country = city.Country, population = city.Population })
                        : Results.NotFound())
                .WithTags("Fake cities");
            return endpoints;
        }
    }

    private sealed record City(int Id, string Name, string Country, int Population);
}
