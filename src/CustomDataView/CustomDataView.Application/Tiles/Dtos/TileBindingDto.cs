using System.Text.Json.Serialization;

namespace CustomDataView.Application.Tiles.Dtos;

public sealed record TileBindingDto(string From, string To, IReadOnlyList<TileBindingMappingDto> Map);

public sealed record TileBindingMappingDto(
    string Field,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] string Parameter,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] string Input,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] string Scope);
