namespace TimeVault.Core.Models;

// Represents an action that can be undone (like restoring a file version).
public class UndoAction
{
    // Unique ID for this undo action.
    public string? Id { get; set; }

    // Type of action: "Restore" for now.
    public string? ActionType { get; set; }

    // File path that was affected.
    public string? FilePath { get; set; }

    // Which version was restored (for "Restore" actions).
    public string? RestoredVersionId { get; set; }

    // When the action was performed.
    public DateTime PerformedAt { get; set; }

    // Description for the user (e.g., "Restored notes.txt to version 3").
    public string? Description { get; set; }
}