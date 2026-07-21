namespace SearchComparisonNet.GUI.Converters;

public class NumStringConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value == null)
        { return null; }

        const double limitAbsLog = 3;
        var isAbsLogSmall = value is double db && Math.Abs(Math.Log10(db)) < limitAbsLog;
        var text = value.ToString();

        if (long.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out var intValue))
        {
            return isAbsLogSmall
                ? intValue.ToString("D3", CultureInfo.InvariantCulture)
                : intValue.ToString("G3", CultureInfo.InvariantCulture);
        }

        if (double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out var dbValue))
        {
            return isAbsLogSmall
                ? dbValue.ToString("G3", CultureInfo.InvariantCulture)
                : dbValue.ToString("0.00E+00", CultureInfo.InvariantCulture);
        }

        // Must be a string
        return text;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        var valueStr = value as string;
        if (int.TryParse(valueStr, NumberStyles.Integer, CultureInfo.InvariantCulture, out var resInt))
            return resInt;

        if (double.TryParse(valueStr, NumberStyles.Float, CultureInfo.InvariantCulture, out var resDb))
            return resDb;

        // Must be a string
        return valueStr;
    }
}
