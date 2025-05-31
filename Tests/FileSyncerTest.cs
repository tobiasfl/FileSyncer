using System.IO.Abstractions.TestingHelpers;
using Client;
using Common;
using Moq;

namespace Tests;

public class FileSyncerTest
{
    private  Mock<IFileMonitor> fileMonitorMock = new Mock<IFileMonitor>();
    private  Mock<ITransport> transportMock = new Mock<ITransport>();
    private const string dirPath = "/source_dir";
    private  MockFileSystem fileSystem = new MockFileSystem();

    [Fact]
    public async Task Test_Constructor_InitialSyncPostsSyncEventForOneExistingFile()
    {
        fileSystem.AddDirectory(dirPath);

        const string expectedFileName = "test";
        fileSystem.AddFile($"{dirPath}/{expectedFileName}", new MockFileData("some data"));
        
        var expectedPostedTask = new AddFileSyncTask
            { RelativePath = "test", Content = System.Text.Encoding.Default.GetBytes("some data"), Attributes = FileAttributes.Normal };
        
        var postCalled = new TaskCompletionSource<bool>();
        transportMock.Setup(x => x.Post(It.IsAny<AddFileSyncTask>()))
            .Callback(() => postCalled.SetResult(true));

        var fileSyncer = new FileSyncer(dirPath, transportMock.Object, fileMonitorMock.Object, fileSystem);

        var completedTask = await Task.WhenAny(postCalled.Task, Task.Delay(1000));
        
        Assert.True(completedTask == postCalled.Task, "Timed out");
        transportMock.Verify(x => x.Post(It.Is<AddFileSyncTask>(t => t.RelativePath == "test")), Times.Once); 
        transportMock.Verify(x => x.Post(It.Is<AddFileSyncTask>(t => t.Attributes == FileAttributes.Normal)), Times.Once); 
        transportMock.Verify(x => x.Post(It.Is<AddFileSyncTask>(t => t.Content.SequenceEqual(expectedPostedTask.Content))), Times.Once); 
    }
    
    
    [Fact]
    public async Task Test_Constructor_InitialSyncPostsSyncEventForOneExistingSubDir()
    {
        fileSystem.AddDirectory(dirPath);

        const string expectedDirName = "test_dir";
        fileSystem.AddDirectory($"{dirPath}/{expectedDirName}");
        
        var expectedPostedTask = new AddDirSyncTask
            { RelativePath = expectedDirName, Attributes = FileAttributes.Directory };
        
        var postCalled = new TaskCompletionSource<bool>();
        transportMock.Setup(x => x.Post(It.IsAny<AddDirSyncTask>()))
            .Callback(() => postCalled.SetResult(true));

        var fileSyncer = new FileSyncer(dirPath, transportMock.Object, fileMonitorMock.Object, fileSystem);

        var completedTask = await Task.WhenAny(postCalled.Task, Task.Delay(1000));
        
        Assert.True(completedTask == postCalled.Task, "Timed out");
        transportMock.Verify(x => x.Post(expectedPostedTask), Times.Once); 
    }
    
    [Fact]
    public async Task Test_Constructor_InitialSyncPostsSyncEventForFilesInsideSubDir()
    {
        fileSystem.AddDirectory(dirPath);
        const string subDir = $"{dirPath}/sub_dir";
        fileSystem.AddDirectory(subDir);
        
        const string firstFile = $"{subDir}/file1";
        fileSystem.AddFile(firstFile, new MockFileData(""));
        
        const string secondFile = $"{subDir}/file2";
        fileSystem.AddFile(secondFile, new MockFileData(""));
        
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
        
        transportMock.Setup(x => x.Post(It.IsAny<AddFileSyncTask>()))
            .Callback(incrementCb);
        transportMock.Setup(x => x.Post(It.IsAny<AddDirSyncTask>()))
            .Callback(incrementCb);

        var fileSyncer = new FileSyncer(dirPath, transportMock.Object, fileMonitorMock.Object, fileSystem);

        var completedTask = await Task.WhenAny(allPostsComplete.Task, Task.Delay(1000));
        
        Assert.True(completedTask == allPostsComplete.Task, "Timed out");
        transportMock.Verify(x => x.Post(It.Is<AddFileSyncTask>(t => t.RelativePath == "sub_dir/file1")), Times.Once); 
        transportMock.Verify(x => x.Post(It.Is<AddFileSyncTask>(t => t.RelativePath == "sub_dir/file2")), Times.Once); 
    }
    
    [Fact]
    public async Task Test_Constructor_InitialSyncPostsSyncEventForDirInsideSubDir()
    {
        fileSystem.AddDirectory(dirPath);
        const string subDir = $"{dirPath}/sub_dir";
        fileSystem.AddDirectory(subDir);
        
        const string nestedSubDir = $"{subDir}/another_dir";
        fileSystem.AddDirectory(nestedSubDir);
       
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
        
        transportMock.Setup(x => x.Post(It.IsAny<AddDirSyncTask>()))
            .Callback(incrementCb);

        var fileSyncer = new FileSyncer(dirPath, transportMock.Object, fileMonitorMock.Object, fileSystem);

        var completedTask = await Task.WhenAny(allPostsComplete.Task, Task.Delay(1000));
        
        Assert.True(completedTask == allPostsComplete.Task, "Timed out");
        transportMock.Verify(x => x.Post(new AddDirSyncTask
        {
            RelativePath = "sub_dir",
            Attributes = FileAttributes.Directory
        }), Times.Once); 
        transportMock.Verify(x => x.Post(new AddDirSyncTask
        {
            RelativePath = "sub_dir/another_dir",
            Attributes = FileAttributes.Directory
        }), Times.Once);
    }
}