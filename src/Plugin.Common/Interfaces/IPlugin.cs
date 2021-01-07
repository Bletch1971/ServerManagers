using System;
using System.Windows;

namespace ServerManagerTool.Plugin.Common
{
    public interface IPlugin
    {
        /// <summary>
        ///   Gets a values indicating if the plugin can be used
        /// </summary>
        bool Enabled
        {
            get;
        }
        /// <summary>
        ///   Gets a value indicating the code of the plugin
        /// </summary>
        string PluginCode
        {
            get;
        }
        /// <summary>
        ///   Gets a value indicating the name of the plugin
        /// </summary>
        string PluginName
        {
            get;
        }
        /// <summary>
        ///   Gets a value indicating the version of the plugin
        /// </summary>
        Version PluginVersion
        {
            get;
        }

        /// <summary>
        ///   Gets a value that indicates if the plugin has a configuration form.
        /// </summary>
        bool HasConfigForm
        {
            get;
        }

        /// <summary>
        /// Performs any initialization for the plugin.
        /// </summary>
        void Initialize();

        /// <summary>
        /// Opens the configuration form.
        /// </summary>
        /// <param name="owner">The owner window.</param>
        void OpenConfigForm(Window owner);
    }
}
