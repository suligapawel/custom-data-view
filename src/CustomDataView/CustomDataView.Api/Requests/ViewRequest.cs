using CustomDataView.Core.Views.Definitions;
using CustomDataView.Core.Views.Validation;

namespace CustomDataView.Api.Requests;

public sealed record ViewRequest(string Name, IReadOnlyList<ViewTileRequest> Tiles)
{
    public IDictionary<string, string[]> Validate() => ViewDefinitionValidator.Validate(ToInput());

    public ViewDefinition ToDefinition() => ToInput().ToDefinition();

    public ViewInput ToInput()
        => new ViewInput(
            Name,
            (Tiles ?? []).Select(tile => new ViewTileInput(tile.TileId, tile.Position, tile.CssClass)).ToList());
}

public sealed record ViewTileRequest(Guid? TileId, int? Position, string CssClass);
