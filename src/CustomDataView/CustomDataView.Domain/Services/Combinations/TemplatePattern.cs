using System.Text;

namespace CustomDataView.Domain.Services.Combinations;

public sealed record TemplatePattern(IReadOnlyList<TemplatePattern.Part> Parts)
{
    public IReadOnlyList<string> Labels => Parts.Where(part => part.IsLabel).Select(part => part.Text).ToList();

    public static bool TryParse(string pattern, out TemplatePattern result)
    {
        result = null;
        if (pattern is null)
        {
            return false;
        }

        var parts = new List<Part>();
        var text = new StringBuilder();
        for (var index = 0; index < pattern.Length; index++)
        {
            var character = pattern[index];
            var doubled = index + 1 < pattern.Length && pattern[index + 1] == character;
            if (character is '{' or '}' && doubled)
            {
                text.Append(character);
                index++;
                continue;
            }

            if (character == '}')
            {
                return false;
            }

            if (character != '{')
            {
                text.Append(character);
                continue;
            }

            var end = pattern.IndexOf('}', index + 1);
            var label = end < 0 ? null : pattern[(index + 1)..end].Trim();
            if (string.IsNullOrEmpty(label) || label.Contains('{'))
            {
                return false;
            }

            parts.Add(new Part(text.ToString(), false));
            text.Clear();
            parts.Add(new Part(label, true));
            index = end;
        }

        parts.Add(new Part(text.ToString(), false));
        result = new TemplatePattern(parts.Where(part => part.IsLabel || part.Text.Length > 0).ToList());
        return true;
    }

    public sealed record Part(string Text, bool IsLabel);
}
