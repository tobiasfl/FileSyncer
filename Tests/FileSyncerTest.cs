using System.IO.Abstractions.TestingHelpers;
using Client;
using Common;
using Moq;

namespace Tests;

public class FileSyncerTest
{
    private readonly Mock<IFileMonitor> _fileMonitorMock = new Mock<IFileMonitor>();
    private readonly Mock<ITransport> _transportMock = new Mock<ITransport>(MockBehavior.Strict);
    private const string DirPath = "/source_dir";
    private readonly MockFileSystem _fileSystem = new MockFileSystem();

    [Fact]
    public async Task Test_StartSyncing_InitialSyncPostsSyncEventForOneExistingFile()
    {
        _fileSystem.AddDirectory(DirPath);

        const string expectedFileName = "test";
        _fileSystem.AddFile($"{DirPath}/{expectedFileName}", new MockFileData("some data"));
        
        var expectedPostedTask = new AddFileSyncTask
            { RelativePath = "test", Content = System.Text.Encoding.Default.GetBytes("some data"), Attributes = FileAttributes.Normal };
        
        var fileSyncer = new FileSyncer(DirPath, _transportMock.Object, _fileMonitorMock.Object, _fileSystem);
        fileSyncer.StartSyncing();
        await fileSyncer.ProcessEnqueuedEvent();

        _transportMock.Verify(x => x.Post(It.Is<AddFileSyncTask>(t => t.RelativePath == "test")), Times.Once); 
        _transportMock.Verify(x => x.Post(It.Is<AddFileSyncTask>(t => t.Attributes == FileAttributes.Normal)), Times.Once); 
        _transportMock.Verify(x => x.Post(It.Is<AddFileSyncTask>(t => t.Content.SequenceEqual(expectedPostedTask.Content))), Times.Once); 
    }

    [Fact]
    public async Task Test_StartSyncing_InitialSyncPostsSyncEventForOneExistingSubDir()
    {
        _fileSystem.AddDirectory(DirPath);

        const string expectedDirName = "test_dir";
        _fileSystem.AddDirectory($"{DirPath}/{expectedDirName}");
        
        var fileSyncer = new FileSyncer(DirPath, _transportMock.Object, _fileMonitorMock.Object, _fileSystem);
        fileSyncer.StartSyncing();
        await fileSyncer.ProcessEnqueuedEvent();
        
        _transportMock.Verify(x => x.Post(new AddDirSyncTask
            { RelativePath = expectedDirName, Attributes = FileAttributes.Directory }), Times.Once); 
    }
    
    [Fact]
    public async Task Test_StartSyncing_InitialSyncPostsSyncEventForFilesInsideSubDir()
    {
        _fileSystem.AddDirectory(DirPath);
        const string subDir = $"{DirPath}/sub_dir";
        _fileSystem.AddDirectory(subDir);
        
        const string firstFile = $"{subDir}/file1";
        _fileSystem.AddFile(firstFile, new MockFileData(""));
        
        const string secondFile = $"{subDir}/file2";
        _fileSystem.AddFile(secondFile, new MockFileData(""));

        var fileSyncer = new FileSyncer(DirPath, _transportMock.Object, _fileMonitorMock.Object, _fileSystem);
        fileSyncer.StartSyncing();
        await fileSyncer.ProcessEnqueuedEvent();
        await fileSyncer.ProcessEnqueuedEvent();
        await fileSyncer.ProcessEnqueuedEvent();

        _transportMock.Verify(x => x.Post(It.Is<AddFileSyncTask>(t => t.RelativePath == "sub_dir/file1")), Times.Once); 
        _transportMock.Verify(x => x.Post(It.Is<AddFileSyncTask>(t => t.RelativePath == "sub_dir/file2")), Times.Once); 
    }
    
    [Fact]
    public async Task Test_StartSyncing_InitialSyncPostsSyncEventForDirInsideSubDir()
    {
        _fileSystem.AddDirectory(DirPath);
        const string subDir = $"{DirPath}/sub_dir";
        _fileSystem.AddDirectory(subDir);
        
        const string nestedSubDir = $"{subDir}/another_dir";
        _fileSystem.AddDirectory(nestedSubDir);
       
        var fileSyncer = new FileSyncer(DirPath, _transportMock.Object, _fileMonitorMock.Object, _fileSystem);
        fileSyncer.StartSyncing();
        await fileSyncer.ProcessEnqueuedEvent();
        await fileSyncer.ProcessEnqueuedEvent();

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
    
    [Fact]
    public async Task Test_ProcessEnqueuedEvent_StartsProcessingOldestEvents()
    {
        var callOrder = new List<int>(2);
        _transportMock
            .Setup(x => x.Post(new AddDirSyncTask
            {
                RelativePath = $"extra_dir",
                Attributes = FileAttributes.Directory
            }))
            .Callback(() => callOrder.Add(0));
        _transportMock
            .Setup(x => x.Post(new AddDirSyncTask
            {
                RelativePath = $"new_dir_before_initial_sync",
                Attributes = FileAttributes.Directory
            }))
            .Callback(() => callOrder.Add(1));
        
        _fileSystem.AddDirectory(DirPath);
        _fileSystem.AddDirectory($"{DirPath}/extra_dir");

        var fileSyncer = new FileSyncer(DirPath, _transportMock.Object, _fileMonitorMock.Object, _fileSystem);
        fileSyncer.StartSyncing();
        
        //Trigger a file monitor event before initial sync of the second existing dir is finished
        const string newDirBeforeInitialSyncProcessed = $"{DirPath}/new_dir_before_initial_sync";
        _fileSystem.AddDirectory(newDirBeforeInitialSyncProcessed);
        _fileMonitorMock.Raise(c => c.NewFile += null, new NewFileEventArgs{RelativePath = newDirBeforeInitialSyncProcessed});    
        
        await fileSyncer.ProcessEnqueuedEvent();
        await fileSyncer.ProcessEnqueuedEvent();
        Assert.Equal(callOrder, new []{0, 1});
    }
}