using System.Text.Json.Nodes;

namespace CustomDataView.Core.Connections.Management;

public sealed record ConnectionDto(Guid Id, JsonNode Body);
