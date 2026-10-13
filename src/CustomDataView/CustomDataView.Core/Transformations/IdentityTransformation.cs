namespace CustomDataView.Core.Transformations;

public sealed class IdentityTransformation : ITransformation
{
    public string Key => "identity";

    public object Apply(object value)
    {
        return value;
    }
}
