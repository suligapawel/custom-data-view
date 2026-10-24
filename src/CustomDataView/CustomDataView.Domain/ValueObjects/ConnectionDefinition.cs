using CustomDataView.Domain.Policies;
using CustomDataView.Domain.Services;

namespace CustomDataView.Domain.ValueObjects;

public abstract record ConnectionDefinition(
    string Name,
    string Url,
    IReadOnlyList<ParameterDeclaration> PathParameters,
    IReadOnlyList<ParameterDeclaration> QueryParameters)
{
    public const int MaxNameLength = 100;
    protected const string RequiredWithDefaultMessage = "Parametr wymagany nie może mieć wartości domyślnej.";
    private const string RepeatedNameMessage = "Nazwa parametru powtarza się.";
    private const string SnakeCaseMessage = "Nazwa parametru musi być w formacie snake_case i mieć najwyżej 50 znaków.";

    public abstract string Method { get; }

    public virtual IReadOnlyList<ParameterDeclaration> Parameters => [.. PathParameters, .. QueryParameters];

    public IReadOnlySet<string> DeclaredNames => Parameters.Select(parameter => parameter.Name).ToHashSet(StringComparer.Ordinal);

    public IReadOnlyDictionary<string, string[]> Check()
    {
        var violations = new DefinitionViolations();
        if (string.IsNullOrWhiteSpace(Name) || Name.Length > MaxNameLength)
        {
            violations.Set("name", $"Nazwa połączenia jest wymagana i może mieć najwyżej {MaxNameLength} znaków.");
        }

        CheckUrl(violations);
        CheckQueryParameters(violations);
        CheckBody(violations);
        return violations.ToDictionary();
    }

    protected virtual void CheckBody(DefinitionViolations violations)
    {
    }

    protected static void CheckTemplateParameterName(
        IReadOnlyList<ParameterDeclaration> parameters,
        int index,
        IReadOnlyList<string> placeholders,
        string key,
        DefinitionViolations violations,
        string unusedMessage)
    {
        var name = parameters[index].Name;
        if (!ParameterNames.IsSnakeCase(name))
        {
            violations.Set($"{key}.name", SnakeCaseMessage);
        }
        else if (parameters.Take(index).Any(parameter => parameter.Name == name))
        {
            violations.Set($"{key}.name", RepeatedNameMessage);
        }
        else if (!placeholders.Contains(name))
        {
            violations.Set($"{key}.name", unusedMessage);
        }
    }

    protected static List<string> Undeclared(IReadOnlyList<string> placeholders, IReadOnlyList<ParameterDeclaration> parameters)
    {
        var declared = parameters.Select(parameter => parameter.Name).ToHashSet();
        return placeholders.Where(name => !declared.Contains(name)).ToList();
    }

    private void CheckUrl(DefinitionViolations violations)
    {
        if (!Uri.TryCreate(Url, UriKind.Absolute, out var url)
            || (url.Scheme != Uri.UriSchemeHttp && url.Scheme != Uri.UriSchemeHttps)
            || UrlTemplate.HasPlaceholdersOutsidePath(Url)
            || UrlTemplate.HasUnclosedBraces(Url))
        {
            violations.Set("url", "Adres połączenia musi być bezwzględnym adresem http lub https z parametrami tylko w ścieżce.");
            return;
        }

        var placeholders = UrlTemplate.PlaceholderNames(Url);
        var undeclared = Undeclared(placeholders, PathParameters);
        if (undeclared.Count > 0)
        {
            violations.Set("url", undeclared.Select(name => $"Parametr adresu \"{name}\" nie został zadeklarowany.").ToArray());
        }

        for (var index = 0; index < PathParameters.Count; index++)
        {
            var key = $"pathParameters[{index}]";
            CheckTemplateParameterName(PathParameters, index, placeholders, key, violations, "Parametr adresu nie występuje w adresie.");

            var parameter = PathParameters[index];
            if (parameter.IsRequiredWithDefault)
            {
                violations.Set($"{key}.defaultValue", RequiredWithDefaultMessage);
            }
            else if (!parameter.Required && string.IsNullOrEmpty(parameter.DefaultText))
            {
                violations.Set($"{key}.defaultValue", "Parametr adresu musi być wymagany albo mieć niepustą wartość domyślną.");
            }
        }
    }

    private void CheckQueryParameters(DefinitionViolations violations)
    {
        for (var index = 0; index < QueryParameters.Count; index++)
        {
            var parameter = QueryParameters[index];
            if (string.IsNullOrEmpty(parameter.Name))
            {
                violations.Set($"queryParameters[{index}].name", "Nazwa parametru zapytania jest wymagana.");
            }
            else if (QueryParameters.Take(index).Any(previous => previous.Name == parameter.Name))
            {
                violations.Set($"queryParameters[{index}].name", RepeatedNameMessage);
            }

            if (parameter.IsRequiredWithDefault)
            {
                violations.Set($"queryParameters[{index}].defaultValue", RequiredWithDefaultMessage);
            }
            else if (parameter.DefaultValue is not null && parameter.DefaultText is null)
            {
                violations.Set($"queryParameters[{index}].defaultValue", "Wartość domyślna parametru zapytania musi być tekstem.");
            }
        }
    }
}
