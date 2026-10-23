namespace CustomDataView.Domain.Policies;

public static class TileLabels
{
    public static string Normalize(string label) => label?.Trim().ToLowerInvariant();

    public static bool AreEqual(string first, string second) => Normalize(first) == Normalize(second);
}
