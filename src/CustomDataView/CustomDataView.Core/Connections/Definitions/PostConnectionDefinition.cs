namespace CustomDataView.Core.Connections.Definitions;

public sealed record PostConnectionDefinition(string Name, string Body) : ConnectionDefinition(Name);
