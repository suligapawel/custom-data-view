namespace CustomDataView.Domain.ValueObjects;

public sealed record TileContract(
    IReadOnlyList<TileContractParameter> Parameters,
    IReadOnlyList<string> Inputs,
    TileContractOutputs Outputs)
{
    public static TileContract Empty { get; } = new([], [], new TileContractOutputs([], false));

    public bool Declares(string parameter, string input)
        => parameter is not null ? DeclaresParameter(parameter) : DeclaresInput(input);

    public bool DeclaresParameter(string name) => Parameters.Any(parameter => parameter.Name == name);

    public bool DeclaresInput(string name) => Inputs.Contains(name);
}

public sealed record TileContractParameter(string Name, bool Required);

public sealed record TileContractOutputs(IReadOnlyList<string> Fields, bool Items);
