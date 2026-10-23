using CustomDataView.Domain.Entities;
using CustomDataView.Domain.Repositories;
using CustomDataView.Domain.ValueObjects;
using CustomDataView.Infrastructure.Persistence.Rows;
using Microsoft.EntityFrameworkCore;

namespace CustomDataView.Infrastructure.Persistence.Repositories;

public sealed class PostgresViewRepository(IDbContextFactory<CustomDataViewDbContext> contextFactory) : IViewRepository
{
    public void Add(View view)
    {
        using var context = contextFactory.CreateDbContext();
        context.Views.Add(new ViewRow { Id = view.Id, Name = view.Definition.Name, Tiles = ToTileRows(view) });
        context.SaveChanges();
    }

    public View Get(Guid id)
    {
        using var context = contextFactory.CreateDbContext();
        var view = context.Views
            .AsNoTracking()
            .Include(stored => stored.Tiles.OrderBy(tile => tile.Position))
            .SingleOrDefault(stored => stored.Id == id);
        return view is null
            ? null
            : new View(
                view.Id,
                new ViewDefinition(
                    view.Name,
                    view.Tiles.Select(tile => new ViewTile(tile.TileId, tile.Position, tile.CssClass)).ToList()));
    }

    public IReadOnlyList<ViewSummary> GetAll()
    {
        using var context = contextFactory.CreateDbContext();
        return context.Views
            .AsNoTracking()
            .OrderBy(view => view.CreatedAt)
            .ThenBy(view => view.Id)
            .Select(view => new ViewSummary(view.Id, view.Name, view.Tiles.Count))
            .ToList();
    }

    public void Update(View view)
    {
        using var context = contextFactory.CreateDbContext();
        using var transaction = context.Database.BeginTransaction();
        context.Views
            .Where(stored => stored.Id == view.Id)
            .ExecuteUpdate(setters => setters
                .SetProperty(stored => stored.Name, view.Definition.Name)
                .SetProperty(stored => stored.UpdatedAt, stored => DateTime.UtcNow));
        context.ViewTiles.Where(tile => tile.ViewId == view.Id).ExecuteDelete();
        context.ViewTiles.AddRange(ToTileRows(view));
        context.SaveChanges();
        transaction.Commit();
    }

    public void Delete(Guid id)
    {
        using var context = contextFactory.CreateDbContext();
        context.Views.Where(view => view.Id == id).ExecuteDelete();
    }

    private static List<ViewTileRow> ToTileRows(View view)
        => view.Definition.Tiles
            .Select(tile => new ViewTileRow
            {
                ViewId = view.Id,
                TileId = tile.TileId,
                Position = tile.Position,
                CssClass = tile.CssClass
            })
            .ToList();
}
