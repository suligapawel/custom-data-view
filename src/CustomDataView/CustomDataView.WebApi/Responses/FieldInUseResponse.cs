using CustomDataView.Domain.Repositories;

namespace CustomDataView.WebApi.Responses;

public sealed record FieldInUseResponse(string Code, IReadOnlyList<string> Fields, IReadOnlyList<TileReference> Tiles)
{
    public const string FieldInUseCode = "field_in_use";
}
