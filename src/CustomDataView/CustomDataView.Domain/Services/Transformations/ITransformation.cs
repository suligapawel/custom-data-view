using CustomDataView.Domain.ValueObjects;

namespace CustomDataView.Domain.Services.Transformations;

public interface ITransformation
{
    string Key { get; }

    string Name { get; }

    string Description { get; }

    TransformationParameterDescriptor Parameter { get; }

    object Apply(object value, string parameter);
}
