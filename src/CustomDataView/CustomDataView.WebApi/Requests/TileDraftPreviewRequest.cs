using System.Text.Json;
using CustomDataView.Application.Tiles.Validation;

namespace CustomDataView.WebApi.Requests;

public sealed record TileDraftPreviewRequest(
    TileRequest Tile,
    Dictionary<string, JsonElement> Parameters,
    Dictionary<string, JsonElement> Inputs,
    Dictionary<string, Dictionary<string, JsonElement>> Forms,
    Dictionary<string, TileDescendantValuesRequest> Descendants)
{
    public IDictionary<string, string[]> Validate(TileReferencesValidator referencesValidator)
    {
        if (Tile is null)
        {
            return new Dictionary<string, string[]> { ["tile"] = ["Definicja kafelka jest wymagana."] };
        }

        var errors = new Dictionary<string, string[]>(Tile.Validate());
        foreach (var error in referencesValidator.Validate(Tile.ToInput()))
        {
            errors.TryAdd(error.Key, error.Value);
        }

        return errors.ToDictionary(error => $"tile.{error.Key}", error => error.Value);
    }
}
