using System;
using System.IO;
using System.IO.Abstractions;

namespace Client;

public interface IFileMonitor
{
    event EventHandler<FileRenamedEventArgs> FileRenamed;
    event EventHandler<NewFileEventArgs> NewFile;
    event EventHandler<FileChangedEventArgs> FileChanged;
    event EventHandler<FileDeletedEventArgs> FileDeleted;
}

public class FileMonitor : IFileMonitor
{
    private readonly IFileSystemWatcher _watcher;

    public event EventHandler<FileRenamedEventArgs>? FileRenamed;
    public event EventHandler<NewFileEventArgs>? NewFile;
    public event EventHandler<FileChangedEventArgs>? FileChanged;
    public event EventHandler<FileDeletedEventArgs>? FileDeleted;
    
    public FileMonitor(string sourcePath, IFileSystemWatcher watcher)
    {
        _watcher = watcher;
        _watcher.Path = sourcePath;
        _watcher.NotifyFilter = NotifyFilters.Attributes 
                                | NotifyFilters.DirectoryName 
                                | NotifyFilters.FileName 
                                | NotifyFilters.LastWrite;
        _watcher.IncludeSubdirectories = true;
        _watcher.EnableRaisingEvents = true;
        
        _watcher.Changed += OnChanged; 
        _watcher.Created += OnCreated; 
        _watcher.Deleted += OnDeleted; 
        _watcher.Renamed += OnRenamed; 
        _watcher.Error += OnError; 
    }
  
    private void OnChanged(object sender, FileSystemEventArgs e)
    {
        if (e.Name != null)
        {
            FileChanged?.Invoke(this, new FileChangedEventArgs { RelativePath = e.Name });
        }
    } 
    
    private void OnCreated(object sender, FileSystemEventArgs e)
    {
        if (e.Name != null)
        {
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
        Console.WriteLine($"FileMonitor error: {e.GetException().Message}");
    }
}