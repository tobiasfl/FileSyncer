using System.Text;
using System.Text.Json;
using Common;

namespace Client;

public interface ITransport
{
    public Task Post(AddFileSyncTask task);
    public Task Post(AddDirSyncTask task);
    public Task Post(RenameSyncTask task);
    public Task Post(DeleteSyncTask task);
}

public class Transport : ITransport
{
    private readonly HttpClient _httpClient;
    
    public Transport(Uri serverUri)
    {
        _httpClient = new HttpClient();
        _httpClient.BaseAddress = serverUri;
    }

    public async Task Post(AddFileSyncTask task)
    {
        await SendRequest(task, $"/sync");
    }
    
    public async Task Post(AddDirSyncTask task)
    {
        await SendRequest(task, $"/sync/dir");
    }
    
    public async Task Post(RenameSyncTask task)
    {
        await SendRequest(task, "/sync/rename");
    }

    public async Task Post(DeleteSyncTask task)
    {
        await SendRequest(task, "/sync/delete");
    }

    private async Task SendRequest<TSyncTask>(TSyncTask task, string requestUri)
    {
        var jsonContent = JsonSerializer.Serialize(task);
        Console.WriteLine(jsonContent);
        
        var httpContent = new StringContent(jsonContent, Encoding.UTF8, "application/json");
               
        Console.WriteLine($"sending post {_httpClient.BaseAddress}{requestUri}");
                
        var response = await _httpClient.PostAsync(requestUri, httpContent);
        if (response.IsSuccessStatusCode)
        {
            Console.WriteLine("Sync task completed successfully");
        }
        else
        {
            string errorContent = await response.Content.ReadAsStringAsync();
            Console.WriteLine($"Error: {response.StatusCode} - {errorContent}");
        }           
    }
}