using System;
using System.Collections.Generic;
using System.IO;
using TimeVault.Core.DataStructures;
using TimeVault.Core.Models;

namespace TimeVault.Core.Services.Watcher
{
    public class WatcherService
    {
        private Dictionary<string, FileSystemWatcher> watchers;

        private Dictionary<string, DateTime> lastEventTime;

        private LinkedQueue<ChangeEvent> eventQueue;

        private object lockObject = new object();

        private const int DebounceMilliseconds = 500;
        public WatcherService(LinkedQueue<ChangeEvent> queue)
        {
            eventQueue = queue;

            watchers = new Dictionary<string, FileSystemWatcher>();
            lastEventTime = new Dictionary<string, DateTime>();
        }

        public void StartWatching(string folderPath)
        {
            if (string.IsNullOrWhiteSpace(folderPath))
            {
                throw new ArgumentException("Folder path cannot be empty.");
            }

            if (!Directory.Exists(folderPath))
            {
                throw new DirectoryNotFoundException("Folder not found: " + folderPath);
            }

            if (watchers.ContainsKey(folderPath))
            {
                return;
            }

            FileSystemWatcher watcher = new FileSystemWatcher(folderPath);

            watcher.IncludeSubdirectories = true;

            watcher.NotifyFilter = NotifyFilters.FileName
                                 | NotifyFilters.LastWrite
                                 | NotifyFilters.Size;

            watcher.Created += OnCreated;
            watcher.Changed += OnChanged;
            watcher.Deleted += OnDeleted;
            watcher.Renamed += OnRenamed;

            watcher.EnableRaisingEvents = true;

            watchers.Add(folderPath, watcher);
        }

        public void StopWatching(string folderPath)
        {
            if (!watchers.ContainsKey(folderPath))
            {
                return;
            }

            FileSystemWatcher watcher = watchers[folderPath];

            watcher.EnableRaisingEvents = false;
            watcher.Dispose();

            watchers.Remove(folderPath);
        }

        public void StopAll()
        {
            foreach (FileSystemWatcher watcher in watchers.Values)
            {
                watcher.EnableRaisingEvents = false;
                watcher.Dispose();
            }

            watchers.Clear();
        }

        private void OnCreated(object sender, FileSystemEventArgs e)
        {
            if (Directory.Exists(e.FullPath))
            {
                return;
            }

            AddEvent(e.FullPath, ChangeType.Created, null);
        }

        private void OnChanged(object sender, FileSystemEventArgs e)
        {
            if (Directory.Exists(e.FullPath))
            {
                return;
            }

            AddEvent(e.FullPath, ChangeType.Modified, null);
        }

        private void OnDeleted(object sender, FileSystemEventArgs e)
        {
            AddEvent(e.FullPath, ChangeType.Deleted, null);
        }

        private void OnRenamed(object sender, RenamedEventArgs e)
        {
            if (Directory.Exists(e.FullPath))
            {
                return;
            }

            AddEvent(e.FullPath, ChangeType.Renamed, e.OldFullPath);
        }

        private void AddEvent(string filePath, ChangeType type, string oldPath)
        {
            lock (lockObject)
            {
                if (IsDuplicate(filePath, type))
                {
                    return;
                }

                ChangeEvent changeEvent = new ChangeEvent();

                // Fill it with the information.
                changeEvent.FilePath = filePath;
                changeEvent.OldPath = oldPath;
                changeEvent.Type = type;
                changeEvent.Time = DateTime.Now;

                eventQueue.Enqueue(changeEvent);
            }
        }

        private bool IsDuplicate(string filePath, ChangeType type)
        {
            string key = type + "|" + filePath;

            DateTime now = DateTime.Now;

            if (lastEventTime.ContainsKey(key))
            {
                TimeSpan difference = now - lastEventTime[key];

                if (difference.TotalMilliseconds < DebounceMilliseconds)
                {
                    return true;
                }
            }

            lastEventTime[key] = now;

            return false;
        }
    }
}