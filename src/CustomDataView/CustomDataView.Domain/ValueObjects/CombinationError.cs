namespace CustomDataView.Domain.ValueObjects;

public sealed record CombinationError(string Target, string Message)
{
    public const string ParameterTarget = "parameter";
    public const string FieldsTarget = "fields";
}
