using System.Text.Json;

namespace CustomDataView.Api.Requests;

public sealed record ParameterDeclarationRequest(string Name, bool Required, JsonElement? DefaultValue);
