namespace CustomDataView.Core.Views.Preview;

public sealed record ViewPreview(Guid Id, string Name, IReadOnlyList<ViewTilePreview> Tiles);
