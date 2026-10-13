namespace CustomDataView.Core.Transformations;

public sealed class TransformationFailedException : Exception
{
    public TransformationFailedException(string key, string reason)
        : base($"Transformation '{key}' failed: {reason}.")
    {
        Key = key;
        Reason = reason;
    }

    public string Key { get; }

    public string Reason { get; }
}
