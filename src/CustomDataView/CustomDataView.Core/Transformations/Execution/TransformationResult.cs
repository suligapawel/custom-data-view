namespace CustomDataView.Core.Transformations.Execution;

public sealed record TransformationResult(object Value, TransformationError Error = null);
