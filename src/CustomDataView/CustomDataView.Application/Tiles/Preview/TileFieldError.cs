using System.Text.Json.Serialization;

namespace CustomDataView.Application.Tiles.Preview;

public sealed record TileFieldError(
    string Code,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] string Transformation = null,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] string Reason = null,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] int? Status = null,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] IReadOnlyList<TileParameterError> Parameters = null,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] TileFieldErrorCause Cause = null,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] IReadOnlyList<TilePendingBinding> Pending = null)
{
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string Combination { get; init; }
}
