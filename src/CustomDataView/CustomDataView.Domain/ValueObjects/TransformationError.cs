namespace CustomDataView.Domain.ValueObjects;

public sealed record TransformationError(string Code, string Key, string Reason);
