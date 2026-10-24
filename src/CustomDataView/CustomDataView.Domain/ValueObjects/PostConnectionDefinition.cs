using CustomDataView.Domain.Dictionaries;
using CustomDataView.Domain.Policies;
using CustomDataView.Domain.Services;

namespace CustomDataView.Domain.ValueObjects;

public sealed record PostConnectionDefinition(
    string Name,
    string Url,
    IReadOnlyList<ParameterDeclaration> PathParameters,
    IReadOnlyList<ParameterDeclaration> QueryParameters,
    string Body,
    IReadOnlyList<ParameterDeclaration> BodyParameters)
    : ConnectionDefinition(Name, Url, PathParameters, QueryParameters)
{
    public override string Method => ConnectionMethods.Post;

    public override IReadOnlyList<ParameterDeclaration> Parameters => [.. base.Parameters, .. BodyParameters];

    protected override void CheckBody(DefinitionViolations violations)
    {
        var placeholders = Body is null ? [] : BodyTemplate.PlaceholderNames(Body);
        var undeclared = Undeclared(placeholders, BodyParameters);
        if (undeclared.Count > 0)
        {
            violations.Set("body", undeclared.Select(name => $"Parametr treści \"{name}\" nie został zadeklarowany.").ToArray());
        }

        for (var index = 0; index < BodyParameters.Count; index++)
        {
            CheckTemplateParameterName(
                BodyParameters,
                index,
                placeholders,
                $"bodyParameters[{index}]",
                violations,
                "Parametr treści nie występuje w treści.");

            if (BodyParameters[index].IsRequiredWithDefault)
            {
                violations.Set($"bodyParameters[{index}].defaultValue", RequiredWithDefaultMessage);
            }
        }
    }
}
