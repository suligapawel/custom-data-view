namespace CustomDataView.Domain.Exceptions;

public sealed class InvalidDefinitionException(IReadOnlyDictionary<string, string[]> violations)
    : Exception("Definition violates domain rules.")
{
    public IReadOnlyDictionary<string, string[]> Violations { get; } = violations;

    public static void ThrowIfAny(IReadOnlyDictionary<string, string[]> violations)
    {
        if (violations.Count > 0)
        {
            throw new InvalidDefinitionException(violations);
        }
    }
}
