using Common;

namespace SyncServer;

public class TaskHandler
{
    private readonly string _destinationDir;

    public TaskHandler(string destinationDir)
    {
        _destinationDir = destinationDir;
    } 
    
    public async Task HandleTask(SyncTask task)
    {
        switch (task)
        {
            case AddDirSyncTask addDirSyncTask:
                AddDir(addDirSyncTask);
                break;
            case AddFileSyncTask fileTask:
                await AddFile(fileTask);
                break;
            case DeleteSyncTask deleteSyncTask:
                Delete(deleteSyncTask); 
                break;
            case RenameSyncTask renameSyncTask:
                Rename(renameSyncTask);
                break;
        } 
    }

    private async Task AddFile(AddFileSyncTask syncTask)
    {
        Console.WriteLine("Received add file sync request");
        try
        {
            var fullPath = _destinationDir + '/' + syncTask.RelativePath;
            await File.WriteAllBytesAsync(fullPath, syncTask.Content);
            File.SetAttributes(fullPath, syncTask.Attributes);
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.Message);
        }
    }
    
    private void AddDir(AddDirSyncTask syncTask)
    {
        Console.WriteLine("Received add dir sync request");
        try
        {
            var fullPath = _destinationDir + '/' + syncTask.RelativePath;
            Directory.CreateDirectory(fullPath);
            File.SetAttributes(fullPath, syncTask.Attributes);
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.Message);
        }
    }
    
    private void Rename(RenameSyncTask syncTask)
    {
        Console.WriteLine("Received rename sync request");
        try
        {
            var fullPathSrc = _destinationDir + '/' + syncTask.RelativePathOld;
            var fullPathDst = _destinationDir + '/' + syncTask.RelativePathNew;
            File.Move(fullPathSrc, fullPathDst);
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.Message);
        }
    }
    
    private void Delete(DeleteSyncTask syncTask)
    {
        Console.WriteLine("Received delete sync request");
        try
        {
            var fullPath = _destinationDir + '/' + syncTask.RelativePath;
            var attributes = File.GetAttributes(fullPath);
            if (attributes.HasFlag(FileAttributes.Directory))
            {
                Directory.Delete(fullPath);
            }
            else
            {
                File.Delete(fullPath);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.Message);
        }
    }
}