using System;
using System.Globalization;
using System.Windows.Data;

namespace DSLR_Tool_PC.Controles
{
    public class IntToString : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            //convert the int to a string:
            return value.ToString();
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            //convert the string back to an int here
            return int.Parse(value.ToString());
        }
    }
}
