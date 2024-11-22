using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Security.Cryptography;


namespace FolderSync
{
    class FolderSync
    {
        private string sourceFolder;
        private string replicaFolder;
        private string logFilePath;
        private TimeSpan syncInterval;
        private readonly object logLock = new object();

        public FolderSync(string sourceFolder, string replicaFolder, string logFilePath, TimeSpan syncInterval)
        {
            this.sourceFolder = sourceFolder;
            this.replicaFolder = replicaFolder;
            this.logFilePath = logFilePath;
            this.syncInterval = syncInterval;
        }

        /// <summary>
        /// Starts a continuous synchronization loop that checks for new files and
        /// updates them in the replica every <see cref="syncInterval"/> seconds.
        /// </summary>
        public void StartSynchronization()
        {
            while (true)
            {
                SynchronizeFolders();
                LogMessage($"Synchronization complete. Waiting for {syncInterval.TotalSeconds} seconds...");
                // Sleep for the specified interval and then repeat the loop
                Thread.Sleep(syncInterval);
            }
        }

        /// <summary>
        /// Synchronizes the source folder with the replica folder.
        /// </summary>
        private void SynchronizeFolders()
        {
            try
            {
                SyncFiles(sourceFolder, replicaFolder);

                RemoveFilesNotInSource(sourceFolder, replicaFolder);
            }
            catch (Exception ex)
            {
                LogMessage($"Error during synchronization: {ex.Message}");
            }
        }

        /// <summary>
        /// Synchronizes files from the source directory to the replica directory.
        /// Files in the source directory that do not exist in the replica directory
        /// are copied there. Files that exist in both directories but are out of
        /// sync are deleted from the replica directory and re-copied from the
        /// source directory.
        /// </summary>
        /// <param name="source">The path to the source directory.</param>
        /// <param name="replica">The path to the replica directory.</param>
        private void SyncFiles(string source, string replica)
        {
            var sourceFiles = Directory.GetFiles(source, "*", SearchOption.AllDirectories);
            foreach (var sourceFile in sourceFiles)
            {
                var relativePath = Path.GetRelativePath(sourceFolder, sourceFile);

                var replicaFile = Path.Combine(replicaFolder, relativePath);

                if (!File.Exists(replicaFile))
                {
                    CopyFileToReplica(sourceFile, replicaFile);
                }
                else if (!AreFilesIdentical(sourceFile, replicaFile))
                {
                    DeleteFileFromReplica(replicaFile);
                    CopyFileToReplica(sourceFile, replicaFile);
                }
            }
        }


        /// <summary>
        /// Removes files from the replica directory that are no longer in the source directory.
        /// </summary>
        /// <param name="source">The source directory path.</param>
        /// <param name="replica">The replica directory path.</param>
        /// <remarks>
        /// This method is called after SyncFiles to remove any files that are no longer
        /// in the source directory from the replica directory.
        /// </remarks>
        private void RemoveFilesNotInSource(string source, string replica)
        {
            var replicaFiles = Directory.GetFiles(replica, "*", SearchOption.AllDirectories);

            foreach (var replicaFile in replicaFiles)
            {
                var relativePath = Path.GetRelativePath(replicaFolder, replicaFile);
                var sourceFile = Path.Combine(sourceFolder, relativePath);

                if (!File.Exists(sourceFile))
                    DeleteFileFromReplica(replicaFile);

            }
        }

        /// <summary>
        /// Compares two files and returns true if they are identical, false if not.
        /// </summary>
        /// <param name="sourceFile">The path to the source file.</param>
        /// <param name="replicaFile">The path to the replica file.</param>
        /// <returns>True if the files are identical, false if not.</returns>
        /// <remarks>
        /// Files are considered identical if their hashes are equal.
        /// If an exception occurs while computing the hash, the exception
        /// is logged and false is returned.
        /// </remarks>
        private bool AreFilesIdentical(string sourceFile, string replicaFile)
        {
            try
            {
                string sourceHash = ComputeHash(sourceFile);
                string replicaHash = ComputeHash(replicaFile);
                return sourceHash == replicaHash;
            }
            catch (Exception ex)
            {
                LogMessage($"Error comparing files: {sourceFile} and {replicaFile}: {ex.Message}");
                return false;
            }
        }


        /// <summary>
        /// Computes the SHA-256 hash of a given file.
        /// </summary>
        /// <param name="filePath">The path to the file to hash.</param>
        /// <returns>The hash value as a hexadecimal string.</returns>
        /// <remarks>
        /// If an exception occurs while computing the hash, the exception
        /// is logged and re-thrown.
        /// </remarks>
        private string ComputeHash(string filePath)
        {
            try
            {
                using (var stream = File.OpenRead(filePath))
                using (var sha256 = SHA256.Create())
                {
                    var hash = sha256.ComputeHash(stream);
                    return BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
                }
            }
            catch (Exception ex)
            {
                LogMessage($"Error computing hash for file {filePath}: {ex.Message}");
                throw;
            }
        }


        /// <summary>
        /// Copies a file from the source directory to the replica directory.
        /// </summary>
        /// <param name="sourceFile">The path to the source file.</param>
        /// <param name="replicaFile">The path to the replica file.</param>
        /// <remarks>
        /// The file is copied using <see cref="File.Copy(string,string,bool)"/>,
        /// and the operation is verified by calling <see cref="AreFilesIdentical(string,string)"/>.
        /// If the verification fails, a warning message is logged. If any exception
        /// occurs, an error message is logged.
        /// </remarks>
        private void CopyFileToReplica(string sourceFile, string replicaFile)
        {
            try
            {
                var replicaDir = Path.GetDirectoryName(replicaFile);
                if (!Directory.Exists(replicaDir) && !string.IsNullOrEmpty(replicaDir))
                    Directory.CreateDirectory(replicaDir);


                File.Copy(sourceFile, replicaFile, true);

                if (AreFilesIdentical(sourceFile, replicaFile))
                {
                    LogMessage($"File copied successfully: {sourceFile} -> {replicaFile}");
                }
                else
                {
                    LogMessage($"File copy verification failed: {sourceFile} -> {replicaFile}");
                }
            }
            catch (Exception ex)
            {
                LogMessage($"Error copying file {sourceFile} to {replicaFile}: {ex.Message}");
            }
        }


        /// <summary>
        /// Deletes a file from the replica directory.
        /// </summary>
        /// <param name="replicaFile">The path to the replica file.</param>
        /// <remarks>
        /// The file is removed silently if an exception occurs.
        /// </remarks>
        private void DeleteFileFromReplica(string replicaFile)
        {
            try
            {
                File.Delete(replicaFile);
                LogMessage($"File removed: {replicaFile}");
            }
            catch (Exception ex)
            {
                LogMessage($"Error deleting file {replicaFile}: {ex.Message}");
            }
        }

        /// <summary>
        /// Writes a message to the console and appends it to the log file.
        /// </summary>
        /// <param name="message">The message to log.</param>
        /// <remarks>
        /// The log file is locked to prevent concurrent log messages from being written
        /// simultaneously. If an exception occurs while writing to the log file,
        /// the error message is written to the console instead.
        /// </remarks>
        private void LogMessage(string message)
        {
            Console.WriteLine(message);
            lock (logLock)
            {
                try
                {
                    using (StreamWriter logFile = new StreamWriter(logFilePath, true))
                    {
                        logFile.WriteLine($"{DateTime.Now}: {message}");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error logging message: {ex.Message}");
                }
            }
        }
    }
}
