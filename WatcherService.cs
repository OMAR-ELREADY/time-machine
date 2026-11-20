using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Threading;
// --- البيانات والنماذج المبدئية (ChangeType, ChangeEvent) ---

// --- هيكل البيانات: الطابور المشترك (LinkedQueue<T>) ---

// --- العملية 1: تهيئة وتشغيل المراقب لكل مجلد (StartWatching) ---

// --- العملية 2: استقبال الحدث الخام (OnFileCreated, OnFileChanged, OnFileDeleted, HandleRawEvent) ---

// --- العملية 3: تهدئة الأحداث السريعة (Debounce / ForwardToDebounce) ---

// --- العملية 4: إضافة الحدث إلى الطابور المشترك (EnqueueDebouncedEvent) ---

// --- العملية 5: إيقاف المراقبة وتنظيف الموارد (StopWatching, StopAll, Dispose) ---

namespace TimeVault.DataStructures
{
    public class LinkedQueue<T>
    {
        private class Node
        {
            public T Value;
            public Node Next;
            public Node(T value) => Value = value;
        }

        private Node _head;
        private Node _tail;
        private readonly object _lock = new object();

        public int Count { get; private set; }

        public bool IsEmpty
        {
            get
            {
                lock (_lock)
                {
                    return Count == 0;
                }
            }
        }

        public void Enqueue(T item)
        {
            lock (_lock)
            {
                var newNode = new Node(item);
                if (_tail == null)
                {
                    _head = _tail = newNode;
                }
                else
                {
                    _tail.Next = newNode;
                    _tail = newNode;
                }
                Count++;
            }
        }

        public bool TryDequeue(out T item)
        {
            lock (_lock)
            {
                if (_head == null)
                {
                    item = default;
                    return false;
                }

                item = _head.Value;
                _head = _head.Next;
                if (_head == null)
                {
                    _tail = null;
                }
                Count--;
                return true;
            }
        }
    }
}

namespace TimeVault.Core
{
    using TimeVault.DataStructures;

    public enum ChangeType
    {
        Created,
        Modified,
        Deleted
    }

    public class ChangeEvent
    {
        public string FolderId { get; set; }
        public string FilePath { get; set; }
        public ChangeType ChangeType { get; set; }
        public DateTime OccurredAt { get; set; }
    }

    public class WatcherService : IDisposable
    {
        private readonly Dictionary<string, FileSystemWatcher> _watchers = new Dictionary<string, FileSystemWatcher>();
        private readonly Dictionary<FileSystemWatcher, string> _watcherToFolderId = new Dictionary<FileSystemWatcher, string>();
        private readonly Dictionary<string, string> _folderIdToPath = new Dictionary<string, string>();

        // --- العملية الثالثة: قاموس مؤقتات التهدئة لكل مسار ملف، وفترة التهدئة بالمللي ثانية ---
        private readonly ConcurrentDictionary<string, Timer> _debounceTimers = new ConcurrentDictionary<string, Timer>();
        private readonly object _debounceLock = new object();
        private readonly int _debounceDelayMs;

        // --- العملية الرابعة: الطابور المشترك لاستقبال الأحداث بعد تهدئتها ---
        private readonly LinkedQueue<ChangeEvent> _sharedEventQueue;

        private bool _disposed;

        public WatcherService(LinkedQueue<ChangeEvent> sharedEventQueue = null, int debounceDelayMs = 400)
        {
            _sharedEventQueue = sharedEventQueue ?? new LinkedQueue<ChangeEvent>();
            _debounceDelayMs = debounceDelayMs;
        }

        public LinkedQueue<ChangeEvent> EventQueue => _sharedEventQueue;

        // --- العملية الأولى: تهيئة وتشغيل المراقب لكل مجلد ---
        public void StartWatching(string folderId, string folderPath, bool includeSubdirectories = true)
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(WatcherService));

            if (string.IsNullOrWhiteSpace(folderId) || string.IsNullOrWhiteSpace(folderPath))
                throw new ArgumentException("معرف المجلد ومساره مطلوبان.");

            if (!Directory.Exists(folderPath))
                throw new DirectoryNotFoundException($"المجلد غير موجود: {folderPath}");

            if (_watchers.ContainsKey(folderId))
            {
                StopWatching(folderId);
            }

            var watcher = new FileSystemWatcher(folderPath)
            {
                IncludeSubdirectories = includeSubdirectories,
                NotifyFilter = NotifyFilters.FileName | NotifyFilters.LastWrite | NotifyFilters.Size
            };

            watcher.Created += OnFileCreated;
            watcher.Changed += OnFileChanged;
            watcher.Deleted += OnFileDeleted;

            watcher.EnableRaisingEvents = true;

