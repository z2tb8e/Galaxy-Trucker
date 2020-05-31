using System;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Windows.Data;

namespace GalaxyTrucker.Views.Utils

{
    [ValueConversion(typeof(Image), typeof(byte[]))]
    public class ImageToByteArrayConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if(value is null || !(value is Image myImage))
            {
                return null;
            }
            using var ms = new MemoryStream();
            myImage.Save(ms, myImage.RawFormat);
            return ms.ToArray();
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
