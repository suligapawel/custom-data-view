using CustomDataView.Domain.ValueObjects;

namespace CustomDataView.Domain.Entities;

public sealed record Connection(Guid Id, ConnectionDefinition Definition);
