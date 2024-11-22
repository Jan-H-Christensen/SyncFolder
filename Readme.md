# Test Task Synchronize two Folder :page_facing_up:

Hi, I'm Jan. Thank you for this opportunity to showcase my skills.

To create a robust folder synchronization solution, I broke down the task into smaller, manageable steps:

1 **File Copying:** I began by implementing a core function to copy files from a source folder to a replica folder. This ensures that the replica remains a mirror image of the source.
2 **Periodic Synchronization:** To maintain consistency, I integrated a task scheduler that triggers the synchronization process at regular intervals. This ensures that changes in the source folder are promptly reflected in the replica.
3 **Hash Verification:** To guarantee data integrity, I incorporated a hashing mechanism. By calculating the MD5 hash of both the source and target files, I can detect any discrepancies and prevent corrupted files from being copied. If a mismatch is detected, the potentially corrupted file in the replica folder is removed.

This approach ensures reliable and accurate file synchronization, safeguarding data integrity and preventing data loss.

## Run the console application :tada:

```
dotnet run -- SourceFolder ReplikaFolder TaskTimer LogFileLocation
```

example

```
dotnet run --  D:\test\SyncFolder D:\test\ReplFolder 30 D:\test\log.txt
```
