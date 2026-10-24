using System.Text.Json;
using CustomDataView.Application.Tiles.Preview;
using CustomDataView.Domain.Policies;
using CustomDataView.Domain.Repositories;
using CustomDataView.Domain.Services.Transformations;
using CustomDataView.Domain.ValueObjects;

namespace CustomDataView.Application.Dictionaries;

public sealed class DictionaryLoader(ITileRepository tiles, TilePreviewer previewer, MapperDictionaries dictionaries)
{
    public Task LoadAll(CancellationToken cancellationToken)
        => Load(tiles.GetAll().SelectMany(tile => tile.Definition.DictionaryReferences()), cancellationToken);

    public async Task Load(IEnumerable<DictionaryReference> references, CancellationToken cancellationToken)
    {
        foreach (var group in references.Distinct().GroupBy(reference => reference.TileId))
        {
            await Load(group.Key, group.ToList(), cancellationToken);
        }
    }

    private async Task Load(Guid tileId, IReadOnlyList<DictionaryReference> references, CancellationToken cancellationToken)
    {
        var definition = tiles.Get(tileId).Definition;
        var preview = await previewer.Preview(
            definition.AsDictionarySource(),
            TilePreviewValues.Of(new Dictionary<string, JsonElement>()),
            cancellationToken);
        var items = preview.Items is null || preview.Items.Any(item => Field(item, definition.Items.Key).Error is not null)
            ? null
            : preview.Items;
        foreach (var reference in references)
        {
            if (Build(items, reference) is { } dictionary)
            {
                dictionaries.Set(reference, dictionary);
            }
            else
            {
                dictionaries.Remove(reference);
            }
        }
    }

    private static MapperDictionary Build(IReadOnlyList<TileItemPreview> items, DictionaryReference reference)
    {
        if (items is null || items.Any(item => Field(item, reference.ValueLabel).Error is not null))
        {
            return null;
        }

        var entries = items.Where(item => item.Key is not null).Select(item => (
            Key: TransformationValue.FromJson(item.Key),
            Value: Field(item, reference.ValueLabel).Value));
        return MapperDictionary.TryBuild(entries, out var dictionary) ? dictionary : null;
    }

    private static TileFieldPreview Field(TileItemPreview item, string label)
        => item.Fields.First(field => TileLabels.AreEqual(field.Label, label));
}
