namespace CustomDataView.Application.Transformations.Dtos;

public sealed record TransformationDto(
    string Key,
    string Name,
    string Description,
    IReadOnlyList<TransformationParameterDto> Parameters);
