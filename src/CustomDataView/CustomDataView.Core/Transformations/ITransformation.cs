namespace CustomDataView.Core.Transformations;

public interface ITransformation
{
    string Key { get; }

    object Apply(object value);
}
