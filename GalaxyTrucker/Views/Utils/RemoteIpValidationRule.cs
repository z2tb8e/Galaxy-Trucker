using System;
using System.Globalization;
using System.Net;
using System.Windows.Controls;

namespace GalaxyTrucker.Views.Utils
{
    public class RemoteIpValidationRule : ValidationRule
    {
        public RemoteIpValidationRule() { }

        public override ValidationResult Validate(object value, CultureInfo cultureInfo)
        {
            try
            {
                IPAddress.Parse((string)value);
                return ValidationResult.ValidResult;
            }
            catch (FormatException)
            {
                return new ValidationResult(false, "A megadott ip cím nem megfelelő formátumú!");
            }
        }
    }
}
