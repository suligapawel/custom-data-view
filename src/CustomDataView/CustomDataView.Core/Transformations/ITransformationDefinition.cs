namespace CustomDataView.Core.Transformations;

public interface ITransformationDefinition
{
    string Key { get; }

    string Name { get; }

    ITransformation Create(ITransformation inner, TransformationParameters parameters);
}
