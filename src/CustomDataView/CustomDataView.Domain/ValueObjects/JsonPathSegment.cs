using System.Text.Json.Nodes;

namespace CustomDataView.Domain.ValueObjects;

public abstract record JsonPathSegment
{
    public sealed record Property(string Name) : JsonPathSegment;

    public sealed record Index(int Value) : JsonPathSegment;

    public sealed record Wildcard : JsonPathSegment;

    public sealed record Filter(JsonPath Operand, bool Negated, JsonNode Literal) : JsonPathSegment;
}
