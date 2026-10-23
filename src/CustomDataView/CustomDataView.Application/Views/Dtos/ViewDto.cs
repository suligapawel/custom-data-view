namespace CustomDataView.Application.Views.Dtos;

public sealed record ViewDto(Guid Id, string Name, IReadOnlyList<ViewTileDto> Tiles);
