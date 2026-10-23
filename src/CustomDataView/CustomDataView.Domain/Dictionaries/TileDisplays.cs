namespace CustomDataView.Domain.Dictionaries;

public static class TileDisplays
{
    public const string Table = "table";
    public const string List = "list";
    public const string Details = "details";
    public const string Summary = "summary";
    public const string Form = "form";
    public const string Tabs = "tabs";
    public const string PaginationTable = "pagination_table";

    public static readonly IReadOnlyList<string> All = [Table, List, Details, Summary, Form, Tabs, PaginationTable];

    public static bool HasItems(string display) => display is Table or List or PaginationTable;
}
