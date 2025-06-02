using System.Globalization;
using System.IO.Abstractions;

namespace Client;

class Program
{
    static async Task Main(string[] args)
    {
        if (args.Length != 3)
        {  
            Console.WriteLine("Usage: <source dir> <server host> <server port>");
            return;
        }
      
        var sourceDirPath = args[0];
        if (!IsValidSourceDir(sourceDirPath))
        {
            Console.WriteLine($"Invalid source directory {sourceDirPath}");
            return;
        }
        
        var serverHost = args[1];
        if (Uri.CheckHostName(serverHost) == UriHostNameType.Unknown)
        {
            Console.WriteLine($"Invalid server hostname {serverHost}");
            return;
        }

        if (!int.TryParse(args[2], out var serverPort) || !IsInValidPortRange(serverPort))
        {
            Console.WriteLine($"Invalid port arg");
            return;
        }
        
        var fileSystem = new FileSystem();
        var monitor = new FileMonitor(sourceDirPath, new FileSystemWatcherWrapper(fileSystem));

        ITransport? transport = CreateTransport(serverHost, serverPort);
        if (transport == null)
        {
            return;
        }
        var syncer = new FileSyncer(sourceDirPath, transport, monitor, fileSystem);
        
        syncer.StartSyncing();
        while (await syncer.AwaitEnqueuedEvents())
        {
            await syncer.ProcessEnqueuedEvent();
        }
    }

    private static ITransport? CreateTransport(string serverHost, int serverPort)
    {
        try
        {
            return new Transport(serverHost, serverPort);
        }
        catch (Exception ex)
        {
            Console.WriteLine("Failed to connect to server" + ex.Message);
            return null;
        }
    }
    
    private static bool IsValidSourceDir(string sourceDirPath)
    {
        return !string.IsNullOrEmpty(sourceDirPath) && Directory.Exists(sourceDirPath);
    }
    
    private static bool IsInValidPortRange(int port) => port >= System.Net.IPEndPoint.MinPort && port <= System.Net.IPEndPoint.MaxPort;
}