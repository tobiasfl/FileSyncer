namespace SyncServer;

class Program
{
    static async Task Main(string[] args)
    {
        if (args.Length != 2)
        {  
            Console.WriteLine("Usage: <destination dir> <TCP port>");
            return;
        }
      
        var dstDir = args[0];
        if (!IsValidDestinationDir(dstDir))
        {
            Console.WriteLine($"Invalid destination directory: {dstDir}");
            return;
        }
      
        
        if (!int.TryParse(args[1], out var port))
        {
            Console.WriteLine($"Invalid port arg");
            return;
        }
        
        Console.WriteLine($"Running sync server, listening on port {port}, syncing to {dstDir}");
        var taskHandler = new TaskHandler(dstDir);
        var receiver = new TaskReceiver(port, taskHandler);
        await receiver.StartListening();
    }

    private static bool IsValidDestinationDir(string dstDirPath) => !string.IsNullOrEmpty(dstDirPath);
}
