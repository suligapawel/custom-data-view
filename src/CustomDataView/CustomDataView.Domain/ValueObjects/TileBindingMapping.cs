namespace CustomDataView.Domain.ValueObjects;

public sealed record TileBindingMapping(string Field, string Parameter, string Input, string Scope)
{
    public bool TargetsExactlyOne => string.IsNullOrWhiteSpace(Parameter) != string.IsNullOrWhiteSpace(Input);

    public string Target => Parameter ?? Input;

    public string TargetKind => Parameter is null ? "input" : "parameter";
}
