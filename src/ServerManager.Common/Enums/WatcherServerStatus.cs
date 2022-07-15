namespace ServerManagerTool.Common.Enums
{
    public enum WatcherServerStatus
    {
        /// <summary>
        /// The server binary couldnot be found.
        /// </summary>
        NotInstalled,

        /// <summary>
        /// The server binary was found, but the process was not
        /// </summary>
        Stopped,

        /// <summary>
        /// The server binary was found, the process was found, but no permissions to access the process.
        /// </summary>
        Unknown,

        /// <summary>
        /// The server process was found, but the server is not responding on its port
        /// </summary>
        Initializing,

        /// <summary>
        /// The server is responding locally on its port, a local check was made
        /// </summary>
        LocalSuccess,

        /// <summary>
        /// The server is responding locally on its port, a public check was skipped
        /// </summary>
        ExternalSkipped,

        /// <summary>
        /// The server is responding locally on its port, a public check was made
        /// </summary>
        ExternalSuccess,

        /// <summary>
        /// The server is responding publicly on its port
        /// </summary>
        Published,
    }
}
