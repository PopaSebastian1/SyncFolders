using System;
using System.IO;
using System.Threading;
using log4net;
using log4net.Config;

namespace FolderSync
{
    class Program
    {
        private static FolderSync folderSync;
        private static Timer syncTimer;

        public static void Main(string[] args)
        {
            if (args.Length < 4)
            {
                Console.WriteLine("Usage: Veeam <sourcePath> <destinationPath> <intervalInSeconds> <logFilePath>");
                return;
            }

            string sourcePath = args[0];
            string destinationPath = args[1];
            int interval = int.Parse(args[2]);
            string logFilePath = args[3];

            string logDirectory = Path.GetDirectoryName(logFilePath);
            if (!Directory.Exists(logDirectory))
            {
                Directory.CreateDirectory(logDirectory);
            }

            folderSync = new FolderSync(sourcePath, destinationPath, interval * 1000, logFilePath);

            syncTimer = new Timer(SyncCallback, null, 0, interval * 1000);

            Console.WriteLine("Press 'Enter' to stop...");
            Console.ReadLine();

            syncTimer.Change(Timeout.Infinite, Timeout.Infinite);
        }

        private static void SyncCallback(object state)
        {
            folderSync.Sync();
            Console.WriteLine("Sync completed.");
        }
    }
}
