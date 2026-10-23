using CustomDataView.Domain.ValueObjects;

namespace CustomDataView.Domain.Entities;

public sealed record View(Guid Id, ViewDefinition Definition);
