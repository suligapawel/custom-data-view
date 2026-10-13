namespace CustomDataView.Core.Transformations;

public interface ITransformationDefinition
{
    string Key { get; }

    ITransformation Create(ITransformation inner, TransformationParameters parameters);
}
