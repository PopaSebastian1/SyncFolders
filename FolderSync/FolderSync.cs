using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Threading.Tasks;
using log4net;

namespace FolderSync
{
    public class FolderSync
    {
        public Folder SourceFolder { get; set; }
        public Folder DestinationFolder { get; set; }
        public int SyncInterval { get; set; }
        public string LogFile { get; set; }
        private DateTime LastSyncTime { get; set; }
        private static readonly object logLock = new object();

        public FolderSync(string sourceFolder, string destinationFolder, int syncInterval, string logFile)
        {
            SourceFolder = new Folder(sourceFolder);
            DestinationFolder = new Folder(destinationFolder);
            SyncInterval = syncInterval;
            LogFile = logFile;
            LastSyncTime = DateTime.MinValue;
        }

        public void Sync()
        {
            SyncFolder(SourceFolder.Path, DestinationFolder.Path);
            LastSyncTime = DateTime.Now;
        }

        private void SyncFolder(string sourcePath, string destinationPath)
        {
            var sourceDir = new DirectoryInfo(sourcePath);
            var destDir = new DirectoryInfo(destinationPath);

            if (!destDir.Exists)
            {
                Directory.CreateDirectory(destinationPath);
            }

            var sourceFiles = sourceDir.GetFiles("*", SearchOption.TopDirectoryOnly).ToDictionary(file => file.Name);
            var destFiles = destDir.GetFiles("*", SearchOption.TopDirectoryOnly);

            Parallel.ForEach(destFiles, (file) =>
            {
                if (!sourceFiles.ContainsKey(file.Name))
                {
                    file.Delete();
                    Log($"File {file.FullName} has been deleted from destination.");
                    Console.WriteLine($"File {file.FullName} has been deleted from destination.");
                }
            });

            var md5 = MD5.Create();

            string CalculateMD5(FileInfo file)
            {
                using (var md5 = MD5.Create())
                {
                    using (var stream = file.OpenRead())
                    {
                        var hash = md5.ComputeHash(stream);
                        return BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
                    }
                }
            }

            Parallel.ForEach(sourceFiles.Values, (file) =>
            {
                if (file.LastAccessTime < LastSyncTime)
                {
                    return;
                }
                var fileHash = CalculateMD5(file);
                if (!destFiles.Any(df => df.Name == file.Name && CalculateMD5(df) == fileHash))
                {
                    string destinationFilePath = Path.Combine(destinationPath, file.Name);
                    file.CopyTo(destinationFilePath, true);
                    Log($"File {file.FullName} has been synchronized to {destinationFilePath}.");
                    Console.WriteLine($"File {file.FullName} has been synchronized to {destinationFilePath}.");
                }
            });

            var sourceSubDirs = sourceDir.GetDirectories().ToDictionary(dir => dir.Name);
            var destSubDirs = destDir.GetDirectories();

            Parallel.ForEach(destSubDirs, (subDir) =>
            {
                if (!sourceSubDirs.ContainsKey(subDir.Name))
                {
                    subDir.Delete(true); 
                    Log($"Directory {subDir.FullName} has been deleted from destination.");
                    Console.WriteLine($"Directory {subDir.FullName} has been deleted from destination.");
                }
            });

            foreach (var sourceSubDir in sourceSubDirs.Values)
            {
                string destSubDirPath = Path.Combine(destinationPath, sourceSubDir.Name);
                SyncFolder(sourceSubDir.FullName, destSubDirPath);
            }
        }


        private void Log(string message)
        {
            try
            {
                lock (logLock)
                {
                    using (StreamWriter sw = File.AppendText(LogFile))
                    {
                        sw.WriteLine($"{DateTime.Now}: {message}");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error writing to log file: {ex.Message}");
            }
        }
    }
}
