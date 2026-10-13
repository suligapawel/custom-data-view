using System.Globalization;

namespace CustomDataView.Core.Transformations;

public static class TransformationValue
{
    public static decimal ToNumber(object value)
    {
        if (value is string text)
        {
            return decimal.Parse(text.Replace(',', '.'), NumberStyles.Float, CultureInfo.InvariantCulture);
        }

        return (decimal)value;
    }
}
