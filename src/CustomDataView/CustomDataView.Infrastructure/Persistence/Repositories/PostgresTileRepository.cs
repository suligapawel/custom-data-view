using System.Text.Json.Nodes;
using CustomDataView.Domain.Entities;
using CustomDataView.Domain.Repositories;
using CustomDataView.Domain.ValueObjects;
using CustomDataView.Infrastructure.Persistence.Rows;
using Microsoft.EntityFrameworkCore;

namespace CustomDataView.Infrastructure.Persistence.Repositories;

public sealed class PostgresTileRepository(IDbContextFactory<CustomDataViewDbContext> contextFactory) : ITileRepository
{
    public void Add(Tile tile)
    {
        using var context = contextFactory.CreateDbContext();
        context.Tiles.Add(new TileRow
        {
            Id = tile.Id,
            Name = tile.Definition.Name,
            Display = tile.Definition.Display,
            ItemsSourceKey = tile.Definition.Items?.Source,
            ItemsPath = tile.Definition.Items?.Path,
            ItemsKeyLabel = tile.Definition.Items?.Key,
            PaginationPageNumberParameter = tile.Definition.Pagination?.PageNumberParameter,
            PaginationPageSizeParameter = tile.Definition.Pagination?.PageSizeParameter,
            PaginationPageSize = tile.Definition.Pagination?.PageSize,
            PaginationTotalPath = tile.Definition.Pagination?.TotalPath,
            Sources = ToSourceRows(tile),
            Fields = ToFieldRows(tile),
            Bindings = ToBindingRows(tile),
            FormFields = ToFormFieldRows(tile),
            Children = ToChildRows(tile),
            Actions = ToActionRows(tile)
        });
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
                .SetProperty(stored => stored.Display, tile.Definition.Display)
                .SetProperty(stored => stored.ItemsSourceKey, tile.Definition.Items == null ? null : tile.Definition.Items.Source)
                .SetProperty(stored => stored.ItemsPath, tile.Definition.Items == null ? null : tile.Definition.Items.Path)
                .SetProperty(stored => stored.ItemsKeyLabel, tile.Definition.Items == null ? null : tile.Definition.Items.Key)
                .SetProperty(
                    stored => stored.PaginationPageNumberParameter,
                    tile.Definition.Pagination == null ? null : tile.Definition.Pagination.PageNumberParameter)
                .SetProperty(
                    stored => stored.PaginationPageSizeParameter,
                    tile.Definition.Pagination == null ? null : tile.Definition.Pagination.PageSizeParameter)
                .SetProperty(stored => stored.PaginationPageSize, tile.Definition.Pagination == null ? null : tile.Definition.Pagination.PageSize)
                .SetProperty(stored => stored.PaginationTotalPath, tile.Definition.Pagination == null ? null : tile.Definition.Pagination.TotalPath)
                .SetProperty(stored => stored.UpdatedAt, stored => DateTime.UtcNow));
        context.TileBindings.Where(binding => binding.TileId == tile.Id).ExecuteDelete();
        context.TileFormFields.Where(field => field.TileId == tile.Id).ExecuteDelete();
        context.TileChildren.Where(child => child.ParentTileId == tile.Id).ExecuteDelete();
        context.TileActions.Where(action => action.TileId == tile.Id).ExecuteDelete();
        context.TileSources.Where(source => source.TileId == tile.Id).ExecuteDelete();
        context.TileSources.AddRange(ToSourceRows(tile));
        context.TileFields.AddRange(ToFieldRows(tile));
        context.TileBindings.AddRange(ToBindingRows(tile));
        context.TileFormFields.AddRange(ToFormFieldRows(tile));
        context.TileChildren.AddRange(ToChildRows(tile));
        context.TileActions.AddRange(ToActionRows(tile));
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
            .Where(tile => tile.Sources.Any(source => source.ConnectionId == connectionId))
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

    public IReadOnlyList<TileReference> GetParents(Guid tileId)
    {
        using var context = contextFactory.CreateDbContext();
        return context.Tiles
            .AsNoTracking()
            .Where(tile => tile.Children.Any(child => child.ChildTileId == tileId))
            .OrderBy(tile => tile.CreatedAt)
            .ThenBy(tile => tile.Id)
            .Select(tile => new TileReference(tile.Id, tile.Name))
            .ToList();
    }

