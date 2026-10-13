using System.Text.Json;
using System.Text.Json.Nodes;
using CustomDataView.Core.Connections.Calls;
using CustomDataView.Core.Connections.Http;
using CustomDataView.Core.Connections.Persistence;
using CustomDataView.Core.Connections.Validation;
using CustomDataView.Core.Exceptions;
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
        var tile = tiles.Get(id) ?? throw new NotFoundException(id);
        return await Preview(tile.Id, tile.Definition, values, cancellationToken);
    }

    public Task<TilePreview> Preview(
        TileDefinition definition,
        IReadOnlyList<ConnectionValuesInput> values,
        CancellationToken cancellationToken)
        => Preview(null, definition, values, cancellationToken);

    private async Task<TilePreview> Preview(
        Guid? id,
        TileDefinition definition,
        IReadOnlyList<ConnectionValuesInput> values,
        CancellationToken cancellationToken)
    {
        var responses = await Call(definition, values, cancellationToken);
        return new TilePreview(
            id,
            definition.Name,
            definition.Fields.Select(field => PreviewField(field, responses[field.ConnectionId])).ToList());
    }

    private async Task<Dictionary<Guid, ExternalApiResult>> Call(
        TileDefinition definition,
        IReadOnlyList<ConnectionValuesInput> values,
        CancellationToken cancellationToken)
    {
        var used = definition.Fields
            .Select(field => field.ConnectionId)
            .Distinct()
            .Select(connectionId => (
                Connection: connections.Get(connectionId),
                Parameters: values.FirstOrDefault(value => value.ConnectionId == connectionId)?.Parameters ?? NoValues))
            .ToList();
        var errors = RepeatedConnections(values);
        foreach (var error in used
            .SelectMany(call => ParameterValuesValidator.Validate(call.Connection.Definition, call.Parameters)
                .Select(error => (Key: $"connections[{call.Connection.Id}].{error.Key}", error.Value))))
        {
            errors[error.Key] = error.Value;
        }

        if (errors.Count > 0)
        {
            throw new ValidationException(errors);
        }

        var results = await Task.WhenAll(used.Select(async call => (
            connectionId: call.Connection.Id,
            result: await factory.Create(call.Connection.Definition).Get(call.Parameters.ToValues(), cancellationToken))));
        return results.ToDictionary(call => call.connectionId, call => call.result);
    }

    private static Dictionary<string, string[]> RepeatedConnections(IReadOnlyList<ConnectionValuesInput> values)
    {
        var errors = new Dictionary<string, string[]>();
        for (var index = 0; index < values.Count; index++)
        {
            var connectionId = values[index].ConnectionId;
            if (values.Take(index).Any(previous => previous.ConnectionId == connectionId))
            {
                errors[$"connections[{index}].connectionId"] = ["Wartości tego połączenia zostały podane więcej niż raz."];
            }
        }

        return errors;
    }

    private TileFieldPreview PreviewField(TileField field, ExternalApiResult response)
    {
        if (!response.IsSuccess)
        {
            return new TileFieldPreview(field.Label, null, new TileFieldError(TileFieldErrorCodes.SourceUnavailable));
        }

        JsonPathParser.TryParse(field.Path, out var path);
        var matches = path.Evaluate(response.Body);
        if (path.HasWildcard)
        {
            return new TileFieldPreview(
                field.Label,
                new JsonArray(matches
                    .Select(match => TransformationValue.ToJson(
                        transformations.Apply(TransformationValue.FromJson(match), field.Transformations).Value))
                    .ToArray()));
        }

        var result = transformations.Apply(TransformationValue.FromJson(matches.Single()), field.Transformations);
        if (result.Error is { } error)
        {
            return new TileFieldPreview(field.Label, null, new TileFieldError(error.Code, error.Key, error.Reason));
        }

        return new TileFieldPreview(field.Label, TransformationValue.ToJson(result.Value));
    }
}
