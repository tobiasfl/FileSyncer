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

        if (!int.TryParse(args[2], out var serverPort))
        {
            Console.WriteLine($"Invalid port arg");
            return;
        }
        
        var fileSystem = new FileSystem();
        var monitor = new FileMonitor(sourceDirPath, new FileSystemWatcherWrapper(fileSystem));
        var transport = new Transport(serverHost, serverPort);
        var syncer = new FileSyncer(sourceDirPath, transport, monitor, fileSystem);
        
        await syncer.ProcessEvents();
    }
    
    private static bool IsValidSourceDir(string sourceDirPath)
    {
        return !string.IsNullOrEmpty(sourceDirPath) && Directory.Exists(sourceDirPath);
    }
}