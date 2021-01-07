// See license at bottom of file
using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Threading;
using System.Windows;

namespace WPFSharp.Globalizer
{
    public abstract class GlobalizedApplication : Application
    {
        public static GlobalizedApplication Instance;

        public GlobalizationManager GlobalizationManager;

        public StyleManager StyleManager;

        public GlobalizedApplication()
        {
            // Make App a singleton
            Instance = this;
        }

        protected override void OnStartup(StartupEventArgs e)
        {
            Init();
        }

        #region Properties

        public virtual String Directory { get; } = Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);

        public virtual EnhancedResourceDictionary FallbackResourceDictionary { get; private set; }

        #endregion

        #region Methods
        public virtual void Init()
        {
            GlobalizationManager = new GlobalizationManager(Resources.MergedDictionaries);
            StyleManager = new StyleManager(Resources.MergedDictionaries);

            // Get current 5 character language and load the appropriate Globalization file
            CreateAvailableLanguages();
            try
            {
                GlobalizationManager.SwitchLanguage(Thread.CurrentThread.CurrentCulture.Name, true);
            }
            catch (Exception ex)
            {
                // Try the fallback
                GlobalizationManager.SwitchLanguage(GlobalizationManager.FallBackLanguage, true);
                Debug.WriteLine($"{nameof(GlobalizationManager.SwitchLanguage)} error.\r\n{ex.Message}");
            }

            // Load the default style
            CreateAvailableStyles();
            try
            {
                StyleManager.SwitchStyle(StyleManager.FallBackStyle, true);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"{nameof(StyleManager.SwitchStyle)} error.\r\n{ex.Message}");
            }

            // Create the FallbackResourceDictionary
            FallbackResourceDictionary = new FallbackResourceDictionary() { Name = "Fallback" };
            Resources.MergedDictionaries.Add(FallbackResourceDictionary);
        }

        public virtual object GetResource(string inKey)
        {
            if (string.IsNullOrWhiteSpace(inKey))
                throw new ArgumentNullException(inKey, "parameter cannot be null.");

            return (Instance.Resources.Contains(inKey)) ? Instance.Resources[inKey] : null;
        }

        public virtual string GetResourceString(string inKey)
        {
            if (string.IsNullOrWhiteSpace(inKey))
                throw new ArgumentNullException(inKey, "parameter cannot be null.");

            if ((Instance.Resources.Contains(inKey) && Instance.Resources[inKey] is string))
            {
                var resourceString = Instance.Resources[inKey].ToString();
                resourceString = resourceString.Replace("\\r", "\r");
                resourceString = resourceString.Replace("\\n", "\n");
                return resourceString;
            }
            return null;
        }

        /// <summary>
        /// // Create and populate the SupportedLanguages singleton
        /// </summary>
        protected virtual void CreateAvailableLanguages()
        {
            AvailableLanguages.CreateInstance();
            AvailableLanguages.Instance.AddListFromSubDirectories(GlobalizationManager.DefaultPath);
        }


        /// <summary>
        /// // Create and populate the SupportedLanguages singleton
        /// </summary>
        protected virtual void CreateAvailableStyles()
        {
            AvailableStyles.CreateInstance();
            AvailableStyles.Instance.AddListFromFiles(StyleManager.DefaultPath);
        }

        #endregion
    }
}

#region License
/*
WPFSharp.Globalizer - A project deisgned to make localization and styling
                      easier by decoupling both process from the build.

Copyright (c) 2015, Jared Barneck (Rhyous)
All rights reserved.
 
Redistribution and use in source and binary forms, with or without
modification, are permitted provided that the following conditions are met:
 
1. Redistributions of source code must retain the above copyright notice, this
   list of conditions and the following disclaimer.
2. Redistributions in binary form must reproduce the above copyright notice,
   this list of conditions and the following disclaimer in the documentation
   and/or other materials provided with the distribution.
3. Use of the source code or binaries that in any way competes with WPFSharp.Globalizer
   or competes with distribution, whether open source or commercial, is 
   prohibited unless permission is specifically granted under a separate
   license by Jared Barneck (Rhyous).
4. Forking for personal or internal, or non-competing commercial use is allowed.
   Distributing compiled releases as part of your non-competing project is 
   allowed.
5. Public copies, or forks, of source is allowed, but from such, public
   distribution of compiled releases is forbidden.
6. Source code enhancements or additions are the property of the author until
   the source code is contributed to this project. By contributing the source
   code to this project, the author immediately grants all rights to the
   contributed source code to Jared Barneck (Rhyous).
 
THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS "AS IS" AND
ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED
WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE
DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT OWNER OR CONTRIBUTORS BE LIABLE FOR
ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES
(INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES;
LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND
ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT
(INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF THIS
SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
 */
#endregion
