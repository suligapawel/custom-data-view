using System.Globalization;
using System.Text;
using CustomDataView.Domain.ValueObjects;

namespace CustomDataView.Domain.Services;

public static class JsonPathParser
{
    private const char Root = '$';

    public static bool TryParse(string text, out JsonPath path)
    {
        path = null;
        if (string.IsNullOrEmpty(text) || text[0] != Root)
        {
            return false;
        }

        var segments = new List<JsonPathSegment>();
        var position = 1;
        while (position < text.Length)
        {
            var segment = text[position] switch
            {
                '.' => ReadDotSegment(text, ref position),
                '[' => ReadBracketSegment(text, ref position),
                _ => null
            };
            if (segment is null)
            {
                return false;
            }

            segments.Add(segment);
        }

        path = new JsonPath(segments);
        return true;
    }

    private static JsonPathSegment ReadDotSegment(string text, ref int position)
    {
        position++;
        if (position < text.Length && text[position] == '*')
        {
            position++;
            return new JsonPathSegment.Wildcard();
        }

        var start = position;
        while (position < text.Length && IsNameCharacter(text[position], position == start))
        {
            position++;
        }

        return position == start ? null : new JsonPathSegment.Property(text[start..position]);
    }

    private static JsonPathSegment ReadBracketSegment(string text, ref int position)
    {
        position++;
        if (position >= text.Length)
        {
            return null;
        }

        var segment = text[position] switch
        {
            '*' => ReadWildcard(ref position),
            '\'' or '"' => ReadQuotedName(text, ref position),
            _ => ReadIndex(text, ref position)
        };
        if (segment is null || position >= text.Length || text[position] != ']')
        {
            return null;
        }

        position++;
        return segment;
    }

    private static JsonPathSegment ReadWildcard(ref int position)
    {
        position++;
        return new JsonPathSegment.Wildcard();
    }

    private static JsonPathSegment ReadQuotedName(string text, ref int position)
    {
        var quote = text[position++];
        var name = new StringBuilder();
        while (position < text.Length && text[position] != quote)
        {
            if (text[position] == '\\')
            {
                position++;
                if (position >= text.Length || text[position] is not ('\'' or '"' or '\\'))
                {
                    return null;
                }
            }

            name.Append(text[position++]);
        }

        if (position >= text.Length)
        {
            return null;
        }

        position++;
        return new JsonPathSegment.Property(name.ToString());
    }

    private static JsonPathSegment ReadIndex(string text, ref int position)
    {
        var start = position;
        if (text[position] == '-')
        {
            position++;
        }

        var digitsStart = position;
        while (position < text.Length && char.IsAsciiDigit(text[position]))
        {
            position++;
        }

        return position > digitsStart
            && int.TryParse(text[start..position], NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out var index)
                ? new JsonPathSegment.Index(index)
                : null;
    }

    private static bool IsNameCharacter(char character, bool first)
        => char.IsAsciiLetter(character) || character == '_' || (!first && char.IsAsciiDigit(character));
}
