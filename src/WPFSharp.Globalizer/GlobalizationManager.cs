// See license at bottom of file
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using System.Windows;
using System.Windows.Markup;

namespace WPFSharp.Globalizer
{
    public sealed class GlobalizationManager : ResourceDictionaryManagerBase
    {
        #region Members

        public const string FallBackLanguage = "en-US";

        #endregion

        #region Contructor
        public GlobalizationManager(Collection<ResourceDictionary> inMergedDictionaries)
            : base(inMergedDictionaries)
        {
            SubDirectory = "Globalization";
        }

        #endregion

        #region Functions

        /// <summary>
        /// Dynamically load a Localization ResourceDictionary from a file
        /// </summary>
        public void SwitchLanguage(string inFiveCharLang, bool inForceSwitch = false)
        {
            if (!AvailableLanguages.Instance.Contains(inFiveCharLang))
            {
                inFiveCharLang = FallBackLanguage;
            }

            if (CultureInfo.CurrentCulture.Name.Equals(inFiveCharLang) && !inForceSwitch)
                return;

            // Set the new language
            var ci = new CultureInfo(inFiveCharLang);
            Thread.CurrentThread.CurrentCulture = ci;
            Thread.CurrentThread.CurrentUICulture = ci;

            FileNames = new List<string>();
            string[] xamlFiles;

            // check if the switch to language matches the fallback language
            if (!inFiveCharLang.Equals(FallBackLanguage))
            {
                // switch to language is different, must load the fallback language first
                xamlFiles = Directory.GetFiles(Path.Combine(DefaultPath, FallBackLanguage), $"{FallBackLanguage}.xaml");
                if (xamlFiles.Length > 0)
                    FileNames.AddRange(xamlFiles);
            }

            // load the switch to language
            xamlFiles = Directory.GetFiles(Path.Combine(DefaultPath, inFiveCharLang), $"{inFiveCharLang}.xaml");
            if (xamlFiles.Length > 0)
                FileNames.AddRange(xamlFiles);

            // Remove previous ResourceDictionaries
            RemoveResourceDictionaries();

            // Add new Resource Dictionaries
            LoadDictionariesFromFiles(FileNames);
            var args = new ResourceDictionaryChangedEventArgs { ResourceDictionaryPaths = FileNames, ResourceDictionaryNames = FileNames.Select(f => Path.GetFileNameWithoutExtension(f)).ToList() };
            NotifyResourceDictionaryChanged(args);
        }

        private void RemoveResourceDictionaries()
        {
            var dictionariesToRemove = new List<ResourceDictionary>();
            foreach (ResourceDictionary rd in GlobalizedApplication.Instance.Resources.MergedDictionaries)
            {
                if (rd is GlobalizationResourceDictionary)
                {
                    dictionariesToRemove.Add(rd);
                }
            }

            foreach (EnhancedResourceDictionary erd in dictionariesToRemove)
            {
                GlobalizedApplication.Instance.Resources.MergedDictionaries.Remove(erd);
                // Also remove any associated LinkedStyles
                var globalizationResourceDictionary = erd as GlobalizationResourceDictionary;
                if (globalizationResourceDictionary != null && globalizationResourceDictionary.LinkedStyle != null)
                    Remove((erd as GlobalizationResourceDictionary).LinkedStyle);
            }
        }

        public override EnhancedResourceDictionary LoadFromFile(string inFile)
        {
            return LoadFromFile(inFile, true);
        }

        public EnhancedResourceDictionary LoadFromFile(string inFile, bool inRequireGlobalizationType = true)
        {
            string file = inFile;
            // Determine if the path is absolute or relative
            if (!Path.IsPathRooted(inFile))
            {
                file = Path.Combine(DefaultPath, CultureInfo.CurrentCulture.Name, inFile);
            }

            if (!File.Exists(file))
                return null;

            try
            {
                using (var fs = new FileStream(file, FileMode.Open, FileAccess.Read, FileShare.Read))
                {
                    // Read in an EnhancedResourceDictionary File or preferably an GlobalizationResourceDictionary file
                    var rd = XamlReader.Load(fs) as EnhancedResourceDictionary;
                    if (rd == null)
                        return null;

                    if (inRequireGlobalizationType)
                    {
                        if (rd is GlobalizationResourceDictionary)
                            return rd;

                        return null;
                    }

                    return rd;
                }
            }
            catch
            {
                return null;
            }
        }

        public override void LoadDictionariesFromFiles(List<string> inList)
        {
            foreach (var filePath in inList)
            {
                // Only Globalization resource dictionaries should be added
                // Ignore other types
                var rd = LoadFromFile(filePath) as GlobalizationResourceDictionary;
                if (rd == null)
                    continue;

                MergedDictionaries.Add(rd);

                if (rd.LinkedStyle == null)
                    continue;

                var styleFile = rd.LinkedStyle + ".xaml";
                if (rd.Source != null)
                {
                    var path = Path.Combine(Path.GetDirectoryName(rd.Source), styleFile);
                    // Todo: Check for file and if not there, look in the Styles dir

                    var lrd = LoadFromFile(path, false);
                    if (lrd != null)
                    {
                        MergedDictionaries.Add(lrd);
                    }
                    return;
                }

                var srd = LoadFromFile(styleFile, false);
                if (srd != null)
                {
                    MergedDictionaries.Add(srd);
                }
            }
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

