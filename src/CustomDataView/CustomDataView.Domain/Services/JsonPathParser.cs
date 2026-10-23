using System.Globalization;
using System.Text;
using System.Text.Json.Nodes;
using CustomDataView.Domain.ValueObjects;

namespace CustomDataView.Domain.Services;

public static class JsonPathParser
{
    private const char Root = '$';
    private const char Current = '@';

    public static bool TryParse(string text, out JsonPath path)
    {
        path = null;
        if (string.IsNullOrEmpty(text) || text[0] != Root)
        {
            return false;
        }

        var position = 1;
        var segments = ReadSegments(text, ref position);
        if (segments is null || position != text.Length)
        {
            return false;
        }

        path = new JsonPath(segments);
        return true;
    }

    private static List<JsonPathSegment> ReadSegments(string text, ref int position)
    {
        var segments = new List<JsonPathSegment>();
        while (position < text.Length && text[position] is '.' or '[')
        {
            var segment = text[position] == '.' ? ReadDotSegment(text, ref position) : ReadBracketSegment(text, ref position);
            if (segment is null)
            {
                return null;
            }

            segments.Add(segment);
        }

        return segments;
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
            '?' => ReadFilter(text, ref position),
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

    private static JsonPathSegment ReadFilter(string text, ref int position)
    {
        position++;
        SkipSpaces(text, ref position);
        var parenthesized = Next(text, position) == '(';
        if (parenthesized)
        {
            position++;
            SkipSpaces(text, ref position);
        }

        if (Next(text, position) != Current)
        {
            return null;
        }

        position++;
        var operand = ReadSegments(text, ref position);
        if (operand is null || operand.Any(segment => segment is not (JsonPathSegment.Property or JsonPathSegment.Index)))
        {
            return null;
        }

        SkipSpaces(text, ref position);
        if (!TryReadOperator(text, ref position, out var negated))
        {
            return null;
        }

        SkipSpaces(text, ref position);
        if (!TryReadLiteral(text, ref position, out var literal))
        {
            return null;
        }

        SkipSpaces(text, ref position);
        if (parenthesized)
        {
            if (Next(text, position) != ')')
            {
                return null;
            }

            position++;
            SkipSpaces(text, ref position);
        }

        return new JsonPathSegment.Filter(new JsonPath(operand), negated, literal);
    }

    private static bool TryReadOperator(string text, ref int position, out bool negated)
    {
        negated = Next(text, position) == '!';
        if (Next(text, position) is not ('=' or '!') || Next(text, position + 1) != '=')
        {
            return false;
        }

        position += 2;
        return true;
    }

    private static bool TryReadLiteral(string text, ref int position, out JsonNode literal)
    {
        literal = null;
        var next = Next(text, position);
        if (next is '\'' or '"')
        {
            var value = ReadQuoted(text, ref position);
            literal = value is null ? null : JsonValue.Create(value);
            return value is not null;
        }

        if (TryReadKeyword(text, ref position, "true"))
        {
            literal = JsonValue.Create(true);
            return true;
        }

        if (TryReadKeyword(text, ref position, "false"))
        {
            literal = JsonValue.Create(false);
            return true;
        }

        if (TryReadKeyword(text, ref position, "null"))
        {
            return true;
        }

        return TryReadNumber(text, ref position, out literal);
    }

    private static bool TryReadKeyword(string text, ref int position, string keyword)
    {
        if (string.CompareOrdinal(text, position, keyword, 0, keyword.Length) != 0
            || IsNameCharacter(Next(text, position + keyword.Length), false))
        {
            return false;
        }

        position += keyword.Length;
        return true;
    }

    private static bool TryReadNumber(string text, ref int position, out JsonNode literal)
    {
        literal = null;
        var start = position;
        if (Next(text, position) == '-')
        {
            position++;
        }

        if (!TryReadDigits(text, ref position, out var integer) || (integer.Length > 1 && integer[0] == '0'))
        {
            return false;
        }

        if (Next(text, position) == '.')
        {
            position++;
            if (!TryReadDigits(text, ref position, out _))
            {
                return false;
            }
        }

        if (Next(text, position) is 'e' or 'E')
        {
            position++;
            if (Next(text, position) is '+' or '-')
            {
                position++;
            }

            if (!TryReadDigits(text, ref position, out _))
            {
                return false;
            }
        }

        var number = text[start..position];
        if (decimal.TryParse(number, NumberStyles.Float, CultureInfo.InvariantCulture, out var exact))
        {
            literal = JsonValue.Create(exact);
            return true;
        }

        if (double.TryParse(number, NumberStyles.Float, CultureInfo.InvariantCulture, out var approximate) && double.IsFinite(approximate))
        {
            literal = JsonValue.Create(approximate);
            return true;
        }

        return false;
    }

    private static bool TryReadDigits(string text, ref int position, out string digits)
    {
        var start = position;
        while (position < text.Length && char.IsAsciiDigit(text[position]))
        {
            position++;
        }

        digits = text[start..position];
        return position > start;
    }

    private static JsonPathSegment ReadQuotedName(string text, ref int position)
    {
        var name = ReadQuoted(text, ref position);
        return name is null ? null : new JsonPathSegment.Property(name);
    }

    private static string ReadQuoted(string text, ref int position)
    {
        var quote = text[position++];
        var value = new StringBuilder();
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

            value.Append(text[position++]);
        }

        if (position >= text.Length)
        {
            return null;
        }

        position++;
        return value.ToString();
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

    private static void SkipSpaces(string text, ref int position)
    {
        while (position < text.Length && text[position] == ' ')
        {
            position++;
        }
    }

    private static char Next(string text, int position) => position < text.Length ? text[position] : '\0';

    private static bool IsNameCharacter(char character, bool first)
        => char.IsAsciiLetter(character) || character == '_' || (!first && char.IsAsciiDigit(character));
}
