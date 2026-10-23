using System.Globalization;
using CustomDataView.Domain.Dictionaries;
using CustomDataView.Domain.Exceptions;
using CustomDataView.Domain.ValueObjects;

namespace CustomDataView.Domain.Services.Transformations;

public abstract class Transformation : ITransformation
{
    public abstract string Key { get; }

    public abstract string Name { get; }

    public abstract string Description { get; }

    public virtual TransformationParameterDescriptor Parameter => null;

    public object Apply(object value, string parameter)
        => value is null ? null : Transform(value, parameter);

    protected abstract object Transform(object value, string parameter);

    protected string RequireText(string parameter)
        => parameter ?? throw InvalidParameter();

    protected int RequireInteger(string parameter)
        => int.TryParse(parameter, NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out var number)
            ? number
            : throw InvalidParameter();

    protected TransformationFailedException InvalidParameter()
        => new(Key, TransformationFailureReasons.InvalidParameter, Parameter?.Name);
}
