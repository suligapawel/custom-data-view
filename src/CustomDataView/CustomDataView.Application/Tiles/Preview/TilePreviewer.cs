using System.Collections.Concurrent;
using System.Text.Json;
using System.Text.Json.Nodes;
using CustomDataView.Application.Connections.Calls;
using CustomDataView.Application.Connections.Validation;
using CustomDataView.Application.Exceptions;
using CustomDataView.Application.Tiles.Contracts;
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
    ITransformationService transformations,
    TileContracts contracts,
    TimeProvider time)
{
    public const int MaxItemInstances = 50;

    public async Task<TilePreview> Preview(Guid id, TilePreviewValues values, CancellationToken cancellationToken)
    {
        var tile = tiles.Get(id) ?? throw new NotFoundException(id);
        return await Preview(tile.Id, tile.Definition, values, cancellationToken);
    }

    public Task<TilePreview> Preview(TileDefinition definition, TilePreviewValues values, CancellationToken cancellationToken)
        => Preview(null, definition, values, cancellationToken);

    private async Task<TilePreview> Preview(
        Guid? id,
        TileDefinition definition,
        TilePreviewValues values,
        CancellationToken cancellationToken)
        => await PreviewInstance(id, definition, values.ToValues(), string.Empty, new PreviewContext(values, time, cancellationToken));

    private async Task<TilePreview> PreviewInstance(
        Guid? id,
        TileDefinition definition,
        TileValues delivered,
        string address,
        PreviewContext context)
    {
        if (definition.Display == TileDisplays.Form)
        {
            return new TilePreview(id, definition.Name, definition.Display, TileFormValues.Preview(definition.FormFields, context.Values.Form(address)));
        }

        var instance = new TileInstance(definition, delivered, address);
        var nodes = definition.Sources.Select(source => source.Key).Concat(definition.Children.Select(child => child.Key)).ToList();
        var edges = TileBindingsValidator.Edges(definition).ToList();
        if (definition.Items is not null)
        {
            edges.AddRange(definition.Children
                .Where(child => child.Scope == TileChildScopes.Item)
                .Select(child => (definition.Items.Source, child.Key)));
        }

        foreach (var wave in BindingGraph.Order(nodes, edges).Waves)
        {
            await Task.WhenAll(wave.Select(node => Execute(instance, node, context)));
        }

        return Build(id, instance);
    }

    private Task Execute(TileInstance instance, string node, PreviewContext context)
    {
        if (instance.Definition.Sources.FirstOrDefault(source => source.Key == node) is { } source)
        {
            return ExecuteSource(instance, source, context);
        }

        var child = instance.Definition.Children.Single(candidate => candidate.Key == node);
        return child.Scope == TileChildScopes.Item
            ? ExecuteItemChild(instance, child, context)
            : ExecuteChild(instance, child, null, context);
    }

    private async Task ExecuteSource(TileInstance instance, TileSource source, PreviewContext context)
    {
        if (source.Kind == TileSourceKinds.Input)
        {
            instance.Complete(source.Key, instance.Delivered.Inputs.TryGetValue(source.Key, out var input) && input is not null
                ? new SourceResult(input.DeepClone(), null)
                : SourceResult.Failed(new TileFieldError(
                    TileFieldErrorCodes.InvalidParameters,
                    Parameters: [new TileParameterError(source.Key, "Brak wartości wejścia danych.", false)])));
            return;
        }

        var gate = Gate(instance, source.Key, null);
        if (gate.Pending.Count > 0)
        {
            instance.Complete(source.Key, SourceResult.Failed(new TileFieldError(TileFieldErrorCodes.AwaitingInput, Pending: gate.Pending)));
            return;
        }

        if (gate.Failure is not null || gate.NotLoaded)
        {
            instance.Complete(source.Key, SourceResult.Failed(new TileFieldError(
                TileFieldErrorCodes.DependencyFailed,
                Cause: gate.Failure ?? new TileFieldErrorCause(null, null, TileFieldErrorCodes.AwaitingLoad))));
            return;
        }

        var connection = connections.Get(source.ConnectionId.Value);
        var declared = TileConnectionParameters.Names(connection.Definition);
        var undeclared = gate.Parameters.Keys.Where(name => !declared.Contains(name)).ToList();
        if (undeclared.Count > 0)
        {
            instance.Complete(source.Key, SourceResult.Failed(new TileFieldError(
                TileFieldErrorCodes.InvalidBinding,
                Parameters: undeclared
                    .Select(name => new TileParameterError(name, $"Wiązanie wskazuje parametr '{name}', którego połączenie nie deklaruje.", true))
                    .ToList())));
            return;
        }

        var values = instance.Delivered.Parameters
            .Where(pair => pair.Value is not null)
            .ToDictionary(pair => pair.Key, pair => JsonSerializer.SerializeToElement(pair.Value), StringComparer.Ordinal);
        foreach (var (name, value) in gate.Parameters)
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

        instance.Complete(
            source.Key,
            await Call(connection, TileParameterValues.For(connection.Definition, values), gate.Parameters.Keys, instance.Address.Length > 0, context));
    }

    private async Task ExecuteChild(TileInstance instance, TileChild child, ItemInstance item, PreviewContext context)
    {
        var address = item is null ? Address(instance.Address, child.Key) : $"{instance.Address}[{item.KeyText}]/{child.Key}";
        var gate = Gate(instance, child.Key, item);
        var values = Merge(instance.Delivered.DescendantsOf(child.Key), gate);
        TileEmbeddingPreview embedding;
        if (gate.Pending.Count > 0)
        {
            embedding = Embedding(child, address, TileEmbeddingStates.AwaitingInput, values, pending: gate.Pending);
        }
        else if (gate.Failure is not null)
        {
            embedding = Embedding(child, address, TileEmbeddingStates.Error, error: new TileFieldError(TileFieldErrorCodes.DependencyFailed, Cause: gate.Failure));
        }
        else if (gate.NotLoaded || child.Load == TileChildLoads.Lazy || (item is not null && item.Index >= MaxItemInstances))
        {
            embedding = Embedding(child, address, TileEmbeddingStates.AwaitingLoad, values);
        }
        else
        {
            embedding = await PreviewChild(child, values, address, context);
        }

        instance.Complete(child.Key, item, embedding);
    }

    private async Task<TileEmbeddingPreview> PreviewChild(TileChild child, TileValues values, string address, PreviewContext context)
    {
        var tile = tiles.Get(child.TileId);
        if (tile is null)
        {
            return Embedding(child, address, TileEmbeddingStates.Error, error: new TileFieldError(TileFieldErrorCodes.TileNotFound));
        }

        if (InvalidBinding(tile.Definition, values) is { } invalid)
        {
            return Embedding(child, address, TileEmbeddingStates.Error, error: invalid);
        }

        if (!context.TryStartEmbedding(tile.Definition.Sources.Count(source => source.Kind == TileSourceKinds.Connection)))
        {
            return Embedding(child, address, TileEmbeddingStates.AwaitingLoad, values);
        }

        var preview = await PreviewInstance(tile.Id, tile.Definition, values, address, context);
        return Embedding(child, address, TileEmbeddingStates.Data, tile: preview);
    }

    private async Task ExecuteItemChild(TileInstance instance, TileChild child, PreviewContext context)
    {
        if (instance.Items(PreviewField) is { } items)
        {
            await Task.WhenAll(items.Select(item => ExecuteChild(instance, child, item, context)));
        }
    }

    private TileFieldError InvalidBinding(TileDefinition definition, TileValues values)
    {
        var contract = contracts.Of(definition);
        var undeclared = values.Bound
            .Where(name => contract.Parameters.All(parameter => parameter.Name != name) && !contract.Inputs.Contains(name))
            .Select(name => new TileParameterError(name, $"Wiązanie wskazuje parametr '{name}', którego kafelek nie deklaruje.", true));
        var missing = values.Descendants
            .Where(pair => pair.Value.Bound.Count > 0)
            .Select(pair => pair.Key.Split(TileNodeKeys.PathSeparator)[0])
            .Where(key => definition.Children.All(child => child.Key != key))
            .Distinct()
            .Select(key => new TileParameterError(key, $"Wiązanie wskazuje osadzenie '{key}', którego kafelek nie zawiera.", true));
        var errors = undeclared.Concat(missing).ToList();
        return errors.Count == 0 ? null : new TileFieldError(TileFieldErrorCodes.InvalidBinding, Parameters: errors);
    }

    private BindingGate Gate(TileInstance instance, string node, ItemInstance item)
    {
        var gate = new BindingGate();
        foreach (var binding in instance.Definition.Bindings)
        {
            var segments = binding.To.Split(TileNodeKeys.PathSeparator, 2);
            if (segments[0] != node)
            {
                continue;
            }

            foreach (var mapping in binding.Map)
            {
                var value = Resolve(instance, binding.From, mapping, item);
                switch (value.Kind)
                {
                    case BoundValueKind.Pending:
                        gate.Pending.AddRange(value.Pending.Select(pending => pending.Parameter is null && pending.Input is null
                            ? pending with { Parameter = mapping.Parameter, Input = mapping.Input }
                            : pending));
                        break;
                    case BoundValueKind.Failed:
                        gate.Failure ??= value.Cause;
                        break;
                    case BoundValueKind.NotLoaded:
                        gate.NotLoaded = true;
                        break;
                    default:
                        gate.Set(segments.Length > 1 ? segments[1] : null, mapping, value.Value);
                        break;
                }
            }
        }

        return gate;
    }

    private BoundValue Resolve(TileInstance instance, string from, TileBindingMapping mapping, ItemInstance item)
    {
        var definition = instance.Definition;
        if (from == TileNodeKeys.Parent || definition.Sources.Any(source => source.Key == from))
        {
            var field = definition.Fields.First(candidate => TileLabels.AreEqual(candidate.Label, mapping.Field));
            if (definition.Items is null || field.Source != definition.Items.Source)
            {
                return Value(instance.Field(field, PreviewField), field.Source, instance.Address);
            }

            var items = instance.Items(PreviewField);
            if (items is null)
            {
                return BoundValue.Failed(new TileFieldErrorCause(field.Source, field.Label, instance.Result(field.Source).Error.Code));
            }

            return (mapping.Scope ?? BindingScopes.Selected) switch
            {
                BindingScopes.All => All(items.Select(candidate => candidate.Field(field.Label)), field.Source, field.Label),
                BindingScopes.Item when item is not null => Value(item.Field(field.Label), field.Source, instance.Address),
                _ => BoundValue.Waiting(new TilePendingBinding(instance.Address, field.Label, BindingScopes.Selected, null, null))
            };
        }

        var segments = from.Split(TileNodeKeys.PathSeparator);
        var child = definition.Children.Single(candidate => candidate.Key == segments[0]);
        var embedding = instance.Embedding(child, child.Scope == TileChildScopes.Item ? item : null);
        foreach (var segment in segments.Skip(1))
        {
            if (embedding.State != TileEmbeddingStates.Data)
            {
                break;
            }

            embedding = embedding.Tile.Children?.FirstOrDefault(candidate => candidate.Key == segment);
            if (embedding is null)
            {
                return BoundValue.Failed(new TileFieldErrorCause(from, mapping.Field, TileFieldErrorCodes.InvalidBinding));
            }
        }

        return embedding.State switch
        {
            TileEmbeddingStates.AwaitingInput => BoundValue.Waiting([.. embedding.Pending]),
            TileEmbeddingStates.AwaitingLoad => BoundValue.Unloaded,
            TileEmbeddingStates.Error => BoundValue.Failed(new TileFieldErrorCause(from, mapping.Field, embedding.Error.Code)),
            _ => FromTile(embedding, mapping)
        };
    }

    private static BoundValue FromTile(TileEmbeddingPreview embedding, TileBindingMapping mapping)
    {
        var tile = embedding.Tile;
        if (tile.ItemKey is null)
        {
            return Value(tile.Fields.First(candidate => TileLabels.AreEqual(candidate.Label, mapping.Field)), embedding.Key, embedding.Address);
        }

        if (tile.ItemsError is not null)
        {
            return BoundValue.Failed(new TileFieldErrorCause(embedding.Key, mapping.Field, tile.ItemsError.Code));
        }

        return mapping.Scope == BindingScopes.All
            ? All(tile.Items.Select(item => item.Fields.First(field => TileLabels.AreEqual(field.Label, mapping.Field))), embedding.Key, mapping.Field)
            : BoundValue.Waiting(new TilePendingBinding(embedding.Address, mapping.Field, BindingScopes.Selected, null, null));
    }

    private static BoundValue Value(TileFieldPreview field, string node, string address)
    {
        if (field.Error is not null)
        {
            return BoundValue.Failed(new TileFieldErrorCause(node, field.Label, field.Error.Code));
        }

        if (field.Form is { Required: true } && field.Value is null)
        {
            return BoundValue.Waiting(new TilePendingBinding(address, field.Label, null, null, null));
        }

        return BoundValue.Of(field.Value?.DeepClone());
    }

    private static BoundValue All(IEnumerable<TileFieldPreview> fields, string node, string label)
    {
        var values = new JsonArray();
        foreach (var field in fields)
        {
            if (field.Error is not null)
            {
                return BoundValue.Failed(new TileFieldErrorCause(node, label, field.Error.Code));
            }

            values.Add(field.Value?.DeepClone());
        }

        return BoundValue.Of(values);
    }

    private static TileValues Merge(TileValues delivered, BindingGate gate)
    {
        var descendants = delivered.Descendants.ToDictionary(pair => pair.Key, pair => pair.Value, StringComparer.Ordinal);
        foreach (var (path, values) in gate.Descendants)
        {
            descendants[path] = Overlay(descendants.GetValueOrDefault(path) ?? TileValues.Empty, values.Parameters, values.Inputs);
        }

        return Overlay(delivered, gate.Parameters, gate.Inputs) with { Descendants = descendants };
    }

    private static TileValues Overlay(
        TileValues values,
        IReadOnlyDictionary<string, JsonNode> parameters,
        IReadOnlyDictionary<string, JsonNode> inputs)
        => values with
        {
            Parameters = Overlay(values.Parameters, parameters),
            Inputs = Overlay(values.Inputs, inputs),
            Bound = new HashSet<string>([.. values.Bound, .. parameters.Keys, .. inputs.Keys], StringComparer.Ordinal)
        };

    private static Dictionary<string, JsonNode> Overlay(IReadOnlyDictionary<string, JsonNode> target, IReadOnlyDictionary<string, JsonNode> values)
    {
        var result = new Dictionary<string, JsonNode>(target, StringComparer.Ordinal);
        foreach (var (name, value) in values)
        {
            if (value is null)
            {
                result.Remove(name);
            }
            else
            {
                result[name] = value;
            }
        }

        return result;
    }

    private async Task<SourceResult> Call(
        Connection connection,
        ParameterValuesInput parameters,
        IEnumerable<string> bound,
        bool embedded,
        PreviewContext context)
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

        var values = parameters.ToValues();
        return SourceResult.Of(await context.Call(
            connection.Id,
            values,
            embedded,
            () => factory.Create(connection.Definition).Get(values, context.CancellationToken)));
    }

    private TilePreview Build(Guid? id, TileInstance instance)
    {
        var definition = instance.Definition;
        var children = definition.Children
            .Where(child => child.Scope == TileChildScopes.Tile)
            .OrderBy(child => child.Position)
            .Select(child => instance.Embedding(child, null))
            .ToList();
        var preview = new TilePreview(id, definition.Name, definition.Display, null)
        {
            Children = children.Count > 0 ? children : null
        };
        if (definition.Display == TileDisplays.Tabs)
        {
            return preview;
        }

        if (definition.Items is null)
        {
            return preview with { Fields = definition.Fields.Select(field => instance.Field(field, PreviewField)).ToList() };
        }

        preview = preview with { ItemKey = definition.Items.Key };
        var items = instance.Items(PreviewField);
        if (items is null)
        {
            return preview with { ItemsError = instance.Result(definition.Items.Source).Error };
        }

        var itemChildren = definition.Children
            .Where(child => child.Scope == TileChildScopes.Item)
            .OrderBy(child => child.Position)
            .ToList();
        return preview with
        {
            Items = items
                .Select(item => new TileItemPreview(item.Key?.DeepClone(), item.Fields)
                {
                    Children = itemChildren.Count > 0 ? itemChildren.Select(child => instance.Embedding(child, item)).ToList() : null
                })
                .ToList()
        };
    }

    private static TileEmbeddingPreview Embedding(
        TileChild child,
        string address,
        string state,
        TileValues inputs = null,
        IReadOnlyList<TilePendingBinding> pending = null,
        TileFieldError error = null,
        TilePreview tile = null)
        => new(child.Key, address, child.TileId, child.Position, child.CssClass, state, tile, inputs?.Compact(), pending, error)
        {
            Label = child.Label
        };

    private static string Address(string parent, string key) => parent.Length == 0 ? key : $"{parent}/{key}";

    private TileFieldPreview PreviewField(TileField field, SourceResult source)
        => source.Error is not null
            ? new TileFieldPreview(field.Label, null, source.Error, field.Hidden)
            : PreviewField(field, source.Body);

    private TileFieldPreview PreviewField(TileField field, JsonNode body)
    {
        JsonPathParser.TryParse(field.Path, out var path);
        var matches = path.Evaluate(body);
        if (!path.IsCollection && matches.Count == 0)
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
        return new TileFieldPreview(field.Label, path.IsCollection ? new JsonArray(values) : values.Single(), null, field.Hidden);
    }

    private sealed class TileInstance(TileDefinition definition, TileValues delivered, string address)
    {
        private readonly ConcurrentDictionary<string, SourceResult> results = new(StringComparer.Ordinal);
        private readonly ConcurrentDictionary<TileField, TileFieldPreview> fields = new();
        private readonly ConcurrentDictionary<(string, int), TileEmbeddingPreview> embeddings = new();
        private readonly Lock itemsLock = new();
        private IReadOnlyList<ItemInstance> items;
        private bool itemsComputed;

        public TileDefinition Definition { get; } = definition;

        public TileValues Delivered { get; } = delivered;

        public string Address { get; } = address;

        public void Complete(string source, SourceResult result) => results[source] = result;

        public void Complete(string child, ItemInstance item, TileEmbeddingPreview embedding) => embeddings[(child, item?.Index ?? -1)] = embedding;

        public SourceResult Result(string source) => results[source];

        public TileEmbeddingPreview Embedding(TileChild child, ItemInstance item) => embeddings[(child.Key, item?.Index ?? -1)];

        public TileFieldPreview Field(TileField field, Func<TileField, SourceResult, TileFieldPreview> preview)
            => fields.GetOrAdd(field, candidate => preview(candidate, results[candidate.Source]));

        public IReadOnlyList<ItemInstance> Items(Func<TileField, JsonNode, TileFieldPreview> preview)
        {
            lock (itemsLock)
            {
                if (itemsComputed)
                {
                    return items;
                }

                itemsComputed = true;
                var source = results[Definition.Items.Source];
                if (source.Error is not null)
                {
                    return items = null;
                }

                JsonPathParser.TryParse(Definition.Items.Path, out var path);
                var itemFields = Definition.Fields.Where(field => field.Source == Definition.Items.Source).ToList();
                return items = path.Evaluate(source.Body)
                    .Select((element, index) =>
                    {
                        var values = itemFields.Select(field => preview(field, element)).ToList();
                        var key = values.First(value => TileLabels.AreEqual(value.Label, Definition.Items.Key)).Value;
                        return new ItemInstance(index, key, values);
                    })
                    .ToList();
            }
        }
    }

    private sealed record ItemInstance(int Index, JsonNode Key, IReadOnlyList<TileFieldPreview> Fields)
    {
        public string KeyText => Key is JsonValue value && value.TryGetValue<string>(out var text) ? text : Key?.ToJsonString();

        public TileFieldPreview Field(string label) => Fields.First(field => TileLabels.AreEqual(field.Label, label));
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
