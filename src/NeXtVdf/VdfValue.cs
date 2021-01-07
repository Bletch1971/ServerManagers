using System.Collections.Generic;

namespace NeXt.Vdf
{
    /// <summary>
    /// Abstract VdfValue
    /// </summary>
    public abstract class VdfValue
    {
        public VdfValue(string name)
        {
            Name = name;
        }
        
        /// <summary>
        /// This values name
        /// </summary>
        public string Name { get; private set; }

        private List<string> comments = new List<string>();

        /// <summary>
        /// This values type, determines how it can be casted
        /// </summary>
        public VdfValueType Type { get; protected set; }

        /// <summary>
        /// Comments that where in front of this VdfValue
        /// </summary>
        public ICollection<string> Comments { get { return comments; } }

        /// <summary>
        /// This values Parent, null for root 
        /// </summary>
        public VdfValue Parent { get; internal set; }
    }
}
