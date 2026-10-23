using System.Text.Json.Nodes;

namespace CustomDataView.Application.Tiles.Preview;

public enum BoundValueKind
{
    Value,
    Failed,
    Pending,
    NotLoaded
}

public sealed record BoundValue(BoundValueKind Kind, JsonNode Value, TileFieldErrorCause Cause, IReadOnlyList<TilePendingBinding> Pending)
{
    public static readonly BoundValue Unloaded = new(BoundValueKind.NotLoaded, null, null, []);

    public static BoundValue Of(JsonNode value) => new(BoundValueKind.Value, value, null, []);

    public static BoundValue Failed(TileFieldErrorCause cause) => new(BoundValueKind.Failed, null, cause, []);

    public static BoundValue Waiting(params TilePendingBinding[] pending) => new(BoundValueKind.Pending, null, null, pending);
}
