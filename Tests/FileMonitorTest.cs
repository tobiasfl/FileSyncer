using System.IO.Abstractions;
using System.IO.Abstractions.TestingHelpers;
using Client;
using Moq;

namespace Tests;

public class FileMonitorTest
{
    private readonly MockFileSystem _fileSystem = new MockFileSystem();

    [Fact]
    public void Test_NewFile_TriggersWhenNewFileInDir()
    {
        var fileSystemWatcherMock = new Mock<IFileSystemWatcher>();

        var dirPath = "/source_dir";
        var fileMonitor = new FileMonitor(dirPath, fileSystemWatcherMock.Object);

        NewFileEventArgs? newFileEvent = null;
        fileMonitor.NewFile += (sender, eventArgs) => { newFileEvent = eventArgs; };

        fileSystemWatcherMock.Raise(fsw => fsw.Created += null,
            new FileSystemEventArgs(WatcherChangeTypes.Created, dirPath, "test.txt"));

        Assert.NotNull(newFileEvent);
    }

    [Fact]
    public void Test_NewFile_NewFileEventArgsContainsRelativeFilePath()
    {
        var fileSystemWatcherMock = new Mock<IFileSystemWatcher>();

        var dirPath = "/source_dir";
        var fileMonitor = new FileMonitor(dirPath, fileSystemWatcherMock.Object);

        NewFileEventArgs? newFileEvent = null;
        fileMonitor.NewFile += (sender, eventArgs) => { newFileEvent = eventArgs; };

        var expectedFileName = "test.txt";
        fileSystemWatcherMock.Raise(fsw => fsw.Created += null,
            new FileSystemEventArgs(WatcherChangeTypes.Created, $"{dirPath}/{expectedFileName}", expectedFileName));

        Assert.Equal(newFileEvent?.RelativePath, expectedFileName);
    }

    [Fact]
    public void Test_NewFile_NewFileEventArgsContainsRelativeFilePathWhenInSubDir()
    {
        var fileSystemWatcherMock = new Mock<IFileSystemWatcher>();

        var dirPath = "/source_dir";
        var fileMonitor = new FileMonitor(dirPath, fileSystemWatcherMock.Object);

        NewFileEventArgs? newFileEvent = null;
        fileMonitor.NewFile += (sender, eventArgs) => { newFileEvent = eventArgs; };

        var expectedFileName = "subdir/test.txt";
        fileSystemWatcherMock.Raise(fsw => fsw.Created += null,
            new FileSystemEventArgs(WatcherChangeTypes.Created, $"{dirPath}/{expectedFileName}", expectedFileName));

        Assert.Equal(newFileEvent?.RelativePath, expectedFileName);
    }
    
    [Fact]
    public void Test_FileChanged_TriggersWhenFileChanged()
    {
        var fileSystemWatcherMock = new Mock<IFileSystemWatcher>();

        var dirPath = "/source_dir";
        var fileMonitor = new FileMonitor(dirPath, fileSystemWatcherMock.Object);

        FileChangedEventArgs?  fileChangedEvent = null;
        fileMonitor.FileChanged += (sender, eventArgs) => {  fileChangedEvent = eventArgs; };

        var expectedFileName = "test.txt";
        fileSystemWatcherMock.Raise(fsw => fsw.Changed += null,
            new FileSystemEventArgs(WatcherChangeTypes.Changed, dirPath, expectedFileName));

        Assert.Equal(fileChangedEvent?.RelativePath, expectedFileName);
    }
}
