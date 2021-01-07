namespace ServerManagerTool.Updater
{
    public static class ScriptUtils
    {
        public static string AsQuoted(this string parameter)
        {
            var newValue = parameter;
            if (!newValue.StartsWith("\""))
                newValue = "\"" + newValue;
            if (!newValue.EndsWith("\""))
                newValue = newValue + "\"";
            return newValue;
        }
    }
}
