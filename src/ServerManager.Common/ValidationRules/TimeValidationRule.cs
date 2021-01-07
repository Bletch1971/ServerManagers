using System;
using System.Windows.Controls;

namespace ServerManagerTool.Common.ValidationRules
{
    public class TimeValidationRule : ValidationRule
    {
        public override ValidationResult Validate(object value, System.Globalization.CultureInfo cultureInfo)
        {
            string strTime = (string)value;

            var split = strTime.Split(':');
            if (split.Length != 2)
            {
                return new ValidationResult(false, "Invalid time.  Time must be formatted as hh:mm");
            }

            int hours;
            if(!Int32.TryParse(split[0], out hours))
            {
                return new ValidationResult(false, "Invalid hours.  Time must be formatted as hh:mm");
            }

            if(hours < 0 || hours > 23)
            {
                return new ValidationResult(false, "Hours must be a value from 00 to 23");
            }

            int minutes;
            if(!Int32.TryParse(split[1], out minutes))
            {
                return new ValidationResult(false, "Invalid hours.  Time must be formatted as hh:mm");
            }

            if(minutes < 0 || minutes > 59)
            {
                return new ValidationResult(false, "Miunutes must be a value from 00 to 59");
            }

            return new ValidationResult(true, null);
        }
    }
}
