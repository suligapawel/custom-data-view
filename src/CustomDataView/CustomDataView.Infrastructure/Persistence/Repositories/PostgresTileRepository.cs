using System.Text.Json.Nodes;
using CustomDataView.Domain.Entities;
using CustomDataView.Domain.Policies;
using CustomDataView.Domain.Repositories;
using CustomDataView.Domain.Services.Transformations;
using CustomDataView.Domain.ValueObjects;
using CustomDataView.Infrastructure.Persistence.Rows;
using Microsoft.EntityFrameworkCore;

namespace CustomDataView.Infrastructure.Persistence.Repositories;

public sealed class PostgresTileRepository(
    IDbContextFactory<CustomDataViewDbContext> contextFactory,
    IEnumerable<ITransformation> transformations)
    : ITileRepository
{
    private readonly Dictionary<string, ITransformation> catalog =
        transformations.ToDictionary(transformation => transformation.Key, StringComparer.Ordinal);

    public void Add(Tile tile)
    {
        using var context = contextFactory.CreateDbContext();
        context.Tiles.Add(new TileRow { Id = tile.Id, Name = tile.Definition.Name, Fields = ToFieldRows(tile) });
        context.SaveChanges();
    }

    public Tile Get(Guid id)
    {
        using var context = contextFactory.CreateDbContext();
        var tile = WithFields(context).SingleOrDefault(stored => stored.Id == id);
        return tile is null ? null : ToTile(tile);
    }

    public IReadOnlyList<Tile> GetAll()
    {
        using var context = contextFactory.CreateDbContext();
        return WithFields(context)
            .OrderBy(tile => tile.CreatedAt)
            .ThenBy(tile => tile.Id)
            .AsEnumerable()
            .Select(ToTile)
            .ToList();
    }

    public void Update(Tile tile)
    {
        using var context = contextFactory.CreateDbContext();
        using var transaction = context.Database.BeginTransaction();
        context.Tiles
            .Where(stored => stored.Id == tile.Id)
            .ExecuteUpdate(setters => setters
                .SetProperty(stored => stored.Name, tile.Definition.Name)
                .SetProperty(stored => stored.UpdatedAt, stored => DateTime.UtcNow));
        context.TileFields.Where(field => field.TileId == tile.Id).ExecuteDelete();
        context.TileFields.AddRange(ToFieldRows(tile));
        context.SaveChanges();
        transaction.Commit();
    }

    public void Delete(Guid id)
    {
        using var context = contextFactory.CreateDbContext();
        context.Tiles.Where(tile => tile.Id == id).ExecuteDelete();
    }

    public IReadOnlyList<TileReference> GetUsingConnection(Guid connectionId)
    {
        using var context = contextFactory.CreateDbContext();
        return context.Tiles
            .AsNoTracking()
            .Where(tile => tile.Fields.Any(field => field.ConnectionId == connectionId))
            .OrderBy(tile => tile.CreatedAt)
            .ThenBy(tile => tile.Id)
            .Select(tile => new TileReference(tile.Id, tile.Name))
            .ToList();
    }

    public IReadOnlyList<TileReference> GetReferences(IReadOnlyCollection<Guid> ids)
    {
        if (ids.Count == 0)
        {
            return [];
        }

        using var context = contextFactory.CreateDbContext();
        return context.Tiles
            .AsNoTracking()
            .Where(tile => ids.Contains(tile.Id))
            .Select(tile => new TileReference(tile.Id, tile.Name))
            .ToList();
    }

    private static IQueryable<TileRow> WithFields(CustomDataViewDbContext context)
        => context.Tiles
            .AsNoTracking()
            .Include(tile => tile.Fields.OrderBy(field => field.Position))
            .ThenInclude(field => field.Transformations.OrderBy(transformation => transformation.Position))
            .ThenInclude(transformation => transformation.Parameters.OrderBy(parameter => parameter.Name))
            .AsSplitQuery();

    private static List<TileFieldRow> ToFieldRows(Tile tile)
        => tile.Definition.Fields
            .Select((field, fieldPosition) => new TileFieldRow
            {
                TileId = tile.Id,
                Position = fieldPosition,
                ConnectionId = field.ConnectionId,
                Path = field.Path,
                Label = field.Label,
                Hidden = field.Hidden,
                Transformations = field.Transformations
                    .Select((step, position) => new TileFieldTransformationRow
                    {
                        TileId = tile.Id,
                        FieldPosition = fieldPosition,
                        Position = position,
                        Key = step.Key,
                        Parameters = (step.Parameters ?? new Dictionary<string, JsonNode>())
                            .Select(parameter => new TileFieldTransformationParameterRow
                            {
                                TileId = tile.Id,
                                FieldPosition = fieldPosition,
                                TransformationPosition = position,
                                Name = parameter.Key,
                                Value = TransformationParameterValues.ToText(parameter.Value)
                            })
                            .ToList()
                    })
                    .ToList()
            })
            .ToList();

    private Tile ToTile(TileRow tile)
        => new Tile(tile.Id, new TileDefinition(
            tile.Name,
            tile.Fields
                .Select(field => new TileField(
                    field.ConnectionId,
                    field.Path,
                    field.Label,
                    field.Transformations.Select(ToStep).ToList(),
                    field.Hidden))
                .ToList()));

    private TransformationStep ToStep(TileFieldTransformationRow step)
    {
        var transformation = catalog.GetValueOrDefault(step.Key);
        return new TransformationStep(
            step.Key,
            step.Parameters.ToDictionary(
                parameter => parameter.Name,
                parameter => TransformationParameterValues.FromText(transformation, parameter.Name, parameter.Value)));
    }
}