    public IReadOnlyList<TileReference> GetActionReferences(Guid tileId)
    {
        using var context = contextFactory.CreateDbContext();
        return context.Tiles
            .AsNoTracking()
            .Where(tile => tile.Id != tileId && tile.Actions.Any(action => action.TargetTileId == tileId))
            .OrderBy(tile => tile.CreatedAt)
            .ThenBy(tile => tile.Id)
            .Select(tile => new TileReference(tile.Id, tile.Name))
            .ToList();
    }

    public IReadOnlyList<TileFieldUse> GetFieldUses(Guid tileId)
    {
        using var context = contextFactory.CreateDbContext();
        var uses = context.TileBindings
            .AsNoTracking()
            .Join(
                context.TileChildren.Where(child => child.ChildTileId == tileId),
                binding => new { TileId = binding.TileId, Key = binding.SourceNode },
                child => new { TileId = child.ParentTileId, Key = child.Key },
                (binding, child) => binding)
            .Join(context.Tiles, binding => binding.TileId, tile => tile.Id, (binding, tile) => new { binding.SourceField, tile.Id, tile.Name })
            .Distinct()
            .AsEnumerable()
            .Select(use => new TileFieldUse(new TileReference(use.Id, use.Name), use.SourceField))
            .ToList();
        var pathBindings = context.TileBindings
            .AsNoTracking()
            .Where(binding => binding.SourceNode.Contains("/"))
            .Join(context.Tiles, binding => binding.TileId, tile => tile.Id, (binding, tile) => new { binding.SourceNode, binding.SourceField, tile.Id, tile.Name })
            .ToList();
        if (pathBindings.Count == 0)
        {
            return uses;
        }

        var children = context.TileChildren
            .AsNoTracking()
            .Select(child => new { child.ParentTileId, child.Key, child.ChildTileId })
            .AsEnumerable()
            .ToDictionary(child => (child.ParentTileId, child.Key), child => child.ChildTileId);
        uses.AddRange(pathBindings
            .Where(binding => Resolve(binding.Id, binding.SourceNode) == tileId)
            .Select(binding => new TileFieldUse(new TileReference(binding.Id, binding.Name), binding.SourceField)));
        return uses.Distinct().ToList();

        Guid? Resolve(Guid parent, string path)
        {
            Guid? resolved = parent;
            foreach (var key in path.Split('/'))
            {
                resolved = children.TryGetValue((resolved.Value, key), out var child) ? child : null;
                if (resolved is null)
                {
                    return null;
                }
            }

            return resolved;
        }
    }

    private static IQueryable<TileRow> WithFields(CustomDataViewDbContext context)
        => context.Tiles
            .AsNoTracking()
            .Include(tile => tile.Sources.OrderBy(source => source.Position))
            .Include(tile => tile.Bindings.OrderBy(binding => binding.Position))
            .Include(tile => tile.FormFields.OrderBy(field => field.Position))
            .Include(tile => tile.Children.OrderBy(child => child.Position))
            .Include(tile => tile.Actions.OrderBy(action => action.Position))
            .ThenInclude(action => action.Mappings.OrderBy(mapping => mapping.Position))
            .Include(tile => tile.Fields.OrderBy(field => field.Position))
            .ThenInclude(field => field.Transformations.OrderBy(transformation => transformation.Position))
            .AsSplitQuery();

    private static List<TileSourceRow> ToSourceRows(Tile tile)
        => tile.Definition.Sources
            .Select((source, position) => new TileSourceRow
            {
                TileId = tile.Id,
                Key = source.Key,
                Position = position,
                Kind = source.Kind,
                ConnectionId = source.ConnectionId
            })
            .ToList();

    private static List<TileChildRow> ToChildRows(Tile tile)
        => tile.Definition.Children
            .Select(child => new TileChildRow
            {
                ParentTileId = tile.Id,
                Key = child.Key,
                ChildTileId = child.TileId,
                Position = child.Position,
                CssClass = child.CssClass,
                Scope = child.Scope,
                Load = child.Load,
                Label = child.Label
            })
            .ToList();

    private static List<TileActionRow> ToActionRows(Tile tile)
        => tile.Definition.Actions
            .Select((action, position) => new TileActionRow
            {
                TileId = tile.Id,
                Key = action.Key,
                Position = position,
                Label = action.Label,
                Type = action.Type,
                TargetTileId = action.TileId,
                Scope = action.Scope,
                Mappings = action.Map
                    .Select((mapping, mappingPosition) => new TileActionMappingRow
                    {
                        TileId = tile.Id,
                        ActionKey = action.Key,
                        TargetKind = mapping.Parameter is null ? BindingTargetKinds.Input : BindingTargetKinds.Parameter,
                        TargetName = mapping.Parameter ?? mapping.Input,
                        SourceField = mapping.Field,
                        Position = mappingPosition
                    })
                    .ToList()
            })
            .ToList();

