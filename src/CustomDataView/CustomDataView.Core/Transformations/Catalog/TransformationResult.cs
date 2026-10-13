namespace CustomDataView.Core.Transformations.Catalog;

public sealed record TransformationResult(object Value, TransformationError Error = null);
