using CustomDataView.Domain.Dictionaries;
using CustomDataView.Domain.Policies;

namespace CustomDataView.Domain.ValueObjects;

public sealed record TileChild(string Key, Guid TileId, int Position, string CssClass, string Scope, string Load, string Label = null)
{
    public void Check(string path, DefinitionViolations violations)
    {
        if (!TileNodeKeys.IsValid(Key))
        {
            violations.Set(
                $"{path}.key",
                $"Klucz dziecka jest wymagany, może mieć najwyżej {TileNodeKeys.MaxLength} znaków, nie może zawierać znaku '/' ani być słowem '{TileNodeKeys.Parent}'.");
        }

        if (TileId == Guid.Empty)
        {
            violations.Set($"{path}.tileId", "Kafelek dziecka jest wymagany.");
        }

        if (Position < 0)
        {
            violations.Set($"{path}.position", "Pozycja dziecka jest wymagana i nie może być mniejsza niż 0.");
        }

        if (!TileChildScopes.All.Contains(Scope))
        {
            violations.Set($"{path}.scope", $"Zakres dziecka musi mieć jedną z wartości: {string.Join(", ", TileChildScopes.All)}.");
        }

        if (!TileChildLoads.All.Contains(Load))
        {
            violations.Set($"{path}.load", $"Ładowanie dziecka musi mieć jedną z wartości: {string.Join(", ", TileChildLoads.All)}.");
        }

        if (Label?.Trim().Length > TileTexts.MaxLength)
        {
            violations.Set($"{path}.label", $"Tytuł osadzenia może mieć najwyżej {TileTexts.MaxLength} znaków.");
        }

        if (CssClass?.Trim().Length > TileTexts.MaxCssClassLength)
        {
            violations.Set($"{path}.cssClass", $"Klasa CSS może mieć najwyżej {TileTexts.MaxCssClassLength} znaków.");
        }
    }
}
