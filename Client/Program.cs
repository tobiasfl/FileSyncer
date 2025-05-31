using System.IO.Abstractions;

namespace Client;

class Program
{
    static async Task Main(string[] args)
    {
        if (args.Length != 2)
        {  
            Console.WriteLine("Usage: <source dir path> <server http uri>");
            return;
        }
      
        var sourceDirPath = args[0];
        if (!IsValidSourceDir(sourceDirPath))
        {
            Console.WriteLine($"Invalid source directory {sourceDirPath}");
            return;
        }
        
        if (!Uri.TryCreate(args[1], UriKind.Absolute, out var serverUri))
        {
            Console.WriteLine($"Invalid server uri {serverUri}");
            return;
        }

        var fileSystem = new FileSystem();
        var monitor = new FileMonitor(sourceDirPath, new FileSystemWatcherWrapper(fileSystem));
        var transport = new Transport(serverUri);
        var syncer = new FileSyncer(sourceDirPath, transport, monitor, fileSystem);
        
        await syncer.ProcessEvents();
    }
    
    private static bool IsValidSourceDir(string sourceDirPath)
    {
        return !string.IsNullOrEmpty(sourceDirPath) && Directory.Exists(sourceDirPath);
    }
}