using System.Text.Json;
using System.Text.Json.Nodes;
using CustomDataView.Application.Connections.Calls;
using CustomDataView.Application.Connections.Validation;
using CustomDataView.Application.Exceptions;
using CustomDataView.Domain.Entities;
using CustomDataView.Domain.Repositories;
using CustomDataView.Domain.Services;
using CustomDataView.Domain.Services.Transformations;
using CustomDataView.Domain.ValueObjects;

namespace CustomDataView.Application.Tiles.Preview;

public sealed class TilePreviewer(
    ITileRepository tiles,
    IConnectionRepository connections,
    IConnectionFactory factory,
    ITransformationService transformations)
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
            definition.Fields
                .Select(field => PreviewField(field, responses[field.ConnectionId]) with { Hidden = field.Hidden })
                .ToList());
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
                errors[ParameterKey(error.Key)] = error.Value;
            }
        }

        if (errors.Count > 0)
        {
            throw new ValidationException(errors);
        }
    }

    private static string ParameterKey(string sectionKey) => $"parameters.{sectionKey.Split('.', 3)[2]}";

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
