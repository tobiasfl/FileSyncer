
## Running client and server

The client expects that the server is already running.

Server:

dotnet run --project SyncServer/SyncServer.csproj -- <destination dir> <TCP port>

Client:

dotnet run --project Client/Client.csproj -- <source dir> <server hostname> <server port>
