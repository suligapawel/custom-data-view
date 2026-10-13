namespace CustomDataView.Core.Transformations;

public sealed record TransformationResult(object Value, TransformationError Error = null);
