using CustomDataView.Application.Abstractions;
using CustomDataView.Application.Transformations.Dtos;
using CustomDataView.Domain.Services.Transformations;

namespace CustomDataView.Application.Transformations.Queries;

public sealed record GetTransformations : IQuery<IReadOnlyList<TransformationDto>>;

public sealed class GetTransformationsHandler(IEnumerable<ITransformation> transformations)
{
    public IReadOnlyList<TransformationDto> Handle(GetTransformations query)
        => transformations
            .OrderBy(transformation => transformation.Key, StringComparer.Ordinal)
            .Select(transformation => new TransformationDto(
                transformation.Key,
                transformation.Name,
                transformation.Description,
                transformation.Parameter is { } parameter ? new TransformationParameterDto(parameter.Name, parameter.Type) : null))
            .ToList()
            .AsReadOnly();
}