            _watchers[folderId] = watcher;
            _watcherToFolderId[watcher] = folderId;
            _folderIdToPath[folderId] = folderPath;
        }

        // --- العملية الخامسة: إيقاف المراقبة وتنظيف الموارد ---
        public void StopWatching(string folderId)
        {
            if (string.IsNullOrWhiteSpace(folderId))
                return;

            if (_watchers.TryGetValue(folderId, out var watcher))
            {
                watcher.EnableRaisingEvents = false;
                watcher.Created -= OnFileCreated;
                watcher.Changed -= OnFileChanged;
                watcher.Deleted -= OnFileDeleted;
                _watcherToFolderId.Remove(watcher);
                watcher.Dispose();
                _watchers.Remove(folderId);
            }

            // إلغاء وتنظيف مؤقتات التهدئة المرتبطة بمسار هذا المجلد إن وُجدت
            if (_folderIdToPath.TryGetValue(folderId, out string folderPath))
            {
                CancelTimersUnderPath(folderPath);
                _folderIdToPath.Remove(folderId);
            }
        }

        public void StopAll()
        {
            var folderIds = new List<string>(_watchers.Keys);
            foreach (var id in folderIds)
            {
                StopWatching(id);
            }

            CancelAllTimers();
        }

        private void CancelTimersUnderPath(string folderPath)
        {
            if (string.IsNullOrWhiteSpace(folderPath))
                return;

            string normalizedFolder = Path.GetFullPath(folderPath).ToLowerInvariant();
            if (!normalizedFolder.EndsWith(Path.DirectorySeparatorChar.ToString()))
            {
                normalizedFolder += Path.DirectorySeparatorChar;
            }

            lock (_debounceLock)
            {
                foreach (var key in new List<string>(_debounceTimers.Keys))
                {
                    if (key.StartsWith(normalizedFolder) && _debounceTimers.TryRemove(key, out var timer))
                    {
                        timer.Dispose();
                    }
                }
            }
        }

        private void CancelAllTimers()
        {
            lock (_debounceLock)
            {
                foreach (var key in new List<string>(_debounceTimers.Keys))
                {
                    if (_debounceTimers.TryRemove(key, out var timer))
                    {
                        timer.Dispose();
                    }
                }
            }
        }

        public void Dispose()
        {
            if (_disposed)
                return;

            StopAll();
            _disposed = true;
            GC.SuppressFinalize(this);
        }

        // --- العملية الثانية: استقبال الحدث الخام ---
        private void OnFileCreated(object sender, FileSystemEventArgs e)
        {
            if (_disposed) return;
            HandleRawEvent(sender, e, ChangeType.Created);
        }

        private void OnFileChanged(object sender, FileSystemEventArgs e)
        {
            if (_disposed) return;
            HandleRawEvent(sender, e, ChangeType.Modified);
        }

        private void OnFileDeleted(object sender, FileSystemEventArgs e)
        {
            if (_disposed) return;
            HandleRawEvent(sender, e, ChangeType.Deleted);
        }

        private void HandleRawEvent(object sender, FileSystemEventArgs e, ChangeType changeType)
        {
            if (sender is not FileSystemWatcher watcher)
                return;

            if (!_watcherToFolderId.TryGetValue(watcher, out string folderId))
                return;

            if (changeType != ChangeType.Deleted && Directory.Exists(e.FullPath))
                return;

            var rawEvent = new ChangeEvent
            {
                FolderId = folderId,
                FilePath = e.FullPath,
                ChangeType = changeType,
                OccurredAt = DateTime.Now
            };

            ForwardToDebounce(rawEvent);
        }

        // --- العملية الثالثة: تهدئة الأحداث السريعة (Debounce) ---
        private void ForwardToDebounce(ChangeEvent rawEvent)
        {
            if (_disposed || string.IsNullOrWhiteSpace(rawEvent.FilePath))
                return;

            string key = rawEvent.FilePath.ToLowerInvariant();

            lock (_debounceLock)
            {
                if (_debounceTimers.TryRemove(key, out var existingTimer))
                {
                    existingTimer.Dispose();
                }

                var timer = new Timer(_ =>
                {
                    if (_disposed)
                        return;

                    if (_debounceTimers.TryRemove(key, out var completedTimer))
                    {
                        completedTimer.Dispose();
                    }

                    EnqueueDebouncedEvent(rawEvent);
                }, null, _debounceDelayMs, Timeout.Infinite);

                _debounceTimers[key] = timer;
            }
        }

        // --- العملية الرابعة: إضافة الحدث إلى الطابور المشترك ---
        private void EnqueueDebouncedEvent(ChangeEvent debouncedEvent)
        {
            if (_disposed || debouncedEvent == null)
                return;

            _sharedEventQueue.Enqueue(debouncedEvent);
        }
    }
}