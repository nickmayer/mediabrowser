using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;
using System.IO;
using MediaBrowser.Library.Factories;
using MediaBrowser.Library.Filesystem;
using MediaBrowser.Library;

namespace TestMediaBrowser {
    [TestFixture]
    public class TestMediaLocation {
        string testFolder =  Path.Combine(Path.GetTempPath(), "MediaBrowserTests");
 
        [TearDown]
        public void Teardown() {
            if (Directory.Exists(testFolder)) {
                Directory.Delete(testFolder, true);
            }
        }

        private void CreateDirectories(params string[] dirs){
            foreach (var dir in dirs) {
                Directory.CreateDirectory(dir);
            }
        }

        [Test]
        public void HiddenFilesAndFoldersShouldBeDetectedProperly() {
            
            var hiddenFolder = Path.Combine(testFolder, "hiddenFolder");
            var folder = Path.Combine(testFolder, "folder");

            CreateDirectories(testFolder, hiddenFolder, folder);
            new DirectoryInfo(hiddenFolder).Attributes = FileAttributes.Hidden;

            var hiddenFile = Path.Combine(testFolder, "hiddenFile");
            var file = Path.Combine(testFolder, "file");

            File.WriteAllText(hiddenFile, "");
            File.WriteAllText(file, "");
            new System.IO.FileInfo(hiddenFile).Attributes = FileAttributes.Hidden;


            // finished setup 

            var root = Kernel.Instance.GetLocation<FolderMediaLocation>(testFolder);

            Assert.AreEqual(4, root.Children.Count);
            Assert.AreEqual(FileAttributes.Hidden, root.GetChild("hiddenFolder").Attributes & FileAttributes.Hidden);
            Assert.AreEqual(FileAttributes.Hidden, root.GetChild("hiddenFile").Attributes & FileAttributes.Hidden);

            Assert.AreNotEqual(FileAttributes.Hidden, root.GetChild("folder").Attributes & FileAttributes.Hidden);
            Assert.AreNotEqual(FileAttributes.Hidden, root.GetChild("file").Attributes & FileAttributes.Hidden);

        } 

        // note this test can take a while.. 
        [Test]
        public void DodgyVfsShouldPartiallyLoad() {

            var vf = Path.Combine(testFolder, "test.vf");

            Directory.CreateDirectory(testFolder);
            var dir1 = Path.Combine(testFolder, "test");
            Directory.CreateDirectory(dir1 + "\\path");

            VirtualFolderContents generator = new VirtualFolderContents("");
            generator.AddFolder(dir1);
            generator.AddFolder(@"\\10.0.0.4\mydir");
           
            File.WriteAllText(vf, generator.Contents);

            var root = Kernel.Instance.GetLocation<VirtualFolderMediaLocation>(vf) ;

            Assert.AreEqual(1, root.Children.Count);
        }

        [Test]
        public void VirtualFoldersCanContainDuplicateFiles() {
            Directory.CreateDirectory(testFolder);

            var dir1 = Path.Combine(testFolder, "test");
            var dir2 = Path.Combine(testFolder, "test2");

            var vf = Path.Combine(testFolder, "test.vf");

            Directory.CreateDirectory(dir1 + "\\path");
            Directory.CreateDirectory(dir2 + "\\path");


            VirtualFolderContents generator = new VirtualFolderContents("");
            generator.AddFolder(dir1);
            generator.AddFolder(dir2);
            
            File.WriteAllText(vf, generator.Contents);

            var root = Kernel.Instance.GetLocation<VirtualFolderMediaLocation>(vf);
            
            Assert.AreEqual(2, root.Children.Count);
            Assert.AreEqual(true, root.ContainsChild("path"));
        } 

        [Test]
        public void TestStandardScanning() {
            CreateTree(3, 10, "hello world");
            var root = Kernel.Instance.GetLocation(testFolder);
            Assert.AreEqual(3, (root as IFolderMediaLocation).Children.Count);

            foreach (var item in (root as IFolderMediaLocation).Children) {
                Assert.AreEqual(10, (item as FolderMediaLocation).Children.Count);
            }
        }


        public void CreateTree(int subDirs, int filesPerSubdir, string fileContents) {
            var info = Directory.CreateDirectory(testFolder);

            for (int i = 0; i < subDirs; i++) {
                var sub = Directory.CreateDirectory(Path.Combine(info.FullName, "SubDir" + i.ToString()));

                for (int j = 0; j < filesPerSubdir; j++) {
                    File.WriteAllText(Path.Combine(sub.FullName,"File" + j.ToString()), fileContents);
                }
            }

        }
    }
}
