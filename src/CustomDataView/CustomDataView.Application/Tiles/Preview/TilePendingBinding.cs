using System.Text.Json.Serialization;

namespace CustomDataView.Application.Tiles.Preview;

public sealed record TilePendingBinding(
    string From,
    string Field,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] string Scope,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] string Parameter,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] string Input);
