namespace CustomDataView.Core.Transformations;

public sealed record TransformationError(string Code, string Key, string Reason);
