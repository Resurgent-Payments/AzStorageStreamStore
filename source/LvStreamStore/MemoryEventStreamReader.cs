namespace LvStreamStore;

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

internal class MemoryEventStreamReader : EventStreamReader {
    private readonly IEnumerator<StreamMessage> _enumerator;

    public MemoryEventStreamReader(MemoryEventStream eventStream, MemoryEventStreamOptions options) {
        _enumerator = eventStream._stream.GetEnumerator();
    }

    public override IAsyncEnumerator<StreamMessage> GetAsyncEnumerator(CancellationToken token = default) =>
        new Enumerator(_enumerator, token);

    class Enumerator : IStreamEnumerator {
        private readonly CancellationToken? _token;
        private readonly IEnumerator<StreamMessage> _enumerator;

        public StreamMessage Current => _enumerator.Current;


        public Enumerator(IEnumerator<StreamMessage> enumerator, CancellationToken? token = default) {
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