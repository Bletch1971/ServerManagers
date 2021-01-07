namespace NeXt.Vdf
{
    /// <summary>
    /// A VdfValue that represents an integer
    /// </summary>
    public sealed class VdfLong : VdfValue
    {
        public VdfLong(string name) : base(name)
        {
            Type = VdfValueType.Long;
        }

        public VdfLong(string name, long value) : this(name)
        {
            Content = value;
        }

        public long Content { get; set; }
    }
}
