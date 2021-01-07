using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NeXt.Vdf
{

    /// <summary>
    /// A VdfValue that represents a double
    /// </summary>
    public sealed class VdfDecimal : VdfValue
    {
        public VdfDecimal(string name) : base(name)
        {
            Type = VdfValueType.Decimal;
        }

        public VdfDecimal(string name, decimal value) : this(name)
        {
            Content = value;
        }

        public decimal Content { get; set; }
    }
}
