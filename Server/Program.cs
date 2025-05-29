using Common;

var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

string destinationDirPath = "server_test";

//TODO: somehow change COntent root path:
if (!Directory.Exists(destinationDirPath))
{
    Directory.CreateDirectory(destinationDirPath);
    Console.WriteLine("Created directory: " + destinationDirPath);
}

app.MapGet("/", () => "Hello World!");

app.MapPost("/sync", async (AddFileSyncTask syncTask) =>
{
    Console.WriteLine("Received POST request");
    try
    {
        string fullPath = destinationDirPath + '/' + syncTask.RelativePath;
        await File.WriteAllBytesAsync(fullPath, syncTask.Content);
        File.SetAttributes(fullPath, syncTask.Attributes);
    }
    catch (FileNotFoundException ex)
    {
        Console.WriteLine(ex.Message);
    }
    catch (Exception ex)
    {
        return Results.Problem(ex.Message);    
    }

    return Results.Ok();
});

app.MapPost("/sync/dir", async (AddDirSyncTask syncTask) =>
{
    Console.WriteLine("Received POST request dir sync");
    try
    {
        var fullPath = destinationDirPath + '/' + syncTask.RelativePath;
        Directory.CreateDirectory(fullPath);
        File.SetAttributes(fullPath, syncTask.Attributes);
    }
    catch (FileNotFoundException ex)
    {
        Console.WriteLine(ex.Message);
    }
    catch (Exception ex)
    {
        return Results.Problem(ex.Message);    
    }

    return Results.Ok();
});

app.Run();