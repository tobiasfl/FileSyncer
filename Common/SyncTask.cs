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
public record RenameSyncTask
{
    public required string RelativePathOld { get; init; }
    public required string RelativePathNew { get; init; }
}

public record DeleteSyncTask
{
    public required string RelativePath { get; init; }
}
