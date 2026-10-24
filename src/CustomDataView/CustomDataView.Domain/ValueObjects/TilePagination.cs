using CustomDataView.Domain.Policies;
using CustomDataView.Domain.Services;

namespace CustomDataView.Domain.ValueObjects;

public sealed record TilePagination(string PageNumberParameter, string PageSizeParameter, int PageSize, string TotalPath)
{
    public const int MaxPageSize = 100;

    public void Check(DefinitionViolations violations)
    {
        if (PageSize is not (>= 1 and <= MaxPageSize))
        {
            violations.Set("pagination.pageSize", $"Rozmiar strony jest wymagany i musi wynosić od 1 do {MaxPageSize}.");
        }

        if (PageNumberParameter.Length > TileNodeKeys.MaxLength || PageSizeParameter.Length > TileNodeKeys.MaxLength)
        {
            violations.Set("pagination", $"Nazwy parametrów stronicowania mogą mieć najwyżej {TileNodeKeys.MaxLength} znaków.");
        }

        if (PageNumberParameter == PageSizeParameter)
        {
            violations.Set("pagination.pageSizeParameter", "Parametry numeru i rozmiaru strony muszą mieć różne nazwy.");
        }

        if (TotalPath is not null && (!JsonPathParser.TryParse(TotalPath, out var path) || path.IsCollection))
        {
            violations.Set("pagination.totalPath", "Ścieżka łącznej liczby elementów musi być poprawną ścieżką JSONPath bez wildcardu i filtra.");
        }
    }
}
