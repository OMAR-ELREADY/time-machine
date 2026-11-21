namespace TimeVault.Core.Models;

// Represents one saved version of a file (like a Git commit for a single file).
public class FileVersion
{
    // Unique ID for this version (e.g., "ver-123").
    public string? Id { get; set; }

    // Which file this version belongs to (e.g., "C:\\Docs\\notes.txt").
    public string? FilePath { get; set; }

    // SHA256 hash of the file content. Used to find the stored copy on disk.
    public string? Hash { get; set; }

    // Size of the file in bytes when this version was saved.
    public long SizeBytes { get; set; }

    // When this version was created.
    public DateTime CreatedAt { get; set; }

    // The version number (1, 2, 3...). First version = 1.
    public int VersionNumber { get; set; }
}