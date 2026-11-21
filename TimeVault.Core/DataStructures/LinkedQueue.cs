namespace TimeVault.Core.DataStructures;

using TimeVault.Core.Models;

// Queue for pending file change events. First-in, first-out (FIFO).
// Enqueue = O(1), Dequeue = O(1). Uses a linked list internally.
public class LinkedQueue
{
    // Node class: holds one ChangeEvent and a pointer to the next node.
    private class Node
    {
        public ChangeEvent Value;
        public Node? Next;

        public Node(ChangeEvent value)
        {
            Value = value;
            Next = null;
        }
    }

    private Node? _head;  // First node in the queue
    private Node? _tail;  // Last node in the queue
    private int _count;  // Number of items in the queue

    // Number of events in the queue.
    public int Count => _count;

    // True if queue has no events.
    public bool IsEmpty => _count == 0;

    // Add an event to the back of the queue. O(1).
    public void Enqueue(ChangeEvent item)
    {
        var newNode = new Node(item);
        if (_tail == null)
        {
            // Queue was empty: new node is both head and tail.
            _head = _tail = newNode;
        }
        else
        {
            // Queue not empty: attach after current tail, then move tail.
            _tail!.Next = newNode;
            _tail = newNode;
        }
        _count++;
    }

    // Remove and return the front event. Returns false if queue empty. O(1).
    public bool TryDequeue(out ChangeEvent? item)
    {
        if (_head == null)
        {
            item = null;
            return false;
        }

        item = _head!.Value;
        _head = _head.Next;

        // If queue becomes empty, clear tail too.
        if (_head == null)
        {
            _tail = null;
        }

        _count--;
        return true;
    }
}