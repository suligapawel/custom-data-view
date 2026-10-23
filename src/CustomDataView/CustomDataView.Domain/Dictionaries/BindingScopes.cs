namespace CustomDataView.Domain.Dictionaries;

public static class BindingScopes
{
    public const string Selected = "selected";
    public const string All = "all";
    public const string Item = "item";

    public static readonly IReadOnlyList<string> Values = [Selected, All, Item];
}
