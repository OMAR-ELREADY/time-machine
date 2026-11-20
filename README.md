# TimeVault

A file versioning and backup application for Windows.

## Project Structure

```
TimeVault/
├── TimeVault.sln
├── TimeVault.Core/                 (Class library, all logic)
│   ├── DataStructures/
│   │   ├── MinHeap.cs
│   │   ├── LinkedQueue.cs
│   │   ├── LinkedStack.cs
│   │   ├── VersionList.cs
│   │   └── FolderNode.cs
│   ├── Services/
│   │   ├── Watcher/
│   │   │   └── WatcherService.cs
│   │   ├── VersionControl/
│   │   │   └── VersionService.cs
│   │   ├── Hashing/
│   │   │   └── HashService.cs
│   │   ├── Scheduler/
│   │   │   └── SchedulerService.cs
│   │   ├── Backup/
│   │   │   └── BackupService.cs
│   │   └── Compare/
│   │       └── CompareService.cs
│   └── Models/
│       ├── WatchedFolder.cs
│       ├── FileVersion.cs
│       ├── Snapshot.cs
│       ├── BackupJob.cs
│       ├── ChangeEvent.cs
│       └── UndoAction.cs
└── TimeVault.App/                  (WinForms, UI only)
    └── Forms/
        (Created in Visual Studio)
```

## Getting Started

1. Open `TimeVault.sln` in Visual Studio 2022+
2. Build the solution
3. Run `TimeVault.App`