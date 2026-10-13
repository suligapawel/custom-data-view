namespace CustomDataView.Core.Transformations.Execution;

public sealed record TransformationError(string Code, string Key, string Reason);
