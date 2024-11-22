using System;
using System.IO;
using System.Security.Cryptography;
using System.Timers;

namespace FolderSync
{
    class Program
    {
        static void Main(string[] args)
        {
            if (args.Length != 4)
            {
                Console.WriteLine("Usage: FolderSync <SourceFolder> <ReplicaFolder> <SyncIntervalSeconds> <LogFilePath>");
                return;
            }

            string sourceFolder = args[0];
            string replicaFolder = args[1];
            int syncIntervalSeconds = int.Parse(args[2]);
            string logFilePath = args[3];

            if (!Directory.Exists(sourceFolder))
            {
                Console.WriteLine($"Source folder '{sourceFolder}' does not exist.");
                return;
            }

            if (!Directory.Exists(replicaFolder))
            {
                Console.WriteLine($"Replica folder '{replicaFolder}' does not exist.");
                return;
            }

            var syncInterval = TimeSpan.FromSeconds(syncIntervalSeconds);
            var synchronizer = new FolderSync(sourceFolder, replicaFolder, logFilePath, syncInterval);

            synchronizer.StartSynchronization();
        }
    }
}