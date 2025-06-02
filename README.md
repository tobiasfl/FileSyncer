
## Running client and server

Platforms: Only tested on Linux, likely works on Mac and Linux. I used '/' to separate paths so probably not windows.

The client expects that the server is already running.

Server:

dotnet run --project SyncServer/SyncServer.csproj -- <destination dir> <TCP port>

e.g. $dotnet run --project SyncServer/SyncServer.csproj -- /tmp/server_test/ 5214

Client:

dotnet run --project Client/Client.csproj -- <source dir> <server hostname> <server port>

e.g. $dotnet run --project Client/Client.csproj -- /home/tobias/client_proj_test localhost 5214

### Some known shortcomings

- Read/Write/Execute changes are not synced
- Because of how noisy FileSystemWatcher with the events when changes happen there may be a lot of redundant syncs happening since the current implementation reacts to every event.
    - This could be mitigated by e.g. waiting a some milliseconds before triggering a sync of a changed file or looking into filtering the events coming from the FileSystemWatcher better.
- When a file changes, the whole file is re-synced even though it might not be neccessary e.g. if just some metadata changed.
- The TCP client assumes that no files are larger than 2^32 bytes (4.3 ish gigabytes)
- Some files are missing unit tests, and quite a lot of functionality and behavior is not covered by any tests.

### Possible improvements and next steps

- Proper logging with a framework
- More error and exception handling in various places (and tests for the different exceptions that may occur)
- End to end tests with both server and client running (Could e.g. mock out the file system on both ends)
- Use built-in Dependency Injection framework instead of passing in objects manually.
- Use TLS to make sure the data is encrypted!
- Unit tests with very large files

