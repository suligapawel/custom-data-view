using System.Text.Json;
using System.Text.Json.Nodes;
using CustomDataView.Core.Connections.Calls;
using CustomDataView.Core.Connections.Definitions;
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
    public async Task<TilePreview> Preview(
        Guid id,
        IReadOnlyDictionary<string, JsonElement> parameters,
        CancellationToken cancellationToken)
    {
        var tile = tiles.Get(id) ?? throw new NotFoundException(id);
        return await Preview(tile.Id, tile.Definition, parameters, cancellationToken);
    }

    public Task<TilePreview> Preview(
        TileDefinition definition,
        IReadOnlyDictionary<string, JsonElement> parameters,
        CancellationToken cancellationToken)
        => Preview(null, definition, parameters, cancellationToken);

    private async Task<TilePreview> Preview(
        Guid? id,
        TileDefinition definition,
        IReadOnlyDictionary<string, JsonElement> parameters,
        CancellationToken cancellationToken)
    {
        var responses = await Call(definition, parameters, cancellationToken);
        return new TilePreview(
            id,
            definition.Name,
            definition.Fields.Select(field => PreviewField(field, responses[field.ConnectionId])).ToList());
    }

    private async Task<Dictionary<Guid, ExternalApiResult>> Call(
        TileDefinition definition,
        IReadOnlyDictionary<string, JsonElement> parameters,
        CancellationToken cancellationToken)
    {
        var used = definition.Fields
            .Select(field => field.ConnectionId)
            .Distinct()
            .Select(connections.Get)
            .Select(connection => (
                Connection: connection,
                Parameters: TileParameterValues.For(connection.Definition, parameters)))
            .ToList();
        ValidateValues(used);
        var results = await Task.WhenAll(used.Select(async call => (
            connectionId: call.Connection.Id,
            result: await factory.Create(call.Connection.Definition).Get(call.Parameters.ToValues(), cancellationToken))));
        return results.ToDictionary(call => call.connectionId, call => call.result);
    }

    private static void ValidateValues(IReadOnlyList<(Connection Connection, ParameterValuesInput Parameters)> used)
    {
        var errors = new Dictionary<string, string[]>();
        foreach (var (connection, parameters) in used)
        {
            foreach (var error in ParameterValuesValidator.Validate(connection.Definition, parameters))
            {
                errors[$"connections[{connection.Id}].{error.Key}"] = error.Value;
            }
        }

        if (errors.Count > 0)
        {
            throw new ValidationException(errors);
        }
    }

    private TileFieldPreview PreviewField(TileField field, ExternalApiResult response)
    {
        if (!response.IsSuccess)
        {
            var reason = response.ErrorCode == TileFieldErrorCodes.SourceUnavailable ? null : response.ErrorCode;
            return new TileFieldPreview(
                field.Label,
                null,
                new TileFieldError(TileFieldErrorCodes.SourceUnavailable, Reason: reason, Status: response.Status));
        }

        JsonPathParser.TryParse(field.Path, out var path);
        var matches = path.Evaluate(response.Body);
        if (!path.HasWildcard && matches.Count == 0)
        {
            return new TileFieldPreview(field.Label, null, new TileFieldError(TileFieldErrorCodes.PathNotFound));
        }

        var results = matches
            .Select(match => transformations.Apply(TransformationValue.FromJson(match), field.Transformations))
            .ToList();
        if (results.FirstOrDefault(result => result.Error is not null)?.Error is { } error)
        {
            return new TileFieldPreview(field.Label, null, new TileFieldError(error.Code, error.Key, error.Reason));
        }

        var values = results.Select(result => TransformationValue.ToJson(result.Value)).ToArray();
        return new TileFieldPreview(field.Label, path.HasWildcard ? new JsonArray(values) : values.Single());
    }
}
