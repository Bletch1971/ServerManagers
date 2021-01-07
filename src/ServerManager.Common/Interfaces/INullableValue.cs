namespace ServerManagerTool.Common.Interfaces
{
    public interface INullableValue
    {
        bool HasValue { get; }

        INullableValue Clone();

        void SetValue(object value);
    }
}
