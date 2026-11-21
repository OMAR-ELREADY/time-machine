namespace TimeVault.Core.Models;

// Represents a point-in-time snapshot of all watched folders (like a Git commit for the whole project).
public class Snapshot
{
    // Unique ID for this snapshot (e.g., "snap-456").
    public string? Id { get; set; }

    // User-friendly name or description (e.g., "Before refactoring").
    public string? Name { get; set; }

    // When this snapshot was taken.
    public DateTime CreatedAt { get; set; }

    // Which watched folder this snapshot belongs to.
    public string? WatchedFolderId { get; set; }

    // List of file versions included in this snapshot (file path -> version ID).
    // Simple string for now: "C:\\file.txt=ver-1;C:\\other.txt=ver-2"
    public string? FileVersions { get; set; }
}