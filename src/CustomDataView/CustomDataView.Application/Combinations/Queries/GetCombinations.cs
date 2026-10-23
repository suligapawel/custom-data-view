using CustomDataView.Application.Abstractions;
using CustomDataView.Application.Combinations.Dtos;
using CustomDataView.Domain.Services.Combinations;

namespace CustomDataView.Application.Combinations.Queries;

public sealed record GetCombinations : IQuery<IReadOnlyList<CombinationDto>>;

public sealed class GetCombinationsHandler(IEnumerable<ICombination> combinations)
{
    public IReadOnlyList<CombinationDto> Handle(GetCombinations query)
        => combinations
            .OrderBy(combination => combination.Key, StringComparer.Ordinal)
            .Select(combination => new CombinationDto(
                combination.Key,
                combination.Name,
                combination.Description,
                combination.Parameter is { } parameter ? new CombinationParameterDto(parameter.Name, parameter.Type, parameter.Required) : null,
                combination.MinFields,
                combination.MaxFields))
            .ToList()
            .AsReadOnly();
}
