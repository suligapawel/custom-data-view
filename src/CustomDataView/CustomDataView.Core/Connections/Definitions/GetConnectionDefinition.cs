namespace CustomDataView.Core.Connections.Definitions;

public sealed record GetConnectionDefinition(string Name, string Url) : ConnectionDefinition(Name, Url);
