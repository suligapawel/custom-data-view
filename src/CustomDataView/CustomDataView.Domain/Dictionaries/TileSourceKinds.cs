namespace CustomDataView.Domain.Dictionaries;

public static class TileSourceKinds
{
    public const string Connection = "connection";
    public const string Input = "input";

    public static readonly IReadOnlyList<string> All = [Connection, Input];
}
