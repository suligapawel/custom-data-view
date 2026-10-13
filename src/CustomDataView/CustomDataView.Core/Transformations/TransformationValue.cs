using System.Globalization;

namespace CustomDataView.Core.Transformations;

public static class TransformationValue
{
    public static decimal ToNumber(string key, object value)
    {
        if (value is string text)
        {
            if (decimal.TryParse(text.Replace(',', '.'), NumberStyles.Float, CultureInfo.InvariantCulture, out var number))
            {
                return number;
            }

            throw new TransformationFailedException(key, TransformationFailureReasons.InvalidValue);
        }

        return (decimal)value;
    }
}
