using System;
using System.Globalization;
using System.Windows.Controls;

namespace GalaxyTrucker.Views.Utils
{
    public class RemotePortValidationRule : ValidationRule
    {
        public RemotePortValidationRule() { }


        public override ValidationResult Validate(object value, CultureInfo cultureInfo)
        {
            int port;
            try
            {
                port = int.Parse((string)value);
            }
            catch (FormatException)
            {
                return new ValidationResult(false, "A megadott port nem szám!");
            }
            if (port < 1024 || port > 65535)
            {
                return new ValidationResult(false, "A megadott port 1024 és 65535 között kell, hogy legyen!");
            }
            else return ValidationResult.ValidResult;
        }
    }
}
