using System.Text.Json.Serialization;

namespace CustomDataView.Application.Tiles.Dtos;

public sealed record TileActionDto(string Key, string Label, string Type, Guid TileId, string Scope, IReadOnlyList<TileActionMappingDto> Map);

public sealed record TileActionMappingDto(
    string Field,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] string Parameter,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] string Input);
