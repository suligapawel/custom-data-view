using System.Text.Json;

namespace CustomDataView.Core.Connections.Definitions;

public sealed record ParameterDeclarationInput(string Name, bool Required, JsonElement? DefaultValue);
