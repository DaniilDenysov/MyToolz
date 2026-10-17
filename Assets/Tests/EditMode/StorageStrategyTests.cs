using System;
using System.IO;
using MyToolz.IO;
using NUnit.Framework;
using UnityEngine;

namespace MyToolz.Tests.EditMode
{
    public class FileStorageStrategyTests
    {
        private string _folder;
        private StorageLocation _location;
        private FileStorageStrategy _storage;

        [SetUp]
        public void SetUp()
        {
            _folder = Path.Combine(Application.temporaryCachePath, "MyToolzTests", Guid.NewGuid().ToString("N"));
            string fullPath = Path.Combine(_folder, "save.json");
            _location = new StorageLocation(fullPath, fullPath + ".tmp", fullPath + ".bak", _folder, "save.json");
            _storage = new FileStorageStrategy();
        }

        [TearDown]
        public void TearDown()
        {
            if (Directory.Exists(_folder))
            {
                Directory.Delete(_folder, recursive: true);
            }
        }

        [Test]
        public void Write_CreatesFolderAndFile()
        {
            _storage.Write(_location, "hello");

            Assert.IsTrue(Directory.Exists(_folder), "the target folder is created on demand");
            Assert.IsTrue(_storage.Exists(_location));
        }

        [Test]
        public void Write_ThenReadPrimary_ReturnsContent()
        {
            _storage.Write(_location, "payload");

            Assert.IsTrue(_storage.TryReadPrimary(_location, out string content));
            Assert.AreEqual("payload", content);
        }

        [Test]
        public void ReadPrimary_NoFile_ReturnsFalse()
        {
            Assert.IsFalse(_storage.TryReadPrimary(_location, out string content));
            Assert.IsNull(content);
        }

        [Test]
        public void Overwrite_KeepsPreviousContentAsBackup()
        {
            _storage.Write(_location, "v1");
            _storage.Write(_location, "v2");

            Assert.IsTrue(_storage.TryReadPrimary(_location, out string primary));
            Assert.AreEqual("v2", primary, "primary holds the latest write");

            Assert.IsTrue(_storage.TryReadBackup(_location, out string backup));
            Assert.AreEqual("v1", backup, "the previous good save is retained as .bak");
        }

        [Test]
        public void ReadBackup_NoBackup_ReturnsFalse()
        {
            _storage.Write(_location, "only");
            Assert.IsFalse(_storage.TryReadBackup(_location, out _), "a first write leaves no backup");
        }

        [Test]
        public void Write_DoesNotLeaveTempFileBehind()
        {
            _storage.Write(_location, "content");
            Assert.IsFalse(File.Exists(_location.TempPath), "the temp file is swapped into place, not left around");
        }

        [Test]
        public void Pending_ReadsCompletedTempFile()
        {
            Directory.CreateDirectory(_folder);
            File.WriteAllText(_location.TempPath, "pending");

            Assert.IsTrue(_storage.TryReadPending(_location, out string content));
            Assert.AreEqual("pending", content);
        }

        [Test]
        public void RestorePrimary_DoesNotRotateCorruptPrimaryOverBackup()
        {
            _storage.Write(_location, "good");
            _storage.Write(_location, "corrupt"); // "good" is now the backup

            _storage.RestorePrimary(_location, "good");

            Assert.IsTrue(_storage.TryReadPrimary(_location, out string primary));
            Assert.AreEqual("good", primary);
            Assert.IsTrue(_storage.TryReadBackup(_location, out string backup));
            Assert.AreEqual("good", backup, "the known-good backup is kept");
        }

        [Test]
        public void PreserveCorrupt_WritesDiagnosticCopy_AndDeleteRemovesIt()
        {
            _storage.Write(_location, "bad");
            _storage.PreserveCorrupt(_location, "bad", primary: true);

            Assert.IsTrue(File.Exists(_location.FullPath + ".corrupt"));
            Assert.IsTrue(_storage.AnyDataExists(_location));

            _storage.Delete(_location);
            Assert.IsFalse(_storage.AnyDataExists(_location));
        }

        [Test]
        public void Delete_RemovesPrimaryTempAndBackup()
        {
            _storage.Write(_location, "v1");
            _storage.Write(_location, "v2"); // creates a .bak

            _storage.Delete(_location);

            Assert.IsFalse(_storage.Exists(_location));
            Assert.IsFalse(_storage.TryReadPrimary(_location, out _));
            Assert.IsFalse(File.Exists(_location.BackupPath));
            Assert.IsFalse(File.Exists(_location.TempPath));
        }
    }

    public class PlayerPrefsStorageStrategyTests
    {
        private StorageLocation _location;
        private PlayerPrefsStorageStrategy _storage;
        private string _key;

        [SetUp]
        public void SetUp()
        {
            _key = "MyToolzTests/" + Guid.NewGuid().ToString("N");
            _location = new StorageLocation("unused", "unused", "unused", "unused", _key);
            _storage = new PlayerPrefsStorageStrategy();
        }

        [TearDown]
        public void TearDown()
        {
            _storage.Delete(_location); // removes the primary, rolling backup and diagnostic keys
        }

        [Test]
        public void Write_ThenRead_RoundTrips()
        {
            _storage.Write(_location, "prefs-value");

            Assert.IsTrue(_storage.Exists(_location));
            Assert.IsTrue(_storage.TryReadPrimary(_location, out string content));
            Assert.AreEqual("prefs-value", content);
        }

        [Test]
        public void Exists_IsFalse_BeforeWrite()
        {
            Assert.IsFalse(_storage.Exists(_location));
        }

        [Test]
        public void Overwrite_KeepsPreviousValueAsBackupKey()
        {
            _storage.Write(_location, "v1");
            _storage.Write(_location, "v2");

            Assert.IsTrue(_storage.TryReadBackup(_location, out string backup));
            Assert.AreEqual("v1", backup);
        }

        [Test]
        public void Delete_RemovesKey()
        {
            _storage.Write(_location, "value");
            _storage.Delete(_location);

            Assert.IsFalse(_storage.Exists(_location));
            Assert.IsFalse(_storage.TryReadPrimary(_location, out _));
        }
    }
}
