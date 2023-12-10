namespace LvStreamStore;

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

internal class MemoryEventStreamReader : EventStreamReader {
    private readonly SinglyLinkedList<StreamItem> _head;

    public MemoryEventStreamReader(MemoryEventStream eventStream, MemoryEventStreamOptions options) {
        _head = eventStream._stream;
    }

    public override IAsyncEnumerator<StreamItem> GetAsyncEnumerator(CancellationToken token = default) =>
        new Enumerator(this, token);

    class Enumerator : IStreamEnumerator {
        private readonly CancellationToken? _token;
        private IEnumerator<StreamItem> _stream;

        public StreamItem Current => _stream.Current;


        public Enumerator(MemoryEventStreamReader reader, CancellationToken? token = default) {
            _stream = reader._head.GetEnumerator();
            _token = token;
        }

        public ValueTask DisposeAsync() => ValueTask.CompletedTask;

        public ValueTask<bool> MoveNextAsync() {
            if (_token?.IsCancellationRequested ?? false) { return ValueTask.FromResult(false); }
            return ValueTask.FromResult(_stream.MoveNext());
        }
    }
}