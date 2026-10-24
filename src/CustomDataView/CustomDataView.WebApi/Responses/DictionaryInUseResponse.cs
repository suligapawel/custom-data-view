using CustomDataView.Domain.Repositories;

namespace CustomDataView.WebApi.Responses;

public sealed record DictionaryInUseResponse(string Code, IReadOnlyList<string> Fields, IReadOnlyList<TileReference> Tiles)
{
    public const string DictionaryInUseCode = "dictionary_in_use";
}
