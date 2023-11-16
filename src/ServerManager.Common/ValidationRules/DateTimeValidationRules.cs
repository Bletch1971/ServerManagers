using ServerManagerTool.Common.Extensions;
using System;
using System.Windows.Controls;

namespace ServerManagerTool.Common.ValidationRules
{
    public class DateTimeValidationRule : ValidationRule
    {
        private static readonly DateTime MinUnixDate = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);
        private static readonly DateTime MaxUnixDate = new DateTime(2038, 1, 19, 3, 14, 7, 0, DateTimeKind.Utc);

        public override ValidationResult Validate(object value, System.Globalization.CultureInfo cultureInfo)
        {
            string strDateTime = (string)value;

            if (strDateTime.IsEmpty())
            {
                return new ValidationResult(true, null);
            }

            if (!DateTime.TryParse(strDateTime, out DateTime datetime))
            {
                return new ValidationResult(false, "Invalid Date. Date must be formatted as yyyy.mm.dd hh:mm:ss");
            }

            if (datetime.ToUniversalTime() < MinUnixDate)
            {
                return new ValidationResult(false, $"Invalid Date. The Date must be after {MinUnixDate.ToLocalTime().ToString("yyyy.MM.dd HH:mm:ss")}");
            }

            if (datetime.ToUniversalTime() > MaxUnixDate)
            {
                return new ValidationResult(false, $"Invalid Date. The Date must be before {MaxUnixDate.ToLocalTime().ToString("yyyy.MM.dd HH:mm:ss")}");
            }

            return new ValidationResult(true, null);
        }
    }
}
