namespace Imapster.Converters;

public class IntToThicknessConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is int indentLevel)
        {
            return new Thickness(indentLevel * 16, 4, 0, 4);
        }
        return new Thickness(4);
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}