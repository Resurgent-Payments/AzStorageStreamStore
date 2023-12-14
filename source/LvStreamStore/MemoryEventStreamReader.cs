namespace LvStreamStore;

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

internal class MemoryEventStreamReader : EventStreamReader {
    private readonly IEnumerator<StreamItem> _enumerator;

    public MemoryEventStreamReader(MemoryEventStream eventStream, MemoryEventStreamOptions options) {
        _enumerator = eventStream._stream.GetEnumerator();
    }

    public override IAsyncEnumerator<StreamItem> GetAsyncEnumerator(CancellationToken token = default) =>
        new Enumerator(_enumerator, token);

    class Enumerator : IStreamEnumerator {
        private readonly CancellationToken? _token;
        private readonly IEnumerator<StreamItem> _enumerator;

        public StreamItem Current => _enumerator.Current;


        public Enumerator(IEnumerator<StreamItem> enumerator, CancellationToken? token = default) {
            _enumerator = enumerator;
            _token = token;
        }

        public ValueTask DisposeAsync() => ValueTask.CompletedTask;

        public ValueTask<bool> MoveNextAsync() {
            if (_token?.IsCancellationRequested ?? false) { return ValueTask.FromResult(false); }
            return ValueTask.FromResult(_enumerator.MoveNext());
        }
    }
}