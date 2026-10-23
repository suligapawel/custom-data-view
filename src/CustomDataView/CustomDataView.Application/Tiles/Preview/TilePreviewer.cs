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
                .Select(field => PreviewField(field, responses[field.Source]) with { Hidden = field.Hidden })
                .ToList());
    }

    private async Task<Dictionary<string, SourceResult>> Call(
        TileDefinition definition,
        IReadOnlyDictionary<string, JsonElement> parameters,
        CancellationToken cancellationToken)
    {
        var results = await Task.WhenAll(definition.Sources.Select(async source =>
        {
            var connection = connections.Get(source.ConnectionId.Value);
            return (
                source: source.Key,
                result: await Call(connection, TileParameterValues.For(connection.Definition, parameters), cancellationToken));
        }));
        return results.ToDictionary(call => call.source, call => call.result);
    }

    private async Task<SourceResult> Call(Connection connection, ParameterValuesInput parameters, CancellationToken cancellationToken)
    {
        var errors = ParameterValuesValidator.Validate(connection.Definition, parameters);
        if (errors.Count > 0)
        {
            return new SourceResult(null, new TileFieldError(
                TileFieldErrorCodes.InvalidParameters,
                Parameters: errors
                    .Select(error => new TileParameterError(error.Key.Split('.', 3)[2], error.Value[0], false))
                    .ToList()));
        }

        return new SourceResult(await factory.Create(connection.Definition).Get(parameters.ToValues(), cancellationToken), null);
    }

    private TileFieldPreview PreviewField(TileField field, SourceResult source)
    {
        if (source.Error is not null)
        {
            return new TileFieldPreview(field.Label, null, source.Error);
        }

        var response = source.Response;
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

    private sealed record SourceResult(ExternalApiResult Response, TileFieldError Error);
}
