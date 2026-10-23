namespace CustomDataView.Application.Combinations.Dtos;

public sealed record CombinationDto(
    string Key,
    string Name,
    string Description,
    CombinationParameterDto Parameter,
    int MinFields,
    int? MaxFields);
