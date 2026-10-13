namespace CustomDataView.Core.Connections.Management;

public sealed record ConnectionSummaryDto(Guid Id, string Name, string Method, string Url);
