using System.Globalization;
using System.IO;
using System.Windows.Controls;

namespace ServerManagerTool.Common.ValidationRules
{
    public class ProfileNameValidationRule : ValidationRule
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

            var invalidFolderChars = Path.GetInvalidPathChars();
            var invalidFileChars = Path.GetInvalidFileNameChars();

            if (!string.IsNullOrWhiteSpace(strValue))
            {
                // check for invalid folder characters
                if (strValue.IndexOfAny(invalidFolderChars) >= 0)
                    return new ValidationResult(false, "Contains an invalid folder character");

                // check for invalid file characters
                if (strValue.IndexOfAny(invalidFileChars) >= 0)
                    return new ValidationResult(false, "Contains an invalid file character");
            }

            return new ValidationResult(true, null);
        }
    }
}
