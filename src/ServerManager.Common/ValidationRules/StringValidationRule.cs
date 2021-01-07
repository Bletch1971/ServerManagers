using System.Globalization;
using System.Windows.Controls;

namespace ServerManagerTool.Common.ValidationRules
{
    public class StringNoSpacesValidationRule : ValidationRule
    {
        /// <summary>
        /// When overridden in a derived class, performs validation checks on a value.
        /// </summary>
        /// <returns>
        /// A <see cref="T:System.Windows.Controls.ValidationResult"/> object.
        /// </returns>
        /// <param name="value">The value from the binding target to check.</param><param name="cultureInfo">The culture to use in this rule.</param>
        public override ValidationResult Validate(object value, CultureInfo cultureInfo)
        {
            var strValue = (string)value;

            if (!string.IsNullOrWhiteSpace(strValue))
            {
                // check if there are any spaces
                if (strValue.Contains(" "))
                    return new ValidationResult(false, "Spaces are not permitted");
            }

            return new ValidationResult(true, null);
        }
    }
}
