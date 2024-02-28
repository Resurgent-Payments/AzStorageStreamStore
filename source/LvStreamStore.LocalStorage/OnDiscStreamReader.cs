namespace LvStreamStore.LocalStorage;

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

using LvStreamStore.Serialization;

internal class OnDiscStreamReader : EventStreamReader {
    private readonly string _dataFileLocation;
    private readonly IEventSerializer _serializer;
    private readonly LocalStorageEventStreamOptions _options;

    private int _position = 0;

    public OnDiscStreamReader(string dataFileLocation, IEventSerializer serializer, LocalStorageEventStreamOptions options) {
        _dataFileLocation = dataFileLocation;
        _serializer = serializer;
        _options = options;
    }

    public override IAsyncEnumerator<StreamMessage> GetAsyncEnumerator(CancellationToken cancellationToken = default)
        => new Enumerator(this);

    public class Enumerator : IStreamEnumerator {
        private readonly OnDiscStreamReader _owner;
        Queue<StreamMessage> _queue = new();

        public Enumerator(OnDiscStreamReader owner) {
            _owner = owner;
            Current = null!;
        }

        public StreamMessage Current { get; private set; }

        public ValueTask DisposeAsync() => ValueTask.CompletedTask;

        public async ValueTask<bool> MoveNextAsync() {
            StreamMessage? queued;

            if (_queue.TryDequeue(out queued)) {
                Current = queued;
                return true;
            }

            var content = new byte[_owner._options.FileReadBlockSize];
            using (var fStream = new FileStream(_owner._dataFileLocation, new FileStreamOptions { Access = FileAccess.Read, Mode = FileMode.Open, Options = FileOptions.Asynchronous, Share = FileShare.ReadWrite })) {
                fStream.Seek(_owner._position, SeekOrigin.Begin);

                Array.Clear(content);
                var offset = await fStream.ReadAsync(content, 0, content.Length);
                if (offset < 0) {
                    Current = null!;
                    return false;
                }

                offset = SplitMessagesFromStream(content, _owner._serializer, _queue);
                _owner._position += offset;
            }

            if (_queue.TryDequeue(out queued)) {
                Current = queued;
                return true;
            }

            return false;

            /// <summary>
            /// Reads the bytes into the message queue, returning the next position to read from on the next chunk to be processed.
            /// </summary>
            static int SplitMessagesFromStream(byte[] bytes, IEventSerializer serializer, Queue<StreamMessage> inMemoryStorage) {
                var pos = 0;
                var toRead = 0;

                do {
                    // if the total "next" message size is greater than the last of the elements of the stream, drop here and re-read this section on next "page"
                    if (bytes.Length <= pos) { break; }

                    var head = bytes[pos..(pos + EventStream.LengthOfEventHeader)];
                    toRead = BitConverter.ToInt32(head);
                    if (toRead <= 0) { break; } // nothing left in array.  can return.

                    var msgEndPos = pos + EventStream.LengthOfEventHeader + toRead;

                    // if the total "next" message size is greater than the last of the elements of the stream, drop here and re-read this section on next "page"
                    if (bytes.Length <= msgEndPos) { break; }

                    pos += EventStream.LengthOfEventHeader;
                    var content = new Span<byte>(bytes, pos, toRead);
                    var msg = serializer.Deserialize<StreamMessage>(content);
                    inMemoryStorage.Enqueue(msg);

                    pos += toRead;
                } while (true);

                return pos;
            }
        }
    }
}
