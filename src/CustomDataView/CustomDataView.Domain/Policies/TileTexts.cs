namespace CustomDataView.Domain.Policies;

public static class TileTexts
{
    public const int MaxLength = 100;
    public const int MaxCssClassLength = 200;

    public static bool IsValid(string text) => !string.IsNullOrWhiteSpace(text) && text.Length <= MaxLength;
}
