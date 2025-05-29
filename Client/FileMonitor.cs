using System;
using System.IO;
using System.IO.Abstractions;

namespace Client;

//TODO: remember the receiver only needs to know relative path (we don't know the full path at the client)
public class FileRenamedEventArgs : EventArgs
{
    public required string FromRelativePath { get; init; }
    public required string ToRelativePath { get; init; } 
}

public class FileChangedEventArgs : EventArgs
{
    public required string RelativePath { get; init; }
}

public class NewFileEventArgs : EventArgs
{
    public required string RelativePath { get; init; } 
}

public class FileDeletedEventArgs : EventArgs
{
    public required string RelativePath { get; init; } 
}

public class FileMonitor
{
    private readonly IFileSystemWatcher _watcher;

    //TODO: private and include in ctor maybe
    public event EventHandler<FileRenamedEventArgs> FileRenamed;
    public event EventHandler<NewFileEventArgs> NewFile;
    public event EventHandler<FileChangedEventArgs> FileChanged;
    public event EventHandler<FileDeletedEventArgs> FileDeleted;
    
    public FileMonitor(string sourcePath, IFileSystemWatcher watcher)
    {
        _watcher = watcher;
        _watcher.Path = sourcePath;
        _watcher.NotifyFilter = NotifyFilters.Attributes 
                                | NotifyFilters.DirectoryName 
                                | NotifyFilters.FileName 
                                | NotifyFilters.Security 
                                | NotifyFilters.LastWrite;
        _watcher.IncludeSubdirectories = true;
        _watcher.EnableRaisingEvents = true;
        
        _watcher.Changed += OnChanged; 
        _watcher.Created += OnCreated; 
        _watcher.Deleted += OnDeleted; 
        _watcher.Renamed += OnRenamed; 
        _watcher.Error += OnError; 
       
        //TODO: watcher.fileSystem (start with all existing files)
    }
  
    private void OnChanged(object sender, FileSystemEventArgs e)
    {
        //TODO: debouncing?
        if (e.Name != null)
        {
            FileChanged?.Invoke(this, new FileChangedEventArgs { RelativePath = e.Name });
        }
    } 
    
    private void OnCreated(object sender, FileSystemEventArgs e)
    {
        if (e.Name != null)
        {
            Console.WriteLine(e.FullPath);
            Console.WriteLine(e.Name);
            NewFile?.Invoke(this, new NewFileEventArgs { RelativePath = e.Name });
        }
    } 
    
    private void OnDeleted(object sender, FileSystemEventArgs e)
    {
        if (e.Name != null)
        {
            FileDeleted?.Invoke(this, new FileDeletedEventArgs { RelativePath = e.Name });
        }
    } 
    
    private void OnRenamed(object sender, RenamedEventArgs e)
    {

        if (e.Name != null && e.OldName != null)
        {
            FileRenamed?.Invoke(this,
                new FileRenamedEventArgs { FromRelativePath = e.OldName, ToRelativePath = e.Name });
        }
    } 
   
    private void OnError(object sender, ErrorEventArgs e)
    {
        Console.WriteLine($"error message: {e.GetException().Message}");
    }
}