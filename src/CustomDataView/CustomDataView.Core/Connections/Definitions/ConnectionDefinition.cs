namespace CustomDataView.Core.Connections.Definitions;

public abstract record ConnectionDefinition(string Name, string Url, IReadOnlyList<NameValuePair> Headers);
