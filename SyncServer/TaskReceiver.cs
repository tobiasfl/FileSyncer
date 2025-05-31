using System.Net;
using System.Net.Sockets;
using System.Text.Json;
using Common;

namespace SyncServer;
public class TaskReceiver
{
    private readonly TcpListener _listener;
    private readonly JsonSerializerOptions _options;
    private readonly TaskHandler _handler;

    public TaskReceiver(int port, TaskHandler handler)
    {
        _handler = handler;
        _listener = new TcpListener(IPAddress.Any, port);
        _options = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };
    }

    public async Task StartListening()
    {
        _listener.Start();
        Console.WriteLine("Server listening...");

        while (true)
        {
            var client = await _listener.AcceptTcpClientAsync();
            await Task.Run(() => HandleClient(client));
        }
    }

    private async Task HandleClient(TcpClient client)
    {
        await using var stream = client.GetStream();

        try
        {
            while (client.Connected)
            {
                const int lengthFieldBytes = 4;
                var lengthBytes = new byte[lengthFieldBytes];
                var bytesRead = await ReadExactly(stream, lengthBytes, lengthFieldBytes);
                if (bytesRead == 0) break;

                var length = BitConverter.ToInt32(lengthBytes);
                
                var taskData = new byte[length];
                await ReadExactly(stream, taskData, length);
                
                var json = System.Text.Encoding.UTF8.GetString(taskData);
                try
                {
                    var task = JsonSerializer.Deserialize<SyncTask>(json, _options);
                    if (task != null)
                    {
                        await _handler.HandleTask(task);
                    }
                }
                catch (JsonException e)
                {
                    Console.WriteLine("received invalid task:" + e.Message); 
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error handling client: {ex.Message}");
        }
        finally
        {
            client.Close();
        }
    }
    
    private async Task<int> ReadExactly(NetworkStream stream, byte[] buffer, int count)
    {
        int totalRead = 0;
        while (totalRead < count)
        {
            var read = await stream.ReadAsync(buffer, totalRead, count - totalRead);
            bool isConnectionClosed = read == 0;
            if (isConnectionClosed) return totalRead;
            totalRead += read;
        }
        return totalRead;
    }
}
