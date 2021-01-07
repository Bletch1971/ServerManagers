namespace NeXt.Vdf
{
    /// <summary>
    /// A VdfValue that represents a string
    /// </summary>
    public sealed class VdfString : VdfValue
    {
        public VdfString(string name) : base(name)
        {
            Type = VdfValueType.String;
        }

        public VdfString(string name, string value) : this(name)
        {
            Content = value;
        }

        public string Content { get; set; }
    }
}
