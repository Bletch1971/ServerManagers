using Microsoft.VisualStudio.TestTools.UnitTesting;
using ServerManagerTool.Plugin.Common;
using ServerManagerTool.Plugin.Common.Lib;
using System.Collections.Generic;

namespace Plugin.Common.UnitTests
{
    [TestClass]
    public class PluginHelperUnitTest
    {
        [TestMethod]
        public void PluginHelper_HandleAlert_When_SingleLineMessage_Then_Valid()
        {
            // Arrange
            PluginHelper.Instance.SetFetchProfileCallback(FetchProfiles);

            // Act
            var profileList = PluginHelper.Instance.FetchProfileList();

            // Assert
            Assert.IsNotNull(profileList);
            Assert.AreEqual(3, profileList.Count);
        }

        public IList<Profile> FetchProfiles()
        {
            return new List<Profile>()
            {
                new Profile() { ProfileName = "Profile 1", InstallationFolder = @"d:\asmdata\servers\server1" },
                new Profile() { ProfileName = "Profile 2", InstallationFolder = @"d:\asmdata\servers\server2" },
                new Profile() { ProfileName = "Profile 3", InstallationFolder = @"d:\asmdata\servers\server3" },
            };
        }

    }
}
