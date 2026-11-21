namespace TimeVault.Core.Models;

// Represents a scheduled backup job for a watched folder.
public class BackupJob
{
    // Unique ID for this backup job (e.g., "job-789").
    public string? Id { get; set; }

    // Which watched folder this job backs up.
    public string? WatchedFolderId { get; set; }

    // Cron-like schedule: "daily", "weekly", or "0 2 * * *" for 2 AM daily.
    public string? Schedule { get; set; }

    // Next time this job should run.
    public DateTime NextRunAt { get; set; }

    // Whether the job is enabled.
    public bool IsEnabled { get; set; }

    // Last time the job ran successfully.
    public DateTime? LastRunAt { get; set; }

    // Last error message if the job failed.
    public string? LastError { get; set; }
}