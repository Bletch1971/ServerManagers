﻿//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool.
//     Runtime Version:4.0.30319.42000
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

namespace ServerManagerTool.Common {
    
    
    [global::System.Runtime.CompilerServices.CompilerGeneratedAttribute()]
    [global::System.CodeDom.Compiler.GeneratedCodeAttribute("Microsoft.VisualStudio.Editors.SettingsDesigner.SettingsSingleFileGenerator", "17.3.0.0")]
    public sealed partial class CommonConfig : global::System.Configuration.ApplicationSettingsBase {
        
        private static CommonConfig defaultInstance = ((CommonConfig)(global::System.Configuration.ApplicationSettingsBase.Synchronized(new CommonConfig())));
        
        public static CommonConfig Default {
            get {
                return defaultInstance;
            }
        }
        
        [global::System.Configuration.ApplicationScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("SteamCMD")]
        public string SteamCmdRelativePath {
            get {
                return ((string)(this["SteamCmdRelativePath"]));
            }
        }
        
        [global::System.Configuration.ApplicationScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("SteamCMD.exe")]
        public string SteamCmdExeFile {
            get {
                return ((string)(this["SteamCmdExeFile"]));
            }
        }
        
        [global::System.Configuration.ApplicationScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("SteamCMD.zip")]
        public string SteamCmdZipFile {
            get {
                return ((string)(this["SteamCmdZipFile"]));
            }
        }
        
        [global::System.Configuration.ApplicationScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("https://steamcdn-a.akamaihd.net/client/installer/steamcmd.zip")]
        public string SteamCmdUrl {
            get {
                return ((string)(this["SteamCmdUrl"]));
            }
        }
        
        [global::System.Configuration.ApplicationScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("+login anonymous +quit")]
        public string SteamCmdInstallArgs {
            get {
                return ((string)(this["SteamCmdInstallArgs"]));
            }
        }
        
        [global::System.Configuration.ApplicationScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("")]
        public string DefaultSteamAPIKey {
            get {
                return ((string)(this["DefaultSteamAPIKey"]));
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("")]
        public string SteamAPIKey {
            get {
                return ((string)(this["SteamAPIKey"]));
            }
            set {
                this["SteamAPIKey"] = value;
            }
        }
        
        [global::System.Configuration.ApplicationScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("https://steamcommunity.com/dev/apikey")]
        public string SteamAPIKeyUrl {
            get {
                return ((string)(this["SteamAPIKeyUrl"]));
            }
        }
        
        [global::System.Configuration.ApplicationScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("+login {0} {1} +quit")]
        public string SteamCmdAuthenticateArgs {
            get {
                return ((string)(this["SteamCmdAuthenticateArgs"]));
            }
        }
        
        [global::System.Configuration.ApplicationScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("Steam")]
        public string SteamProcessName {
            get {
                return ((string)(this["SteamProcessName"]));
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("")]
        public string SteamClientFile {
            get {
                return ((string)(this["SteamClientFile"]));
            }
            set {
                this["SteamClientFile"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("True")]
        public bool UpgradeConfig {
            get {
                return ((bool)(this["UpgradeConfig"]));
            }
            set {
                this["UpgradeConfig"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("False")]
        public bool SteamCmdRemoveQuit {
            get {
                return ((bool)(this["SteamCmdRemoveQuit"]));
            }
            set {
                this["SteamCmdRemoveQuit"] = value;
            }
        }
        
        [global::System.Configuration.ApplicationScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("https://api.ipify.org")]
        public string PublicIPCheckUrl1 {
            get {
                return ((string)(this["PublicIPCheckUrl1"]));
            }
        }
        
        [global::System.Configuration.ApplicationScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("http://whatismyip.akamai.com/")]
        public string PublicIPCheckUrl2 {
            get {
                return ((string)(this["PublicIPCheckUrl2"]));
            }
        }
    }
}
