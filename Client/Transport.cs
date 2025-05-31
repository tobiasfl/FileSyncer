using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using Common;

namespace Client;

public interface ITransport
{
    public Task Post(SyncTask task);
}

public class Transport : ITransport
{
    private readonly TcpClient _client;
    private readonly NetworkStream _stream;
    private readonly JsonSerializerOptions _options; 
    
    public Transport(Uri serverUri)
    {
        var host = serverUri.Host;
        var port = serverUri.Port;
        _client = new TcpClient(host, port);
        _stream = _client.GetStream();
        _options = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };
    }

    private async Task SendTask(SyncTask task)
    {
        var json = JsonSerializer.Serialize(task, _options);
        var data = System.Text.Encoding.UTF8.GetBytes(json);
        
        var dataLengthBytes = BitConverter.GetBytes(data.Length);
        try
        {
            await _stream.WriteAsync(dataLengthBytes);
            await _stream.WriteAsync(data);
            await _stream.FlushAsync();
        }
        catch (IOException e)
        {
            Console.WriteLine(e.Message);
        }
        finally
        {
            Console.WriteLine("Closing connection");
            Dispose();
        }
    }
    private void Dispose()
    {
        _stream?.Dispose();
        _client?.Dispose();
    } 
    
    public async Task Post(SyncTask task)
    {
        await SendTask(task);
    }
}