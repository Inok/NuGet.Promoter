using System.Diagnostics.CodeAnalysis;

namespace Promote.NuGet.Commands.Core;

internal class DistinctQueue<T>
{
    private readonly Queue<T> _queue;
    private readonly HashSet<T> _enqueuedItems;

    public bool HasItems => _queue.Count > 0;

    public DistinctQueue()
    {
        _queue = new Queue<T>();
        _enqueuedItems = new HashSet<T>();
    }

    public DistinctQueue(IEnumerable<T> items) : this()
    {
        if (items == null) throw new ArgumentNullException(nameof(items));

        foreach (var item in items)
        {
            Enqueue(item);
        }
    }

    public bool Enqueue(T item)
    {
        if (!_enqueuedItems.Add(item))
        {
            return false;
        }

        _queue.Enqueue(item);
        return true;
    }

    public bool TryDequeue([MaybeNullWhen(false)] out T result)
    {
        return _queue.TryDequeue(out result);
    }
}