namespace CustomDataView.Application.Tiles.Contracts;

public sealed record TileContract(
    IReadOnlyList<TileContractParameter> Parameters,
    IReadOnlyList<string> Inputs,
    TileContractOutputs Outputs);

public sealed record TileContractParameter(string Name, bool Required);

public sealed record TileContractOutputs(IReadOnlyList<string> Fields, bool Items);
