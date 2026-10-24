using CustomDataView.Domain.ValueObjects;

namespace CustomDataView.Application.Tiles.Dtos;

public sealed record TileContractDto(
    IReadOnlyList<TileContractParameterDto> Parameters,
    IReadOnlyList<string> Inputs,
    TileContractOutputsDto Outputs)
{
    public static TileContractDto From(TileContract contract)
        => new(
            contract.Parameters.Select(parameter => new TileContractParameterDto(parameter.Name, parameter.Required)).ToList(),
            contract.Inputs,
            new TileContractOutputsDto(contract.Outputs.Fields, contract.Outputs.Items));
}

public sealed record TileContractParameterDto(string Name, bool Required);

public sealed record TileContractOutputsDto(IReadOnlyList<string> Fields, bool Items);
