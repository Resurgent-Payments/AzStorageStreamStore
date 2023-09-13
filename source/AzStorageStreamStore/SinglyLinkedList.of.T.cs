namespace AzStorageStreamStore;

using System.Collections;

public class SinglyLinkedList<T> : IEnumerable<T> where T : class {
    private ListItem<T> _headNode;

    public SinglyLinkedList() {

    }

    public SinglyLinkedList(IEnumerable<T> items) {
        foreach (var item in items) {
            Append(item);
        }
    }

    public T this[long index] {
        get {
            if ((Length < index) || index < 0) throw new ArgumentOutOfRangeException(nameof(index));

            long pos = -1;

            var enumerator = GetEnumerator();
            while (enumerator.MoveNext()) {
                pos += 1;

                if (pos == index) {
                    return enumerator.Current;
                }
            }

            throw new ArgumentOutOfRangeException($"Item does not exist at position '{index}'.");
        }
    }

    public long Length { get; private set; }

    public long Append(T item) {
        var node = new ListItem<T> {
            Item = item,
        };
        _headNode = _headNode ?? node;

        var lastNode = _headNode;
        while (lastNode.Next != null) {
            lastNode = lastNode.Next;
        }

        if (!lastNode.Equals(node)) {
            lastNode.Next = node;
        }

        Length += 1; // should be the # of items.

        return Length - 1; // should be the item's index.
    }

    public void Clear() {
        lock (this) {
            var nodes = new Stack<ListItem<T>>();

            var childNode = _headNode;

            while ((childNode = childNode.Next) != null) { nodes.Push(childNode); }

            ListItem<T> node;
            while ((node = nodes.Pop()) != null) {
                node.Next = null;
            }
            _headNode = null;
            Length = 0;
        }
    }
    public void Dispose() {
        // no-op
    }

    public IEnumerator<T> GetEnumerator() => new Enumerator(_headNode);
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    private class ListItem<T> {
        public ListItem<T> Next { get; set; }
        public T Item { get; set; }
    }

    private class Enumerator : IEnumerator<T> {
        private readonly ListItem<T> _headNode;
        private ListItem<T> _current;

        public Enumerator(ListItem<T> headNode) {
            _headNode = headNode;
        }

        public T Current => _current?.Item;

        object IEnumerator.Current => Current;

        public void Dispose() {
            // no-op
        }

        public bool MoveNext() {
            if (_current != null && _current.Next == null) {
                return false;
            }

            if (_headNode == null) {
                return false;
            }

            if (_current != null && _current.Next != null) {
                _current = _current.Next;
            } else {
                _current = _headNode;
            }


            return true;
        }

        public void Reset() {
            _current = null;
        }
    }
}
