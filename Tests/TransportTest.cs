using System.IO.Abstractions;
using System.Net;
using System.Net.Sockets;
using System.Text.Json;
using Client;
using Common;
using Moq;

namespace Tests;

public class TransportTest
{
    private readonly JsonSerializerOptions _expectedJsonOptions = new JsonSerializerOptions
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };
    
    [Fact]
    public void Test_Constructor_ThrowsSocketExceptionIfServerNotListeningOnPort()
    {
        Assert.Throws<SocketException>(() => new Transport("localhost", 0));
    }
    
    [Fact]
    public void Test_Constructor_SuccessFullyConstructsIfServerListeningOnPort()
    {
        TcpListener fakeServer = new TcpListener(IPAddress.Loopback, 0);
        fakeServer.Start();
        int serverPort = ((IPEndPoint)fakeServer.LocalEndpoint).Port;
        var transport = new Transport("localhost", serverPort); 
    }

    [Fact]
    public async Task Test_Post_SerializesSentSyncTasksAsJson()
    {
        TcpListener fakeServer = new TcpListener(IPAddress.Loopback, 0);
        fakeServer.Start();
        int serverPort = ((IPEndPoint)fakeServer.LocalEndpoint).Port;
        
        var transport = new Transport("localhost", serverPort);
        var clientConnection = await fakeServer.AcceptTcpClientAsync();

        SyncTask sentTask = new AddDirSyncTask{ RelativePath = "/test_dir", Attributes = FileAttributes.Directory | FileAttributes.Hidden};
        await transport.Post(sentTask);

        var taskAsJson = JsonSerializer.Serialize(sentTask, _expectedJsonOptions);
        var fullDataLength = System.Text.Encoding.UTF8.GetBytes(taskAsJson).Length;
        
        await using var stream = clientConnection.GetStream();
        var buffer = new byte[fullDataLength + sizeof(int)];
        await stream.ReadExactlyAsync(buffer, offset: 0, buffer.Length);

        var receivedJson = System.Text.Encoding.UTF8.GetString(buffer.AsEnumerable().Skip(sizeof(int)).ToArray());
        var task = JsonSerializer.Deserialize<SyncTask>(receivedJson, _expectedJsonOptions);
        
        Assert.Equal(task, sentTask); 
    }
    
    [Fact]
    public async Task Test_Post_PrependsLengthOfSerializedData()
    {
        TcpListener fakeServer = new TcpListener(IPAddress.Loopback, 0);
        fakeServer.Start();
        int serverPort = ((IPEndPoint)fakeServer.LocalEndpoint).Port;
        
        var transport = new Transport("localhost", serverPort);
        var clientConnection = await fakeServer.AcceptTcpClientAsync();

        SyncTask sentTask = new RenameSyncTask { RelativePathNew = "to", RelativePathOld = "from" };
        await transport.Post(sentTask);

        var taskAsJson = JsonSerializer.Serialize(sentTask, _expectedJsonOptions);
        var expectedLength = System.Text.Encoding.UTF8.GetBytes(taskAsJson).Length;
        
        await using var stream = clientConnection.GetStream();
        var buffer = new byte[sizeof(int)];
        await stream.ReadExactlyAsync(buffer, 0, sizeof(int));
        
        Assert.Equal(expectedLength, BitConverter.ToInt32(buffer)); 
    }
}
