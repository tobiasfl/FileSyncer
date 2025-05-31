namespace SyncServer;

class Program
{
    private const int DefaultTcpPort = 5214;

    static async Task Main(string[] args)
    {
        if (args.Length != 1)
        {  
            Console.WriteLine("Usage: <destination dir path>");
            return;
        }
      
        var dstDir = args[0];
        if (!IsValidDestinationDir(dstDir))
        {
            Console.WriteLine($"Invalid destination directory: {dstDir}");
            return;
        }
        
        Console.WriteLine($"Running sync server, listening on port {DefaultTcpPort}, syncing to {dstDir}");
        var taskHandler = new TaskHandler(dstDir);
        var receiver = new TaskReceiver(DefaultTcpPort, taskHandler);
        await receiver.StartListening();
    }

    private static bool IsValidDestinationDir(string dstDirPath) => !string.IsNullOrEmpty(dstDirPath);
}
