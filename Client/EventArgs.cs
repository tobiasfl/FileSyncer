namespace Client;

public class FileRenamedEventArgs : EventArgs
{
    public required string FromRelativePath { get; init; }
    public required string ToRelativePath { get; init; } 
}

public class FileChangedEventArgs : EventArgs
{
    public required string RelativePath { get; init; }
}

public class NewFileEventArgs : EventArgs
{
    public required string RelativePath { get; init; } 
}

public class FileDeletedEventArgs : EventArgs
{
    public required string RelativePath { get; init; } 
}
