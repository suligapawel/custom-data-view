namespace CustomDataView.Core.Transformations;

public sealed record TransformationDto(
    string Key,
    string Name,
    string Description,
    IReadOnlyList<TransformationParameterDto> Parameters);
