namespace CustomDataView.Core.Connections.Definitions;

public sealed record PostConnectionDefinition(string Name, string Url, IReadOnlyList<NameValuePair> Headers, string Body)
    : ConnectionDefinition(Name, Url, Headers);
