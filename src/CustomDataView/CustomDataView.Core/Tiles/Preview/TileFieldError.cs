using System.Text.Json.Serialization;

namespace CustomDataView.Core.Tiles.Preview;

public sealed record TileFieldError(
    string Code,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] string Transformation = null,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] string Reason = null,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] int? Status = null);
