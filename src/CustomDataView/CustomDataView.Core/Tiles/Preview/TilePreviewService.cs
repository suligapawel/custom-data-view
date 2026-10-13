using System.Text.Json;
using System.Text.Json.Nodes;
using CustomDataView.Core.Connections.Calls;
using CustomDataView.Core.Connections.Http;
using CustomDataView.Core.Connections.Persistence;
using CustomDataView.Core.Tiles.Definitions;
using CustomDataView.Core.Tiles.Paths;
using CustomDataView.Core.Tiles.Persistence;
using CustomDataView.Core.Transformations.Abstractions;
using CustomDataView.Core.Transformations.Execution;

namespace CustomDataView.Core.Tiles.Preview;

public sealed class TilePreviewService(
    ITileRepository tiles,
    IConnectionRepository connections,
    IConnectionFactory factory,
    ITransformationService transformations) : ITilePreviewService
{
    private static readonly ParameterValuesInput NoValues = new(
        new Dictionary<string, JsonElement>(),
        new Dictionary<string, JsonElement>(),
        new Dictionary<string, JsonElement>());

    public async Task<TilePreview> Preview(
        Guid id,
        IReadOnlyList<ConnectionValuesInput> values,
        CancellationToken cancellationToken)
    {
        var tile = tiles.Get(id);
        var definition = tile.Definition;
        var responses = await Call(definition, values, cancellationToken);
        return new TilePreview(
            tile.Id,
            definition.Name,
            definition.Fields.Select(field => PreviewField(field, responses[field.ConnectionId])).ToList());
    }

    private async Task<Dictionary<Guid, ExternalApiResult>> Call(
        TileDefinition definition,
        IReadOnlyList<ConnectionValuesInput> values,
        CancellationToken cancellationToken)
    {
        var calls = definition.Fields
            .Select(field => field.ConnectionId)
            .Distinct()
            .Select(async connectionId =>
            {
                var connection = connections.Get(connectionId);
                var parameters = values.FirstOrDefault(value => value.ConnectionId == connectionId)?.Parameters ?? NoValues;
                var result = await factory.Create(connection.Definition).Get(parameters.ToValues(), cancellationToken);
                return (connectionId, result);
            });
        var results = await Task.WhenAll(calls);
        return results.ToDictionary(call => call.connectionId, call => call.result);
    }

    private TileFieldPreview PreviewField(TileField field, ExternalApiResult response)
    {
        JsonPathParser.TryParse(field.Path, out var path);
        var value = path.Evaluate(response.Body).Single();
        var result = transformations.Apply(TransformationValue.FromJson(value), field.Transformations);
        return new TileFieldPreview(field.Label, TransformationValue.ToJson(result.Value));
    }
}
