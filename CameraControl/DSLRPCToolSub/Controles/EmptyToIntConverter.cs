using System;
using System.Globalization;
using System.Windows.Data;

namespace DSLR_Tool_PC.Controles
{
    public class EmptyToIntConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null || (int)value == default(int))
                return "";

            return value.ToString();
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (String.IsNullOrEmpty(value as string))
                return default(int);

            return int.Parse(value.ToString());
        }
    }
}
