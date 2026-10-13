namespace CustomDataView.Core.Transformations.Chain;

public sealed record TransformationChainResult(object Value, TransformationChainError Error = null);
