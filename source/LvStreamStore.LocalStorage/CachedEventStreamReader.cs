namespace LvStreamStore.LocalStorage {
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;

    using LvStreamStore.Serialization;

    internal class CachedEventStreamReader : EventStreamReader {
        protected static long LastBytePosition { get; set; }
        protected static SinglyLinkedList<StreamMessage> Cache { get; } = new();

        protected string DataFileName { get; private set; }
        protected IEventSerializer Serializer { get; private set; }
        protected LocalStorageEventStreamOptions Options { get; private set; }
        public CachedEventStreamReader(string dataFileName, IEventSerializer eventSerializer, LocalStorageEventStreamOptions options) : base() {
            DataFileName = dataFileName;
            Serializer = eventSerializer;
            Options = options;
        }

        public override IAsyncEnumerator<StreamMessage> GetAsyncEnumerator(CancellationToken token = default)
            => new Enumerator(this);

        class Enumerator : IStreamEnumerator {
            IEnumerator<StreamMessage> _messages;
            CachedEventStreamReader _reader;

            public StreamMessage Current { get; private set; }

            public Enumerator(CachedEventStreamReader reader) {
                _messages = Cache.GetEnumerator();
                _reader = reader;
            }

            public ValueTask<bool> MoveNextAsync() {
                if (_messages.MoveNext()) {
                    Current = _messages.Current;
                    return ValueTask.FromResult(true);
                }

                lock (Cache) {
                    int readOffset;
                    byte[] headerBuffer = new byte[EventStream.LengthOfEventHeader];
                    byte[] readBuffer = new byte[4096];

                    using (var fStream = new FileStream(_reader.DataFileName, new FileStreamOptions { Access = FileAccess.Read, Mode = FileMode.Open, Options = FileOptions.Asynchronous, Share = FileShare.ReadWrite })) {
                        if (fStream.Length <= LastBytePosition) {
                            Current = null;
                            goto AFTER_READ;
                        }


                        fStream.Seek(LastBytePosition, SeekOrigin.Begin);

                        // read event header bytes
                        Array.Clear(headerBuffer);
                        readOffset = fStream.Read(headerBuffer, 0, headerBuffer.Length);

                        if (readOffset <= 0) {
                            Current = null;
                            goto AFTER_READ;
                        }

                        var numberOfBytesToRead = BitConverter.ToInt32(headerBuffer);
                        LastBytePosition += EventStream.LengthOfEventHeader;

                        var ms = new MemoryStream(numberOfBytesToRead);

                        do {
                            Array.Clear(readBuffer);
                            readOffset = fStream.Read(readBuffer, 0, Math.Min(numberOfBytesToRead, EventStream.LengthOfEventHeader));

                            if (readOffset > 0) {
                                ms.Write(readBuffer, 0, readOffset);
                            }

                            LastBytePosition += readOffset;
                            numberOfBytesToRead -= readOffset;
                        } while (readOffset > 0);

                        ms.Seek(0, SeekOrigin.Begin);

                        var str = System.Text.Encoding.UTF8.GetString(ms.ToArray());

                        Cache.Append(_reader.Serializer.Deserialize<StreamMessage>(ms));
                    }
                }

            AFTER_READ:
                if (_messages.MoveNext()) {
                    Current = _messages.Current;
                } else {
                    Current = null;
                }

                return ValueTask.FromResult(Current is not null);
            }

            public ValueTask DisposeAsync() => ValueTask.CompletedTask;
        }
    }
}
