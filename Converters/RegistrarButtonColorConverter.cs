using System;
using System.Globalization;
using Microsoft.Maui.Controls;

namespace sistemaPlaya.Converters
{
    public class RegistrarButtonColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            bool habilitado = value is bool b && b;
            // Verde normal si habilitado, gris opaco si no
            return habilitado ? Color.FromArgb("#4CAF50") : Color.FromArgb("#B0BEC5");
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
