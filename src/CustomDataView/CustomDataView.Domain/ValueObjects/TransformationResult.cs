namespace CustomDataView.Domain.ValueObjects;

public sealed record TransformationResult(object Value, TransformationError Error = null);
