using System.Text.Json;

namespace CustomDataView.WebApi.Requests;

public sealed record ParameterDeclarationRequest(string Name, bool Required, JsonElement? DefaultValue);
