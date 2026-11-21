using System;
using System.Collections.Generic;
using System.IO;

namespace TimeVault
{
    public class WatcherService
    {
        // Keep track of the active watcher for each folder by its ID
        private Dictionary<string, FileSystemWatcher> activeWatchers = new Dictionary<string, FileSystemWatcher>();

        // Our team's custom LinkedQueue to hold events until we process them
        private LinkedQueue<ChangeEvent> eventQueue;

        // Remember when each file last changed so we can ignore rapid duplicates
        private Dictionary<string, DateTime> lastChangedTimes = new Dictionary<string, DateTime>();

        public WatcherService(LinkedQueue<ChangeEvent> queue)
        {
            eventQueue = queue;
        }

        public void StartWatching(WatchedFolder folder)
        {
            // Make sure the directory actually exists on disk
            if (!Directory.Exists(folder.Path))
                return;

            // If we are already watching this folder, stop it first so we don't get duplicate watchers
            StopWatching(folder.Id);

            FileSystemWatcher watcher = new FileSystemWatcher(folder.Path);
            watcher.IncludeSubdirectories = true;

            // Hook up the events
            watcher.Created += (sender, e) => RecordChange(folder.Id, e.FullPath, "Created");
            watcher.Changed += (sender, e) => RecordChange(folder.Id, e.FullPath, "Modified");
            watcher.Deleted += (sender, e) => RecordChange(folder.Id, e.FullPath, "Deleted");
            watcher.Renamed += (sender, e) => RecordChange(folder.Id, e.FullPath, "Renamed");

            watcher.EnableRaisingEvents = true;
            activeWatchers[folder.Id] = watcher;
        }

        public void StopWatching(string folderId)
        {
            if (activeWatchers.ContainsKey(folderId))
            {
                FileSystemWatcher watcher = activeWatchers[folderId];
                watcher.EnableRaisingEvents = false;
                watcher.Dispose();
                activeWatchers.Remove(folderId);
            }
        }

        public void StopAll()
        {
            foreach (var watcher in activeWatchers.Values)
            {
                watcher.EnableRaisingEvents = false;
                watcher.Dispose();
            }
            activeWatchers.Clear();
        }

        private void RecordChange(string folderId, string filePath, string changeType)
        {
            lock (lastChangedTimes)
            {
                DateTime now = DateTime.Now;

                // When an app (like Notepad or Word) saves a file, Windows fires 2 or 3 'Changed' events
                // within a few milliseconds. We skip any event on the same file if it happened less than 500ms ago.
                if (lastChangedTimes.ContainsKey(filePath))
                {
                    double millisecondsSinceLast = (now - lastChangedTimes[filePath]).TotalMilliseconds;
                    if (millisecondsSinceLast < 500)
                    {
                        return;
                    }
                }

                lastChangedTimes[filePath] = now;

                // Create the change event object and push it into our queue
                ChangeEvent newEvent = new ChangeEvent
                {
                    FolderId = folderId,
                    FullPath = filePath,
                    ChangeType = changeType,
                    Timestamp = now
                };

                eventQueue.Enqueue(newEvent);
            }
        }
    }

    // --- Supporting Models & Data Structure needed by WatcherService ---

    public class ChangeEvent
    {
        public string FolderId { get; set; }
        public string FullPath { get; set; }
        public string ChangeType { get; set; } // Created, Modified, Deleted, Renamed
        public DateTime Timestamp { get; set; }
    }

    public class WatchedFolder
    {
        public string Id { get; set; }
        public string Path { get; set; }
    }

    public class LinkedQueue<T>
    {
        private class Node
        {
            public T Value;
            public Node Next;
            public Node(T val) { Value = val; }
        }

        private Node head;
        private Node tail;
        private int count;

        public int Count => count;
        public bool IsEmpty => count == 0;

        public void Enqueue(T item)
        {
            Node newNode = new Node(item);
            if (tail == null)
            {
                head = tail = newNode;
            }
            else
            {
                tail.Next = newNode;
                tail = newNode;
            }
            count++;
        }

        public bool TryDequeue(out T item)
        {
            if (head == null)
            {
                item = default;
                return false;
            }

            item = head.Value;
            head = head.Next;
            if (head == null) tail = null;
            count--;
            return true;
        }
    }
}
