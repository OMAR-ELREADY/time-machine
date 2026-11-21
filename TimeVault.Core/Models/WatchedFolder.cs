namespace TimeVault.Core.Models;

// Represents a folder that TimeVault watches for changes.
public class WatchedFolder
{
    // Unique identifier for this watched folder (e.g., "folder-1").
    public string? Id { get; set; }

    // Full path to the folder on disk (e.g., "C:\\Users\\Omar\\Documents").
    public string? Path { get; set; }

    // Whether to watch subfolders inside this folder.
    public bool IncludeSubdirectories { get; set; }

    // File extensions to watch (e.g., ".txt,.cs"). Empty = all files.
    public string? FileFilters { get; set; }

    // When this folder was added to TimeVault.
    public DateTime AddedAt { get; set; }
}