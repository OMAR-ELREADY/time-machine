namespace TimeVault.Core.Models;

// Represents a file system change event (created, modified, deleted).
public class ChangeEvent
{
    // Unique ID for this event.
    public string? Id { get; set; }

    // Which watched folder the change happened in.
    public string? WatchedFolderId { get; set; }

    // Full path of the file that changed.
    public string? FilePath { get; set; }

    // Type of change: Created, Modified, or Deleted.
    public string? ChangeType { get; set; }

    // When the change occurred.
    public DateTime OccurredAt { get; set; }
}