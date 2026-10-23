namespace CustomDataView.Domain.ValueObjects;

public sealed record ParameterDeclaration(string Name, bool Required, string DefaultValue);
