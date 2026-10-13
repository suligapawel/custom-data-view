namespace CustomDataView.Core.Transformations.Catalog;

public sealed class TransformationService : ITransformationService
{
    public const string TransformationFailedCode = "transformation_failed";

    private readonly Dictionary<string, ITransformation> _transformations;
    private readonly IReadOnlyList<TransformationDto> _catalog;

    public TransformationService(IEnumerable<ITransformation> transformations)
    {
        _transformations = transformations.ToDictionary(transformation => transformation.Key, StringComparer.Ordinal);
        _catalog = _transformations.Values
            .OrderBy(transformation => transformation.Key, StringComparer.Ordinal)
            .Select(transformation => new TransformationDto(
                transformation.Key,
                transformation.Name,
                transformation.Description,
                transformation.Parameters.Select(parameter => new TransformationParameterDto(parameter.Name, parameter.Type)).ToList()))
            .ToList()
            .AsReadOnly();
    }

    public IReadOnlyList<TransformationDto> GetAll()
    {
        return _catalog;
    }

    public TransformationResult Apply(object value, IReadOnlyList<TransformationStep> steps)
    {
        try
        {
            foreach (var step in steps)
            {
                var transformation = _transformations.GetValueOrDefault(step.Key)
                    ?? throw new TransformationFailedException(step.Key, TransformationFailureReasons.UnknownTransformation);
                value = transformation.Apply(value, new TransformationParameters(step.Parameters));
            }

            return new TransformationResult(value);
        }
        catch (TransformationFailedException exception)
        {
            return new TransformationResult(null, new TransformationError(TransformationFailedCode, exception.Key, exception.Reason));
        }
    }
}
