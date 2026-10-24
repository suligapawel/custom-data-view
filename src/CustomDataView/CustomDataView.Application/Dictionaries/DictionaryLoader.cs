using System.Text.Json;
using CustomDataView.Application.Tiles.Preview;
using CustomDataView.Domain.Policies;
using CustomDataView.Domain.Repositories;
using CustomDataView.Domain.Services.Transformations;
using CustomDataView.Domain.ValueObjects;

namespace CustomDataView.Application.Dictionaries;

public sealed class DictionaryLoader(ITileRepository tiles, TilePreviewer previewer, MapperDictionaries dictionaries)
{
    public async Task LoadAll(CancellationToken cancellationToken)
    {
        var references = tiles.GetAll().SelectMany(tile => tile.Definition.DictionaryReferences()).Distinct();
        foreach (var group in references.GroupBy(reference => reference.TileId))
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
        if (preview.Items is null)
        {
            return;
        }

        foreach (var reference in references)
        {
            dictionaries.Set(reference, MapperDictionary.Build(preview.Items.Select(item => (
                Key: TransformationValue.ToText(null, TransformationValue.FromJson(item.Key)),
                Value: item.Fields.First(field => TileLabels.AreEqual(field.Label, reference.ValueLabel)).Value))));
        }
    }
}
