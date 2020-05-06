using System.Globalization;
using System.Linq;
using System.Windows.Controls;

namespace GalaxyTrucker.Views.Utils
{
    public class PlayerNameValidationRule : ValidationRule
    {
        public PlayerNameValidationRule() { }

        public override ValidationResult Validate(object value, CultureInfo cultureInfo)
        {
            if(((string)value).All(c => char.IsLetter(c)))
            {
                return ValidationResult.ValidResult;
            }
            else
            {
                return new ValidationResult(false, "A játékos neve csak betűket tartalmazhat!");
            }
        }
    }
}
