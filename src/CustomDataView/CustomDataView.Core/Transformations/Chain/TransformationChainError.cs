namespace CustomDataView.Core.Transformations.Chain;

public sealed record TransformationChainError(string Code, string Key, string Reason);
