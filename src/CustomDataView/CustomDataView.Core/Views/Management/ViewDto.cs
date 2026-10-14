namespace CustomDataView.Core.Views.Management;

public sealed record ViewDto(Guid Id, string Name, IReadOnlyList<ViewTileDto> Tiles);
