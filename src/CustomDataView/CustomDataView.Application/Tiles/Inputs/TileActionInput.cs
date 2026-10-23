namespace CustomDataView.Application.Tiles.Inputs;

public sealed record TileActionInput(string Key, string Label, string Type, Guid? TileId, string Scope, IReadOnlyList<TileActionMappingInput> Map);

public sealed record TileActionMappingInput(string Field, string Parameter, string Input);
