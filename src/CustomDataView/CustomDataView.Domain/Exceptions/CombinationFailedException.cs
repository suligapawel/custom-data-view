namespace CustomDataView.Domain.Exceptions;

public sealed class CombinationFailedException : Exception
{
    public CombinationFailedException(string key, string reason)
        : base($"Combination '{key}' failed: {reason}.")
    {
        Key = key;
        Reason = reason;
    }

    public string Key { get; }

    public string Reason { get; }
}
