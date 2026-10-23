namespace CustomDataView.Application.Connections.Dtos;

public sealed record ConnectionSummaryDto(Guid Id, string Name, string Method, string Url);
