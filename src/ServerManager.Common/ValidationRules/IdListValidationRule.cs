using System;
using System.Linq;
using System.Windows.Controls;

namespace ServerManagerTool.Common.ValidationRules
{
    public class IdListValidationRule : ValidationRule
    {
        public override ValidationResult Validate(object value, System.Globalization.CultureInfo cultureInfo)
        {
            var strValue = (string)value;

            if (!String.IsNullOrWhiteSpace(strValue))
            {
                // check if there are any spaces
                if (strValue.Contains(" "))
                {
                    return new ValidationResult(false, "Spaces are not permitted");
                }

                // check for valid ids
                var entries = strValue.Split(',');

                if (entries.FirstOrDefault(e => !Int64.TryParse(e, out long throwaway)) != null)
                {
                    return new ValidationResult(false, "Must be a comma-separated list of ids");
                }
            }

            return new ValidationResult(true, null);
        }
    }
}
