using Common;

var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

//TODO: need to be arg
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
    Console.WriteLine("Received add file sync request");
    try
    {
        var fullPath = destinationDirPath + '/' + syncTask.RelativePath;
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

app.MapPost("/sync/dir", (AddDirSyncTask syncTask) =>
{
    Console.WriteLine("Received add dir request");
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


app.MapPost("/sync/rename", (RenameSyncTask syncTask) =>
{
    Console.WriteLine("Received POST request rename sync");
    try
    {
        var fullPathSrc = destinationDirPath + '/' + syncTask.RelativePathOld;
        var fullPathDst = destinationDirPath + '/' + syncTask.RelativePathNew;
        File.Move(fullPathSrc, fullPathDst);
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


app.MapPost("/sync/delete", (DeleteSyncTask syncTask) =>
{
    Console.WriteLine("Received POST request delete sync");
    try
    {
        var fullPath = destinationDirPath + '/' + syncTask.RelativePath;
        var attributes = File.GetAttributes(fullPath);
        if (attributes.HasFlag(FileAttributes.Directory))
        {
            Directory.Delete(fullPath);
        }
        else
        {
            File.Delete(fullPath);
        }
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