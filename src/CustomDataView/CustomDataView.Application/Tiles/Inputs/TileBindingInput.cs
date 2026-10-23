namespace CustomDataView.Application.Tiles.Inputs;

public sealed record TileBindingInput(string From, string To, IReadOnlyList<TileBindingMappingInput> Map);

public sealed record TileBindingMappingInput(string Field, string Parameter, string Input, string Scope);
