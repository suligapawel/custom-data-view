namespace CustomDataView.Core.Transformations.Abstractions;

public sealed class TransformationFailedException : Exception
{
    public TransformationFailedException(string key, string reason, string parameter = null)
        : base($"Transformation '{key}' failed: {reason}.")
    {
        Key = key;
        Reason = reason;
        Parameter = parameter;
    }

    public string Key { get; }

    public string Reason { get; }

    public string Parameter { get; }
}
