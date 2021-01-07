namespace NeXt.Vdf
{
    /// <summary>
    /// Flag that determines how to handle a VdfValue
    /// </summary>
    /// <remarks>
    /// All VdfValues can be casted according to their Type:
    /// VdfValueType.Table => VdfTable
    /// VdfValueType.String => VdfString
    /// VdfValueType.Integer => VdfInteger
    /// VdfValueType.Double => VdfDouble
    /// </remarks>
    public enum VdfValueType
    {
        Table,
        String,
        Long,
        Decimal,
    }
}
