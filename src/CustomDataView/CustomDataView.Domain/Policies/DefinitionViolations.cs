namespace CustomDataView.Domain.Policies;

public sealed class DefinitionViolations
{
    private readonly Dictionary<string, string[]> violations = new(StringComparer.Ordinal);

    public int Count => violations.Count;

    public bool Contains(string path) => violations.ContainsKey(path);

    public void Set(string path, params string[] messages)
    {
        violations[path] = messages;
    }

    public void TryAdd(string path, params string[] messages)
    {
        violations.TryAdd(path, messages);
    }

    public IReadOnlyDictionary<string, string[]> ToDictionary() => new Dictionary<string, string[]>(violations, StringComparer.Ordinal);
}
