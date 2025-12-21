namespace Imapster.Converters;

public abstract class AiCategoryConverter : IValueConverter
{
    public static (Color Background, Color Foreground) GetTagColors(object? tag) => tag?.ToString()?.ToLower() switch
    {
        "persoonlijk" => (Color.FromArgb("#E0F2FE"), Color.FromArgb("#0369A1")), // Light blue bg, dark blue text
        "werk" => (Color.FromArgb("#FEF3C7"), Color.FromArgb("#92400E")), // Light amber bg, dark amber text
        "administratie" => (Color.FromArgb("#DCFCE7"), Color.FromArgb("#166534")), // Light green bg, dark green text
        "reclame" => (Color.FromArgb("#F3E8FF"), Color.FromArgb("#6B21A8")), // Light purple bg, dark purple text
        "nieuws" => (Color.FromArgb("#ECFDF5"), Color.FromArgb("#065F46")), // Mint bg, dark teal text
        "overig" => (Color.FromArgb("#E0E7FF"), Color.FromArgb("#3730A3")), // Light indigo bg, dark indigo text
        //"Frontend" => (Color.FromArgb("#E0F2FE"), Color.FromArgb("#0369A1")), // Same as Development
        //"Backend" => (Color.FromArgb("#F3F4F6"), Color.FromArgb("#374151")), // Light gray bg, dark gray text
        _ => (Color.FromArgb("#E5E7EB"), Color.FromArgb("#111827"))  // Default: neutral gray
    };

    public abstract object Convert(object? value, Type targetType, object? parameter, CultureInfo culture);

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) => throw new NotImplementedException();
}

public class AiForegroundConverter : AiCategoryConverter
{
    public override object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) => GetTagColors(value).Foreground;
}

public class AiBackgroundConverter : AiCategoryConverter
{
    public override object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) => GetTagColors(value).Background;
}
