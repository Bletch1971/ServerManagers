using System;
using System.ComponentModel;
using WPFSharp.Globalizer;

namespace ServerManagerTool.Lib.ViewModel
{
    public class EnumDescriptionTypeConverter : EnumConverter
    {
        public EnumDescriptionTypeConverter(Type type)
            : base(type)
        {
        }

        public override object ConvertTo(ITypeDescriptorContext context, System.Globalization.CultureInfo culture, object value, Type destinationType)
        {
            if (destinationType == typeof(string))
            {
                if (value != null)
                {
                    var strType = value.GetType().Name;
                    var strVal = value.ToString();

                    return GlobalizedApplication.Instance.GetResourceString($"{strType}_{strVal}") ?? strVal;
                }

                return string.Empty;
            }

            return base.ConvertTo(context, culture, value, destinationType);
        }
    }
}
