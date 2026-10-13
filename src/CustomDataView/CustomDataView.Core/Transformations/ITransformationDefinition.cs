namespace CustomDataView.Core.Transformations;

public interface ITransformationDefinition
{
    string Key { get; }

    string Name { get; }

    string Description { get; }

    IReadOnlyList<TransformationParameterDescriptor> Parameters { get; }

    ITransformation Create(ITransformation inner, TransformationParameters parameters);
}
