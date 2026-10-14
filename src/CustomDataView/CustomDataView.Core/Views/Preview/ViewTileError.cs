namespace CustomDataView.Core.Views.Preview;

public sealed record ViewTileError(string Code, IDictionary<string, string[]> Errors);
