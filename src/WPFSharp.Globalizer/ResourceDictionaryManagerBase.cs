// See license at end of the file
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Windows;

namespace WPFSharp.Globalizer
{
    public abstract class ResourceDictionaryManagerBase : IManageResourceDictionaries
    {
        #region IManageResourceDictionaries Events

        public virtual event ResourceDictionaryChangedEventHandler ResourceDictionaryChangedEvent;

        #endregion

        #region Contructor

        public ResourceDictionaryManagerBase(Collection<ResourceDictionary> inMergedDictionaries)
        {
            MergedDictionaries = inMergedDictionaries;
        }

        #endregion

        #region Properties

        public virtual string SubDirectory { get; set; }

        public virtual string DefaultPath
        {
            get { return _defaultPath ?? (_defaultPath = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), SubDirectory)); }
        }
        private String _defaultPath;

        public Collection<ResourceDictionary> MergedDictionaries { get; set; }

        public virtual List<string> FileNames
        {
            get { return _FileNames ?? (_FileNames = new List<string>()); }
            set { _FileNames = value; }
        } private List<string> _FileNames;

        #endregion

        #region Methods

        public void Remove(string inResourceDictionaryName)
        {
            EnhancedResourceDictionary erdToRemove = null;
            foreach (EnhancedResourceDictionary erd in MergedDictionaries.OfType<EnhancedResourceDictionary>())
            {
                if (erd.Name == inResourceDictionaryName)
                    erdToRemove = erd;
            }
            if (erdToRemove != null)
                MergedDictionaries.Remove(erdToRemove);
        }

        public void RemoveAll()
        {
            MergedDictionaries.Clear();
        }

        public virtual void NotifyResourceDictionaryChanged(ResourceDictionaryChangedEventArgs inEventArgs = null)
        {
            ResourceDictionaryChangedEvent?.Invoke(this, inEventArgs ?? new ResourceDictionaryChangedEventArgs());
        }

        public abstract EnhancedResourceDictionary LoadFromFile(string inFile);

        public virtual void LoadDictionariesFromFiles(List<string> inList)
        {
            foreach (var filePath in inList)
            {
                MergedDictionaries.Add(LoadFromFile(filePath) as EnhancedResourceDictionary);
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

