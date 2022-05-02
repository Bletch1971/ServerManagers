using System;
using System.IO;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using ServerManagerTool.Common.Utils;

namespace ServerManager.Common.Tests
{
    [TestClass]
    public class ZipUtilsUnitTest
    {
        [TestMethod]
        public void ZipUtils_()
        {
            // Arrange
            var zipFile = FetchZipFile();
            var destinationPath = Path.Combine(@"D:\_smtest", Path.GetFileNameWithoutExtension(zipFile));
            var destinationPath1 = Path.Combine(destinationPath, "SavedArks");

            // Act
            var count = ZipUtils.ExtractFiles(zipFile, destinationPath1, recurseFolders: false);
            // Assert
            Assert.AreEqual(3, count);

            // Act
            var exists = ZipUtils.DoesFolderExist(zipFile, "SaveGames");
            // Assert
            Assert.IsTrue(exists);

            // Act
            count = ZipUtils.ExtractFiles(zipFile, destinationPath, sourceFolder: "SaveGames", recurseFolders: true);
            // Assert
            Assert.AreEqual(6, count);

            // Act
            exists = ZipUtils.DoesFolderExist(zipFile, "AwesomeTeleporters");
            // Assert
            Assert.IsTrue(exists);

            // Act
            count = ZipUtils.ExtractFiles(zipFile, destinationPath, sourceFolder: "AwesomeTeleporters", recurseFolders: true);
            // Assert
            Assert.AreEqual(1, count);
        }

        private string FetchZipFile()
        {
            return @"D:\_smtest\new_theisland_20220501_221004.zip";
        }
    }
}
