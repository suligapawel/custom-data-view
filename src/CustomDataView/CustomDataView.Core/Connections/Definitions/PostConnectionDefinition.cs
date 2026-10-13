namespace CustomDataView.Core.Connections.Definitions;

public sealed record PostConnectionDefinition(string Name, string Url, string Body) : ConnectionDefinition(Name, Url);
