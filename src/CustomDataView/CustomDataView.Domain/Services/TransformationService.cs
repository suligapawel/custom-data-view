using CustomDataView.Domain.Dictionaries;
using CustomDataView.Domain.Exceptions;
using CustomDataView.Domain.Services.Transformations;
using CustomDataView.Domain.ValueObjects;

namespace CustomDataView.Domain.Services;

public sealed class TransformationService : ITransformationService
{
    public const string TransformationFailedCode = "transformation_failed";

    private readonly Dictionary<string, ITransformation> _transformations;

    public TransformationService(IEnumerable<ITransformation> transformations)
    {
        _transformations = transformations.ToDictionary(transformation => transformation.Key, StringComparer.Ordinal);
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
