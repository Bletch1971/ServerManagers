namespace ServerManagerTool.Plugin.Common
{
    public interface IAlertPlugin : IPlugin
    {
        /// <summary>
        /// Handles the alert message passed for the profile.
        /// </summary>
        /// <param name="alertType">The type of alert message.</param>
        /// <param name="profileName">The name of the profile the alert message is associated with.</param>
        /// <param name="alertMessage">The message of the alert.</param>
        void HandleAlert(AlertType alertType, string profileName, string alertMessage);
    }
}
