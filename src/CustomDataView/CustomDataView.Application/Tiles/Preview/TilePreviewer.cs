using System.Text.Json;
using System.Text.Json.Nodes;
using CustomDataView.Application.Connections.Calls;
using CustomDataView.Application.Connections.Validation;
using CustomDataView.Application.Exceptions;
using CustomDataView.Domain.Entities;
using CustomDataView.Domain.Policies;
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
        if (definition.Items is not null)
        {
            return PreviewItems(id, definition, responses[definition.Items.Source]);
        }

        return new TilePreview(
            id,
            definition.Name,
            definition.Display,
            definition.Fields.Select(field => PreviewField(field, responses[field.Source])).ToList());
    }

    private TilePreview PreviewItems(Guid? id, TileDefinition definition, SourceResult source)
    {
        var preview = new TilePreview(id, definition.Name, definition.Display, null, definition.Items.Key);
        if (Error(source) is { } error)
        {
            return preview with { ItemsError = error };
        }

        JsonPathParser.TryParse(definition.Items.Path, out var path);
        var fields = definition.Fields.Where(field => field.Source == definition.Items.Source).ToList();
        var items = path.Evaluate(source.Response.Body)
            .Select(element =>
            {
                var values = fields.Select(field => PreviewField(field, element)).ToList();
                var key = values.First(value => TileLabels.AreEqual(value.Label, definition.Items.Key)).Value?.DeepClone();
                return new TileItemPreview(key, values);
            })
            .ToList();
        return preview with { Items = items };
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
        => Error(source) is { } error
            ? new TileFieldPreview(field.Label, null, error, field.Hidden)
            : PreviewField(field, source.Response.Body);

    private static TileFieldError Error(SourceResult source)
    {
        if (source.Error is not null)
        {
            return source.Error;
        }

        var response = source.Response;
        if (response.IsSuccess)
        {
            return null;
        }

        var reason = response.ErrorCode == TileFieldErrorCodes.SourceUnavailable ? null : response.ErrorCode;
        return new TileFieldError(TileFieldErrorCodes.SourceUnavailable, Reason: reason, Status: response.Status);
    }

    private TileFieldPreview PreviewField(TileField field, JsonNode body)
    {
        JsonPathParser.TryParse(field.Path, out var path);
        var matches = path.Evaluate(body);
        if (!path.HasWildcard && matches.Count == 0)
        {
            return new TileFieldPreview(field.Label, null, new TileFieldError(TileFieldErrorCodes.PathNotFound), field.Hidden);
        }

        var results = matches
            .Select(match => transformations.Apply(TransformationValue.FromJson(match), field.Transformations))
            .ToList();
        if (results.FirstOrDefault(result => result.Error is not null)?.Error is { } error)
        {
            return new TileFieldPreview(field.Label, null, new TileFieldError(error.Code, error.Key, error.Reason), field.Hidden);
        }

        var values = results.Select(result => TransformationValue.ToJson(result.Value)).ToArray();
        return new TileFieldPreview(field.Label, path.HasWildcard ? new JsonArray(values) : values.Single(), null, field.Hidden);
    }

    private sealed record SourceResult(ExternalApiResult Response, TileFieldError Error);
}
