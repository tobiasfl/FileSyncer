using System.IO.Abstractions.TestingHelpers;
using Client;
using Common;
using Moq;

namespace Tests;

public class FileSyncerTest
{
    private readonly Mock<IFileMonitor> _fileMonitorMock = new Mock<IFileMonitor>();
    private readonly Mock<ITransport> _transportMock = new Mock<ITransport>();
    private const string DirPath = "/source_dir";
    private readonly MockFileSystem _fileSystem = new MockFileSystem();

    [Fact]
    public async Task Test_Constructor_InitialSyncPostsSyncEventForOneExistingFile()
    {
        _fileSystem.AddDirectory(DirPath);

        const string expectedFileName = "test";
        _fileSystem.AddFile($"{DirPath}/{expectedFileName}", new MockFileData("some data"));
        
        var expectedPostedTask = new AddFileSyncTask
            { RelativePath = "test", Content = System.Text.Encoding.Default.GetBytes("some data"), Attributes = FileAttributes.Normal };
        
        var postCalled = new TaskCompletionSource<bool>();
        _transportMock.Setup(x => x.Post(It.IsAny<AddFileSyncTask>()))
            .Callback(() => postCalled.SetResult(true));

        var fileSyncer = new FileSyncer(DirPath, _transportMock.Object, _fileMonitorMock.Object, _fileSystem);

        var completedTask = await Task.WhenAny(postCalled.Task, Task.Delay(1000));
        
        Assert.True(completedTask == postCalled.Task, "Timed out");
        _transportMock.Verify(x => x.Post(It.Is<AddFileSyncTask>(t => t.RelativePath == "test")), Times.Once); 
        _transportMock.Verify(x => x.Post(It.Is<AddFileSyncTask>(t => t.Attributes == FileAttributes.Normal)), Times.Once); 
        _transportMock.Verify(x => x.Post(It.Is<AddFileSyncTask>(t => t.Content.SequenceEqual(expectedPostedTask.Content))), Times.Once); 
    }

    [Fact]
    public async Task Test_Constructor_InitialSyncPostsSyncEventForOneExistingSubDir()
    {
        _fileSystem.AddDirectory(DirPath);

        const string expectedDirName = "test_dir";
        _fileSystem.AddDirectory($"{DirPath}/{expectedDirName}");
        
        var expectedPostedTask = new AddDirSyncTask
            { RelativePath = expectedDirName, Attributes = FileAttributes.Directory };
        
        var postCalled = new TaskCompletionSource<bool>();
        _transportMock.Setup(x => x.Post(It.IsAny<AddDirSyncTask>()))
            .Callback(() => postCalled.SetResult(true));

        var fileSyncer = new FileSyncer(DirPath, _transportMock.Object, _fileMonitorMock.Object, _fileSystem);

        var completedTask = await Task.WhenAny(postCalled.Task, Task.Delay(1000));
        
        Assert.True(completedTask == postCalled.Task, "Timed out");
        _transportMock.Verify(x => x.Post(expectedPostedTask), Times.Once); 
    }
    
    [Fact]
    public async Task Test_Constructor_InitialSyncPostsSyncEventForFilesInsideSubDir()
    {
        _fileSystem.AddDirectory(DirPath);
        const string subDir = $"{DirPath}/sub_dir";
        _fileSystem.AddDirectory(subDir);
        
        const string firstFile = $"{subDir}/file1";
        _fileSystem.AddFile(firstFile, new MockFileData(""));
        
        const string secondFile = $"{subDir}/file2";
        _fileSystem.AddFile(secondFile, new MockFileData(""));
        
        int posts = 0;
        const int expectedPosts = 3; 
        var allPostsComplete = new TaskCompletionSource<bool>();

        var incrementCb = () =>
        {
            if (Interlocked.Increment(ref posts) >= expectedPosts)
            {
                allPostsComplete.TrySetResult(true);
            }
        };
        
        _transportMock.Setup(x => x.Post(It.IsAny<AddFileSyncTask>()))
            .Callback(incrementCb);
        _transportMock.Setup(x => x.Post(It.IsAny<AddDirSyncTask>()))
            .Callback(incrementCb);

        var fileSyncer = new FileSyncer(DirPath, _transportMock.Object, _fileMonitorMock.Object, _fileSystem);

        var completedTask = await Task.WhenAny(allPostsComplete.Task, Task.Delay(1000));
        
        Assert.True(completedTask == allPostsComplete.Task, "Timed out");
        _transportMock.Verify(x => x.Post(It.Is<AddFileSyncTask>(t => t.RelativePath == "sub_dir/file1")), Times.Once); 
        _transportMock.Verify(x => x.Post(It.Is<AddFileSyncTask>(t => t.RelativePath == "sub_dir/file2")), Times.Once); 
    }
    
    [Fact]
    public async Task Test_Constructor_InitialSyncPostsSyncEventForDirInsideSubDir()
    {
        _fileSystem.AddDirectory(DirPath);
        const string subDir = $"{DirPath}/sub_dir";
        _fileSystem.AddDirectory(subDir);
        
        const string nestedSubDir = $"{subDir}/another_dir";
        _fileSystem.AddDirectory(nestedSubDir);
       
        int posts = 0;
        const int expectedPosts = 2; 
        var allPostsComplete = new TaskCompletionSource<bool>();

        var incrementCb = () =>
        {
            if (Interlocked.Increment(ref posts) >= expectedPosts)
            {
                allPostsComplete.TrySetResult(true);
            }
        };
        
        _transportMock.Setup(x => x.Post(It.IsAny<AddDirSyncTask>()))
            .Callback(incrementCb);

        var fileSyncer = new FileSyncer(DirPath, _transportMock.Object, _fileMonitorMock.Object, _fileSystem);

        var completedTask = await Task.WhenAny(allPostsComplete.Task, Task.Delay(1000));
        
        Assert.True(completedTask == allPostsComplete.Task, "Timed out");
        _transportMock.Verify(x => x.Post(new AddDirSyncTask
        {
            RelativePath = "sub_dir",
            Attributes = FileAttributes.Directory
        }), Times.Once); 
        _transportMock.Verify(x => x.Post(new AddDirSyncTask
        {
            RelativePath = "sub_dir/another_dir",
            Attributes = FileAttributes.Directory
        }), Times.Once);
    }
}