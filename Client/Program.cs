namespace Client;

class Program
{
    // <source dir> <server uri>
    static void Main(string[] args)
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

        var transport = new Transport(serverUri);
        var syncer = new FileSyncer("/tmp", transport);
        while (true)
        {
            syncer.ProcessEvents();
        }
    }
    
    private static bool IsValidSourceDir(string sourceDirPath)
    {
        return !string.IsNullOrEmpty(sourceDirPath) && Directory.Exists(sourceDirPath);
    }
}