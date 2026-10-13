using System.Text.Json.Nodes;

namespace CustomDataView.Core.Connections.Http;

public sealed record ExternalApiResult(int Status, long ElapsedMs, JsonNode Body);
