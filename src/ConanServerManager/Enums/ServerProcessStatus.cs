namespace ServerManagerTool.Enums
{
    public enum ServerProcessStatus
    {
        /// <summary>
        /// The server binary could not be found
        /// </summary>
        NotInstalled,

        /// <summary>
        /// The server binary was found, but the process was not.
        /// </summary>
        Stopped,

        /// <summary>
        /// The server binary was found, the process was found, but no permissions to access the process.
        /// </summary>
        Unknown,

        /// <summary>
        /// The server process was found
        /// </summary>
        Running,
    }
}
