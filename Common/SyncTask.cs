namespace Common;

public record AddFileSyncTask
{
    public required string RelativePath { get; init; }
    public required FileAttributes Attributes { get; init; }
    public required byte[] Content { get; init; }
}

public record AddDirSyncTask
{
    public required string RelativePath { get; init; }
    public required FileAttributes Attributes { get; init; }
}
