namespace CustomDataView.Domain.Dictionaries;

public static class TileFieldRoles
{
    public const string Title = "title";
    public const string Subtitle = "subtitle";
    public const string Badge = "badge";
    public const string Primary = "primary";
    public const string Secondary = "secondary";

    public static IReadOnlyList<string> For(string display)
        => display switch
        {
            TileDisplays.List => [Title, Subtitle, Badge],
            TileDisplays.Summary => [Primary, Secondary],
            _ => []
        };
}
