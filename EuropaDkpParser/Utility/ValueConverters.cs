// -----------------------------------------------------------------------
// ValueConverters.cs Copyright 2025 Craig Gjeltema
// -----------------------------------------------------------------------

namespace EuropaDkpParser.Utility;

using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Input;

[ValueConversion(typeof(bool), typeof(Cursor))]
public class BoolToCursorConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is not bool)
            return Cursors.Arrow;

        Cursor cur = parameter as Cursor ?? Cursors.Wait;

        return (bool)value ? cur : Cursors.Arrow;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotImplementedException();
}

[ValueConversion(typeof(bool), typeof(Visibility))]
public class BoolToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is not bool)
            return Visibility.Visible;

        Visibility visibility = parameter as Visibility? ?? Visibility.Collapsed;

        return (bool)value ? Visibility.Visible : visibility;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotImplementedException();
}

[ValueConversion(typeof(bool), typeof(bool))]
public class InverseBoolConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (targetType != typeof(bool))
            return true;

        return !(bool)value;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (targetType != typeof(bool))
            return true;

        return !(bool)value;
    }
}

[ValueConversion(typeof(bool), typeof(Visibility))]
public class InverseBoolToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is not bool)
            return Visibility.Collapsed;

        Visibility visibility = parameter as Visibility? ?? Visibility.Visible;

        return (bool)value ? Visibility.Collapsed : visibility;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotImplementedException();
}

[ValueConversion(typeof(string), typeof(System.Windows.Media.Brush))]
public class StringToBrushConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is not string)
            return new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromArgb(255, 255, 255, 255));

        System.Windows.Media.BrushConverter bc = new();
        return (System.Windows.Media.Brush)bc.ConvertFrom(value);
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotImplementedException();
}
