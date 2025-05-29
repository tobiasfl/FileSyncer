using System.Collections.Concurrent;
using System.IO.Abstractions;
using System.Text;
using System.Text.Json;
using Common;

namespace Client;

public class FileSyncer
{
    private readonly FileMonitor _monitor;
    private readonly ITransport _transport;
    private readonly string _sourceDirPath;
    private readonly BlockingCollection<Func<Task>> _queuedSyncTasks = new BlockingCollection<Func<Task>>();
    private bool _initialSyncDone = false;
    
    public FileSyncer(string sourceDirPath, ITransport transport)
    {
        _sourceDirPath = sourceDirPath;
        _monitor = new FileMonitor(sourceDirPath, new FileSystemWatcherWrapper(new FileSystem()));
        _monitor.NewFile += (_, e ) => { EnqueueTask(() => HandleEvent(e));};
        _monitor.FileChanged += (_, e ) => { EnqueueTask(() => HandleEvent(e));};
        _transport = transport;
        
    }
  
    private void EnqueueTask(Func<Task> task)
    {
        if (!_queuedSyncTasks.IsAddingCompleted)
        {
            _queuedSyncTasks.Add(task);     
        }
    }
    
    public async Task ProcessEvents()
    {
        if (!_initialSyncDone)
        {
            await SyncFullSourceDirectory();
            _initialSyncDone = true;
        }
        
        try
        {
            foreach (var getATaskFunc in _queuedSyncTasks.GetConsumingEnumerable())
            {
                Console.WriteLine("processing");
                try
                {
                    await getATaskFunc();
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error handling event: {ex.Message}");
                }
            }
        }
        catch (OperationCanceledException)
        {
            Console.WriteLine("Event processing cancelled");
        }
    }


    private async Task SyncFullSourceDirectory()
    {
        string[]? filesAndDirs = null;
        try
        {
            filesAndDirs = Directory.GetFiles(_sourceDirPath, "*.*", SearchOption.AllDirectories);
        } 
        catch (FileNotFoundException ex)
        {
            Console.WriteLine($"File not found");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"error {ex.Message}");
        }

        if (filesAndDirs == null)
        {
            return;
        }
        var fullPath = _sourceDirPath;
        foreach (var fileOrDir in filesAndDirs)
        {
            try
            {
                FileAttributes attributes = File.GetAttributes($"{fullPath}/{fileOrDir}");
                if (attributes.HasFlag(FileAttributes.Directory))
                {
                    var syncTask = new AddDirSyncTask
                    {
                        RelativePath = fileOrDir,
                        Attributes = attributes,
                    };

                    await _transport.Post(syncTask);
                }
                else if (attributes.HasFlag(FileAttributes.Normal))
                {
                    var bytes = await File.ReadAllBytesAsync(fullPath);
                    var syncTask = new AddFileSyncTask
                    {
                        RelativePath = fileOrDir,
                        Attributes = attributes,
                        Content = bytes
                    };

                    await _transport.Post(syncTask);
                }
            }
            catch (FileNotFoundException ex)
            {
                Console.WriteLine($"File not found");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"error {ex.Message}");
            }
        } 
    }

    private async Task HandleEvent(NewFileEventArgs e)
    {
        try
        {
            var fullPath = ToFullPath(e.RelativePath);
            FileAttributes attributes = File.GetAttributes(fullPath);
            if (attributes.HasFlag(FileAttributes.Directory))
            {
                var syncTask = new AddDirSyncTask
                {
                    RelativePath = e.RelativePath,
                    Attributes = attributes,
                };

                await _transport.Post(syncTask);
            }
            else if (attributes.HasFlag(FileAttributes.Normal))
            {
                var bytes = await File.ReadAllBytesAsync(fullPath);
                var syncTask = new AddFileSyncTask
                {
                    RelativePath = e.RelativePath,
                    Attributes = attributes,
                    Content = bytes
                };

                await _transport.Post(syncTask);
            }
        }
        catch (FileNotFoundException ex)
        {
            Console.WriteLine($"File not found ${e.RelativePath}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"error ${e.RelativePath} {ex.Message}");
        }
    }
    
    private async Task HandleEvent(FileChangedEventArgs e)
    {
        await HandleEvent(new NewFileEventArgs { RelativePath = e.RelativePath });
    }

    private string ToFullPath(string relativePath) => Path.Combine(_sourceDirPath, relativePath);
}