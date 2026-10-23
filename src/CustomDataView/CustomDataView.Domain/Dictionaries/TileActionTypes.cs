namespace CustomDataView.Domain.Dictionaries;

public static class TileActionTypes
{
    public const string Button = "button";
    public const string Link = "link";

    public static readonly IReadOnlyList<string> All = [Button, Link];
}
