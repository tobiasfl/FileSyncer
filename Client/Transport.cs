using System.Text;
using System.Text.Json;
using Common;

namespace Client;

public interface ITransport
{
    public Task Post(AddFileSyncTask task);
    public Task Post(AddDirSyncTask task);
}

public class Transport : ITransport
{
    private readonly HttpClient _httpClient;
    
    public Transport(Uri serverUri)
    {
        _httpClient = new HttpClient();
        //_httpClient.BaseAddress = new Uri("http://localhost:5214");
        _httpClient.BaseAddress = serverUri;
    }

    public async Task Post(AddFileSyncTask task)
    {
        string jsonContent = JsonSerializer.Serialize(task);
        Console.WriteLine(jsonContent);
        var httpContent = new StringContent(jsonContent, Encoding.UTF8, "application/json");
               
        Console.WriteLine($"sending post {_httpClient.BaseAddress}sync");
                
        var response = await _httpClient.PostAsync($"/sync", httpContent);
        if (response.IsSuccessStatusCode)
        {
            Console.WriteLine("File sync completed successfully");
        }
        else
        {
            string errorContent = await response.Content.ReadAsStringAsync();
            Console.WriteLine($"Error: {response.StatusCode} - {errorContent}");
        }           
    }
    
    public async Task Post(AddDirSyncTask task)
    {
        var jsonContent = JsonSerializer.Serialize(task);
        Console.WriteLine(jsonContent);
        var httpContent = new StringContent(jsonContent, Encoding.UTF8, "application/json");
               
        Console.WriteLine($"sending post {_httpClient.BaseAddress}sync");
                
        var response = await _httpClient.PostAsync($"/sync/dir", httpContent);
        if (response.IsSuccessStatusCode)
        {
            Console.WriteLine("File sync completed successfully");
        }
        else
        {
            string errorContent = await response.Content.ReadAsStringAsync();
            Console.WriteLine($"Error: {response.StatusCode} - {errorContent}");
        }           
    }
}