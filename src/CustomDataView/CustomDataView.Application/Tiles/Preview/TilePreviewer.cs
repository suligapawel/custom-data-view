using System.Collections.Concurrent;
using System.Text.Json;
using System.Text.Json.Nodes;
using CustomDataView.Application.Connections.Calls;
using CustomDataView.Application.Connections.Validation;
using CustomDataView.Application.Exceptions;
using CustomDataView.Application.Tiles.Validation;
using CustomDataView.Domain.Dictionaries;
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
        TilePreviewValues values,
        CancellationToken cancellationToken)
    {
        var tile = tiles.Get(id) ?? throw new NotFoundException(id);
        return await Preview(tile.Id, tile.Definition, values, cancellationToken);
    }

    public Task<TilePreview> Preview(
        TileDefinition definition,
        TilePreviewValues values,
        CancellationToken cancellationToken)
        => Preview(null, definition, values, cancellationToken);

    private async Task<TilePreview> Preview(
        Guid? id,
        TileDefinition definition,
        TilePreviewValues values,
        CancellationToken cancellationToken)
    {
        var instance = new TileInstance(definition, values);
        var order = BindingGraph.Order(
            definition.Sources.Select(source => source.Key).ToList(),
            definition.Bindings.Select(binding => (binding.From, binding.To)));
        foreach (var wave in order.Waves)
        {
            await Task.WhenAll(wave.Select(key => Execute(instance, key, cancellationToken)));
        }

        if (definition.Items is not null)
        {
            return PreviewItems(id, instance);
        }

        return new TilePreview(
            id,
            definition.Name,
            definition.Display,
            definition.Fields.Select(field => instance.Field(field, PreviewField)).ToList());
    }

    private async Task Execute(TileInstance instance, string key, CancellationToken cancellationToken)
    {
        var source = instance.Definition.Sources.Single(candidate => candidate.Key == key);
        if (source.Kind == TileSourceKinds.Input)
        {
            instance.Complete(key, instance.Values.Inputs.TryGetValue(key, out var input)
                ? new SourceResult(JsonNode.Parse(input.GetRawText()), null)
                : SourceResult.Failed(new TileFieldError(
                    TileFieldErrorCodes.InvalidParameters,
                    Parameters: [new TileParameterError(key, "Brak wartości wejścia danych.", false)])));
            return;
        }

        var connection = connections.Get(source.ConnectionId.Value);
        var mappings = instance.Definition.Bindings
            .Where(binding => binding.To == key)
            .SelectMany(binding => binding.Map.Select(mapping => (binding.From, Mapping: mapping)))
            .ToList();
        var bound = new Dictionary<string, JsonNode>(StringComparer.Ordinal);
        foreach (var (from, mapping) in mappings)
        {
            var field = instance.Definition.Fields.First(candidate => TileLabels.AreEqual(candidate.Label, mapping.Field));
            var value = instance.Field(field, PreviewField);
            if (value.Error is not null)
            {
                instance.Complete(key, SourceResult.Failed(new TileFieldError(
                    TileFieldErrorCodes.DependencyFailed,
                    Cause: new TileFieldErrorCause(from, field.Label, value.Error.Code))));
                return;
            }

            bound[mapping.Parameter] = value.Value;
        }

        var declared = TileConnectionParameters.Names(connection.Definition);
        var undeclared = bound.Keys.Where(name => !declared.Contains(name)).ToList();
        if (undeclared.Count > 0)
        {
            instance.Complete(key, SourceResult.Failed(new TileFieldError(
                TileFieldErrorCodes.InvalidBinding,
                Parameters: undeclared
                    .Select(name => new TileParameterError(
                        name,
                        $"Wiązanie wskazuje parametr '{name}', którego połączenie nie deklaruje.",
                        true))
                    .ToList())));
            return;
        }

        var values = new Dictionary<string, JsonElement>(instance.Values.Parameters, StringComparer.Ordinal);
        foreach (var (name, value) in bound)
        {
            if (value is null)
            {
                values.Remove(name);
            }
            else
            {
                values[name] = JsonSerializer.SerializeToElement(value);
            }
        }

        instance.Complete(key, await Call(connection, TileParameterValues.For(connection.Definition, values), bound.Keys, cancellationToken));
    }

    private async Task<SourceResult> Call(
        Connection connection,
        ParameterValuesInput parameters,
        IEnumerable<string> bound,
        CancellationToken cancellationToken)
    {
        var errors = ParameterValuesValidator.Validate(connection.Definition, parameters);
        if (errors.Count > 0)
        {
            var boundNames = bound.ToHashSet(StringComparer.Ordinal);
            return SourceResult.Failed(new TileFieldError(
                TileFieldErrorCodes.InvalidParameters,
                Parameters: errors
                    .Select(error =>
                    {
                        var name = error.Key.Split('.', 3)[2];
                        return new TileParameterError(name, error.Value[0], boundNames.Contains(name));
                    })
                    .ToList()));
        }

        return SourceResult.Of(await factory.Create(connection.Definition).Get(parameters.ToValues(), cancellationToken));
    }

    private TilePreview PreviewItems(Guid? id, TileInstance instance)
    {
        var definition = instance.Definition;
        var preview = new TilePreview(id, definition.Name, definition.Display, null, definition.Items.Key);
        var source = instance.Result(definition.Items.Source);
        if (source.Error is not null)
        {
            return preview with { ItemsError = source.Error };
        }

        JsonPathParser.TryParse(definition.Items.Path, out var path);
        var fields = definition.Fields.Where(field => field.Source == definition.Items.Source).ToList();
        var items = path.Evaluate(source.Body)
            .Select(element =>
            {
                var values = fields.Select(field => PreviewField(field, element)).ToList();
                var key = values.First(value => TileLabels.AreEqual(value.Label, definition.Items.Key)).Value?.DeepClone();
                return new TileItemPreview(key, values);
            })
            .ToList();
        return preview with { Items = items };
    }

    private TileFieldPreview PreviewField(TileField field, SourceResult source)
        => source.Error is not null
            ? new TileFieldPreview(field.Label, null, source.Error, field.Hidden)
            : PreviewField(field, source.Body);

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

    private sealed class TileInstance(TileDefinition definition, TilePreviewValues values)
    {
        private readonly ConcurrentDictionary<string, SourceResult> results = new(StringComparer.Ordinal);
        private readonly ConcurrentDictionary<TileField, TileFieldPreview> fields = new();

        public TileDefinition Definition { get; } = definition;

        public TilePreviewValues Values { get; } = values;

        public void Complete(string source, SourceResult result) => results[source] = result;

        public SourceResult Result(string source) => results[source];

        public TileFieldPreview Field(TileField field, Func<TileField, SourceResult, TileFieldPreview> preview)
            => fields.GetOrAdd(field, candidate => preview(candidate, results[candidate.Source]));
    }

    private sealed record SourceResult(JsonNode Body, TileFieldError Error)
    {
        public static SourceResult Failed(TileFieldError error) => new(null, error);

        public static SourceResult Of(ExternalApiResult response)
        {
            if (response.IsSuccess)
            {
                return new SourceResult(response.Body, null);
            }

            var reason = response.ErrorCode == TileFieldErrorCodes.SourceUnavailable ? null : response.ErrorCode;
            return Failed(new TileFieldError(TileFieldErrorCodes.SourceUnavailable, Reason: reason, Status: response.Status));
        }
    }
}
