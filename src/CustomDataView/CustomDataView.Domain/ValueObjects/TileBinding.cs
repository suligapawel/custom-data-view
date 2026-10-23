namespace CustomDataView.Domain.ValueObjects;

public sealed record TileBinding(string From, string To, IReadOnlyList<TileBindingMapping> Map);
