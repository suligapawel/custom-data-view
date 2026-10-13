namespace CustomDataView.Core.Transformations.Abstractions;

public interface ITransformation
{
    string Key { get; }

    string Name { get; }

    string Description { get; }

    IReadOnlyList<TransformationParameterDescriptor> Parameters { get; }

    object Apply(object value, TransformationParameters parameters);
}
