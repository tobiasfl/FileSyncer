using System.IO.Abstractions;
using System.Threading.Channels;
using Common;

namespace Client;


/*
 * Acts as a queue of file syncing tasks, ensuring that incoming changes in the directory
 * are synced to the server in the order they happen and that only one file is read into
 * memory at a time.
 */

public class FileSyncer
{
    private readonly IFileMonitor _monitor;
    private readonly ITransport _transport;
    private readonly string _sourceDirPath;
    private readonly IFileSystem _fileSystem;
    private readonly Channel<Func<Task>> _queuedSyncTasks = Channel.CreateUnbounded<Func<Task>>();
    
    public FileSyncer(string sourceDirPath, ITransport transport, IFileMonitor monitor, IFileSystem fileSystem)
    {
        _sourceDirPath = sourceDirPath;
        _monitor = monitor;
        _transport = transport;
        _fileSystem = fileSystem;
    }

    public void StartSyncing()
    {
        SyncFullSourceDirectory();
        
        _monitor.NewFile += (_, e ) => { EnqueueTask(() => HandleEvent(e));};
        _monitor.FileChanged += (_, e ) => { EnqueueTask(() => HandleEvent(e));};
        _monitor.FileRenamed += (_, e ) => { EnqueueTask(() => HandleEvent(e));};
        _monitor.FileDeleted += (_, e ) => { EnqueueTask(() => HandleEvent(e));};
    }

    public async Task<bool> AwaitEnqueuedEvents()
    {
        try
        {
            return await _queuedSyncTasks.Reader.WaitToReadAsync();
        }
        catch (OperationCanceledException)
        {
            Console.WriteLine("Event processing cancelled");
        }

        return false;
    }
    
    public async Task ProcessEnqueuedEvent()
    {
        try
        {
            var getATaskFunc = await _queuedSyncTasks.Reader.ReadAsync();
            await getATaskFunc();
        }
        catch (OperationCanceledException)
        {
            Console.WriteLine("Event processing cancelled");
        }
    }

    private void EnqueueTask(Func<Task> task)
    {
        _queuedSyncTasks.Writer.TryWrite(task);     
    }

    private void SyncFullSourceDirectory()
    {
        SyncDirectoriesInSource();
        SyncFilesInSource();
    }

    private void SyncDirectoriesInSource()
    {
        IEnumerable<string> subDirectories = [];
        try
        {
            subDirectories = _fileSystem.Directory.EnumerateDirectories(_sourceDirPath, "*", SearchOption.AllDirectories);
        } 
        catch (DirectoryNotFoundException ex)
        {
            Console.WriteLine($"Directory not found {_sourceDirPath}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"error {ex.Message}");
        }

        foreach (var dirPath in subDirectories)
        {
            EnqueueTask(() => SyncDir(dirPath));
        } 
    }

    private async Task SyncDir(string fullPath)
    {
        try
        {
            var attributes = _fileSystem.File.GetAttributes(fullPath);
            var syncTask = new AddDirSyncTask
            {
                RelativePath = Path.GetRelativePath(_sourceDirPath, fullPath),
                Attributes = attributes,
            };

            await _transport.Post(syncTask);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"error {ex.Message}");
        }
    }
    
    private void SyncFilesInSource()
    {
        IEnumerable<string> fileNames = [];
        try
        {
            fileNames = _fileSystem.Directory.EnumerateFiles(_sourceDirPath, "*", SearchOption.AllDirectories);
        } 
        catch (FileNotFoundException ex)
        {
            Console.WriteLine($"File not found");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"error {ex.Message}");
        }

        foreach (var fileName in fileNames)
        {
            EnqueueTask(() => SyncFile(fileName));
        } 
    }

    private async Task SyncFile(string fullPath)
    {
        try
        {
            FileAttributes attributes = _fileSystem.File.GetAttributes(fullPath);
            var bytes = await _fileSystem.File.ReadAllBytesAsync(fullPath);
            var syncTask = new AddFileSyncTask
            {
                RelativePath = Path.GetRelativePath(_sourceDirPath, fullPath),
                Attributes = attributes,
                Content = bytes
            };

            await _transport.Post(syncTask);
        }
        catch (FileNotFoundException ex)
        {
            Console.WriteLine($"File not found {fullPath}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"error {ex.Message}");
        }
    }
   
    private async Task HandleEvent(NewFileEventArgs e)
    {
        try
        {
            var fullPath = ToFullPath(e.RelativePath);
            FileAttributes attributes = _fileSystem.File.GetAttributes(fullPath);
            if (attributes.HasFlag(FileAttributes.Directory))
            {
                await SyncDir(fullPath);
            }
            else if (attributes.HasFlag(FileAttributes.Normal))
            {
                await SyncFile(fullPath);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"error ${e.RelativePath} {ex.Message}");
        }
    }
    
    private async Task HandleEvent(FileChangedEventArgs e)
    {
        // Simply overwrite the whole file on far-end if it changes
        await HandleEvent(new NewFileEventArgs { RelativePath = e.RelativePath });
    }
    
    private async Task HandleEvent(FileRenamedEventArgs e)
    {
        var syncTask = new RenameSyncTask
        {
            RelativePathOld = e.FromRelativePath,
            RelativePathNew = e.ToRelativePath
        };
        await _transport.Post(syncTask);
    }

    private async Task HandleEvent(FileDeletedEventArgs e)
    {
        var syncTask = new DeleteSyncTask()
        {
            RelativePath = e.RelativePath,
        };
        await _transport.Post(syncTask);
    }
    
    private string ToFullPath(string relativePath) => Path.Combine(_sourceDirPath, relativePath);
}