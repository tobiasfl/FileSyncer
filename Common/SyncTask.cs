using System.Text.Json.Serialization;

namespace Common;

public record AddFileSyncTask : SyncTask
{
    public required string RelativePath { get; init; }
    public required FileAttributes Attributes { get; init; }
    public required byte[] Content { get; init; }
}

public record AddDirSyncTask : SyncTask
{
    public required string RelativePath { get; init; }
    public required FileAttributes Attributes { get; init; }
}
public record RenameSyncTask : SyncTask
{
    public required string RelativePathOld { get; init; }
    public required string RelativePathNew { get; init; }
}

public record DeleteSyncTask : SyncTask
{
    public required string RelativePath { get; init; }
}

[JsonDerivedType(typeof(AddFileSyncTask), "add file")]
[JsonDerivedType(typeof(AddDirSyncTask), "add dir")]
[JsonDerivedType(typeof(RenameSyncTask), "rename")]
[JsonDerivedType(typeof(DeleteSyncTask), "delete")]
public abstract record SyncTask;
