using CustomDataView.Domain.Policies;

namespace CustomDataView.Domain.ValueObjects;

public sealed record DictionaryReference(Guid TileId, string ValueLabel)
{
    private const int GuidLength = 36;

    public static bool TryParse(string parameter, out DictionaryReference reference)
    {
        reference = null;
        if (parameter is null
            || parameter.Length <= GuidLength
            || parameter[GuidLength] != ':'
            || string.IsNullOrWhiteSpace(parameter[(GuidLength + 1)..])
            || !Guid.TryParseExact(parameter[..GuidLength], "D", out var tileId))
        {
            return false;
        }

        reference = new DictionaryReference(tileId, parameter[(GuidLength + 1)..]);
        return true;
    }

    public bool Equals(DictionaryReference other)
        => other is not null && TileId == other.TileId && TileLabels.AreEqual(ValueLabel, other.ValueLabel);

    public override int GetHashCode() => HashCode.Combine(TileId, TileLabels.Normalize(ValueLabel));
}
