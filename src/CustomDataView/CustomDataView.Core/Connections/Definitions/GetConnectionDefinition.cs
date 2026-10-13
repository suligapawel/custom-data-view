namespace CustomDataView.Core.Connections.Definitions;

public sealed record GetConnectionDefinition(string Name, string Url, IReadOnlyList<NameValuePair> Headers)
    : ConnectionDefinition(Name, Url, Headers);
