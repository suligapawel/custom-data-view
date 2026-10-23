namespace CustomDataView.Domain.Policies;

public static class TileNodeKeys
{
    public const string Parent = "parent";
    public const char PathSeparator = '/';
    public const int MaxLength = 50;

    public static bool IsValid(string key)
        => !string.IsNullOrWhiteSpace(key)
            && key.Length <= MaxLength
            && !key.Contains(PathSeparator)
            && key != Parent;
}
