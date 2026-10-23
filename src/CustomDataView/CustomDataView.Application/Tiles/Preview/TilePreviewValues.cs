using System.Text.Json;

namespace CustomDataView.Application.Tiles.Preview;

public sealed record TilePreviewValues(
    IReadOnlyDictionary<string, JsonElement> Parameters,
    IReadOnlyDictionary<string, JsonElement> Inputs)
{
    public static readonly TilePreviewValues Empty = new(new Dictionary<string, JsonElement>(), new Dictionary<string, JsonElement>());

    public IReadOnlyDictionary<string, IReadOnlyDictionary<string, JsonElement>> Forms { get; init; } =
        new Dictionary<string, IReadOnlyDictionary<string, JsonElement>>();

    public static TilePreviewValues Of(IReadOnlyDictionary<string, JsonElement> parameters)
        => new(parameters, new Dictionary<string, JsonElement>());

    public IReadOnlyDictionary<string, JsonElement> Form(string address)
        => Forms.GetValueOrDefault(address) ?? new Dictionary<string, JsonElement>();
}
