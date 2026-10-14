using CustomDataView.Core.Exceptions;
using CustomDataView.Core.Tiles.Persistence;
using CustomDataView.Core.Views.Definitions;
using CustomDataView.Core.Views.Persistence;
using CustomDataView.Core.Views.Validation;

namespace CustomDataView.Core.Views.Management;

public sealed class ViewService(
    IViewRepository repository,
    ITileRepository tiles,
    ViewReferencesValidator referencesValidator) : IViewService
{
    public Guid Add(ViewDefinition definition)
    {
        ValidateReferences(definition);
        var view = new View(Guid.NewGuid(), definition);
        repository.Add(view);
        return view.Id;
    }

    public IReadOnlyList<ViewSummaryDto> GetAll()
        => repository.GetAll().Select(view => new ViewSummaryDto(view.Id, view.Name, view.TileCount)).ToList();

    public ViewDto Get(Guid id)
    {
        var view = repository.Get(id) ?? throw new NotFoundException(id);
        var names = tiles.GetReferences(view.Definition.Tiles.Select(tile => tile.TileId).ToList())
            .ToDictionary(tile => tile.Id, tile => tile.Name);
        return new ViewDto(
            view.Id,
            view.Definition.Name,
            view.Definition.Tiles
                .OrderBy(tile => tile.Position)
                .Select(tile => new ViewTileDto(tile.TileId, names[tile.TileId], tile.Position, tile.CssClass))
                .ToList());
    }

    public void Update(Guid id, ViewDefinition definition)
    {
        ValidateReferences(definition);
        repository.Update(new View(id, definition));
    }

    private void ValidateReferences(ViewDefinition definition)
    {
        var errors = referencesValidator.Validate(definition);
        if (errors.Count > 0)
        {
            throw new ValidationException(errors);
        }
    }
}
