using System.Globalization;
using System.Text.Json;
using System.Text.Json.Nodes;
using CustomDataView.Domain.Services;
using CustomDataView.Domain.ValueObjects;

namespace CustomDataView.Application.Tiles.Preview;

public static class TilePages
{
    public static bool TryRead(
        IReadOnlyDictionary<string, JsonElement> values,
        string name,
        int fallback,
        int max,
        out int value)
    {
        value = fallback;
        if (!values.TryGetValue(name, out var provided))
        {
            return true;
        }

        var parsed = provided.ValueKind switch
        {
            JsonValueKind.Number when provided.TryGetInt32(out var number) => number,
            JsonValueKind.String when int.TryParse(provided.GetString(), NumberStyles.None, CultureInfo.InvariantCulture, out var number) => number,
            _ => (int?)null
        };
        value = parsed ?? fallback;
        return parsed is >= 1 && parsed <= max;
    }

    public static TilePaginationPreview Preview(TilePagination pagination, Page page, int count, JsonNode body, TileValues delivered)
    {
        var total = Total(pagination.TotalPath, body);
        int? pages = total is { } known ? (known + page.Size - 1) / page.Size : null;
        var hasNext = pages is { } last ? page.Number < last : count >= page.Size;
        return new TilePaginationPreview(
            page.Number,
            page.Size,
            total,
            pages,
            page.Number > 1 ? Values(pagination, delivered, page.Number - 1, page.Size) : null,
            hasNext ? Values(pagination, delivered, page.Number + 1, page.Size) : null);
    }

    private static int? Total(string totalPath, JsonNode body)
    {
        if (totalPath is null || !JsonPathParser.TryParse(totalPath, out var path))
        {
            return null;
        }

        return path.Evaluate(body) is [JsonValue value] && value.TryGetValue<int>(out var total) && total >= 0 ? total : null;
    }

    private static TileValues Values(TilePagination pagination, TileValues delivered, int number, int size)
    {
        var parameters = new Dictionary<string, JsonNode>(delivered.Parameters, StringComparer.Ordinal)
        {
            [pagination.PageNumberParameter] = JsonValue.Create(number),
            [pagination.PageSizeParameter] = JsonValue.Create(size)
        };
        return (delivered with { Parameters = parameters }).Compact();
    }

    public sealed record Page(int Number, int Size);
}
