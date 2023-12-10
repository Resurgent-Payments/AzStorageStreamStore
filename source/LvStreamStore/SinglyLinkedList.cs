namespace LvStreamStore {
    using System;
    using System.Collections;
    using System.Linq;

    internal class SinglyLinkedList<T> : IEnumerable<T>, IEnumerable {
        private readonly int _pageSize;

        private Node<T> _head;
        private Node<T> _tail;


        public SinglyLinkedList(int pageSize = 100) {
            _pageSize = pageSize;
            _head = _tail = new Node<T>();
        }

        public int Count { get; private set; } = 0;

        public T this[int index] {
            get {
                var numberOfPages = (index / _pageSize) + 1;
                var itemNumber = index % _pageSize;

                var page = _head;

                if (numberOfPages > 1) {
                    for (var i = 1; i < numberOfPages; i++) {
                        page = page.Next;
                    }
                }

                return page.Items.Skip(itemNumber).FirstOrDefault() ?? throw new IndexOutOfRangeException();
            }
            set => throw new NotSupportedException();
        }

        public void Append(T item) {
            Interlocked.MemoryBarrier(); //todo: research this one out.
            if (_tail.Items.Count == _pageSize) {
                // we need a new page.
                _tail = _tail.Next = new Node<T>();
            }
            _tail.Items.Add(item);
            Count += 1;
        }

        public IEnumerator<T> GetEnumerator() => new Enumerator<T>(_head, _pageSize);

        IEnumerator IEnumerable.GetEnumerator() => (IEnumerator)new Enumerator<T>(_head, _pageSize);


        private class Enumerator<T> : IEnumerator<T>, IEnumerator {
            Node<T> _head;
            Node<T> _page;
            int _pageSize = 0;
            int _itemIndex = -1;

            public T Current { get; private set; }

            object IEnumerator.Current => Current;


            public Enumerator(Node<T> head, int pageSize) {
                _page = _head = head;
                _pageSize = pageSize;
            }


            public void Dispose() {
                // no-op
            }

            public bool MoveNext() {
                _itemIndex++;

                if (_itemIndex == _pageSize) {
                    if (_page.Next is null) {
                        Current = default;
                        return false;
                    }

                    _itemIndex = 0;
                    _page = _page.Next;
                }

                Current = _page.Items.Count > _itemIndex
                    ? _page.Items[_itemIndex]
                    : default;

                return Current is not null;
            }

            public void Reset() {
                _itemIndex = -1;
                _page = _head;
            }
        }

        private class Node<T> {
            public List<T> Items { get; private set; } = new();
            public Node<T> Next { get; set; }
        }
    }
}
