namespace CustomDataView.Application.Views.Preview;

public sealed record ViewTileError(string Code, IDictionary<string, string[]> Errors);