    private static List<TileFormFieldRow> ToFormFieldRows(Tile tile)
        => tile.Definition.FormFields
            .Select((field, position) => new TileFormFieldRow
            {
                TileId = tile.Id,
                Position = position,
                Label = field.Label,
                Type = field.Type,
                Required = field.Required,
                DefaultValue = field.Default?.ToJsonString()
            })
            .ToList();

    private static List<TileBindingRow> ToBindingRows(Tile tile)
        => tile.Definition.Bindings
            .SelectMany(binding => binding.Map.Select(mapping => (binding, mapping)))
            .Select((pair, position) => new TileBindingRow
            {
                TileId = tile.Id,
                TargetNode = pair.binding.To,
                TargetKind = pair.mapping.Parameter is null ? BindingTargetKinds.Input : BindingTargetKinds.Parameter,
                TargetName = pair.mapping.Parameter ?? pair.mapping.Input,
                SourceNode = pair.binding.From,
                SourceField = pair.mapping.Field,
                SourceScope = pair.mapping.Scope,
                Position = position
            })
            .ToList();

    private static List<TileBinding> ToBindings(IEnumerable<TileBindingRow> rows)
        => rows
            .GroupBy(row => (row.SourceNode, row.TargetNode))
            .Select(group => new TileBinding(
                group.Key.SourceNode,
                group.Key.TargetNode,
                group
                    .Select(row => row.TargetKind == BindingTargetKinds.Parameter
                        ? new TileBindingMapping(row.SourceField, row.TargetName, null, row.SourceScope)
                        : new TileBindingMapping(row.SourceField, null, row.TargetName, row.SourceScope))
                    .ToList()))
            .ToList();

    private static List<TileFieldRow> ToFieldRows(Tile tile)
        => tile.Definition.Fields
            .Select((field, fieldPosition) => new TileFieldRow
            {
                TileId = tile.Id,
                Position = fieldPosition,
                SourceKey = field.Source,
                Path = field.Path,
                Label = field.Label,
                Hidden = field.Hidden,
                Role = field.Role,
                Transformations = field.Transformations
                    .Select((step, position) => new TileFieldTransformationRow
                    {
                        TileId = tile.Id,
                        FieldLabel = field.Label,
                        Position = position,
                        Key = step.Key,
                        Parameter = step.Parameter
                    })
                    .ToList()
            })
            .ToList();

    private static Tile ToTile(TileRow tile)
        => new Tile(tile.Id, new TileDefinition(
            tile.Name,
            tile.Display,
            tile.Sources.Select(source => new TileSource(source.Key, source.Kind, source.ConnectionId)).ToList(),
            tile.ItemsSourceKey is null ? null : new TileItems(tile.ItemsSourceKey, tile.ItemsPath, tile.ItemsKeyLabel),
            tile.Fields
                .Select(field => new TileField(
                    field.SourceKey,
                    field.Path,
                    field.Label,
                    field.Transformations.Select(step => new TransformationStep(step.Key, step.Parameter)).ToList(),
                    field.Hidden,
                    field.Role))
                .ToList())
        {
            Pagination = tile.PaginationPageSize is null
                ? null
                : new TilePagination(
                    tile.PaginationPageNumberParameter,
                    tile.PaginationPageSizeParameter,
                    tile.PaginationPageSize.Value,
                    tile.PaginationTotalPath),
            Bindings = ToBindings(tile.Bindings),
            Actions = tile.Actions
                .Select(action => new TileAction(
                    action.Key,
                    action.Label,
                    action.Type,
                    action.TargetTileId,
                    action.Scope,
                    action.Mappings
                        .Select(mapping => mapping.TargetKind == BindingTargetKinds.Parameter
                            ? new TileActionMapping(mapping.SourceField, mapping.TargetName, null)
                            : new TileActionMapping(mapping.SourceField, null, mapping.TargetName))
                        .ToList()))
                .ToList(),
            Children = tile.Children
                .Select(child => new TileChild(child.Key, child.ChildTileId, child.Position, child.CssClass, child.Scope, child.Load, child.Label))
                .ToList(),
            FormFields = tile.FormFields
                .Select(field => new TileFormField(
                    field.Label,
                    field.Type,
                    field.Required,
                    field.DefaultValue is null ? null : JsonNode.Parse(field.DefaultValue)))
                .ToList()
        });
}
