using GalaxyTrucker.ViewModels;
using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace GalaxyTrucker.Views.Utils
{
    public class EnumDescriptionTypeConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null) return DependencyProperty.UnsetValue;

            return EnumHelpers.GetDescription((Enum)value);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value;
        }

        public EnumDescriptionTypeConverter() { }
    }
}
