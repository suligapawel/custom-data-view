using CustomDataView.Domain.Dictionaries;

namespace CustomDataView.Domain.ValueObjects;

public sealed record GetConnectionDefinition(
    string Name,
    string Url,
    IReadOnlyList<ParameterDeclaration> PathParameters,
    IReadOnlyList<ParameterDeclaration> QueryParameters)
    : ConnectionDefinition(Name, Url, PathParameters, QueryParameters)
{
    public override string Method => ConnectionMethods.Get;
}
