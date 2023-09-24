namespace AzStorageStreamStore.Tests {
    using System;
    using System.Linq;
    using System.Threading.Tasks;

    using Xunit;

    public class MemoryFileManagerTests {
        IDataFileManager _fileManager;

        public MemoryFileManagerTests() {
            _fileManager = new MemoryDataFileManager();
        }

        [Fact]
        public async Task TheFileExists() => Assert.True(await _fileManager.ExistsAsync());

        [Fact]
        public async Task CheckpointShouldBeZeroWithoutAnyOperations() => Assert.Equal(0, await _fileManager.GetLengthAsync());

        [Fact]
        public async Task ByteArrayShouldBeEmptyWithNoDataAvailable() {
            var buffer = new byte[4096];  /// disk block size.

            var read = await _fileManager.ReadLogAsync(buffer, 0, buffer.Length);

            Assert.Equal(0, read);
        }

        [Fact]
        public async Task ByteArrayShouldHaveDataEquivalentToWritten() {
            var expected = new byte[] { 1, 2, 3, 4 };
            byte[] writeBuffer = new byte[4096];
            Array.Copy(expected, writeBuffer, expected.Length);

            // write
            await _fileManager.WriteAsync(writeBuffer);
            _fileManager.Seek(0, SeekOrigin.Begin);

            // read
            var readbuffer = new byte[4096];
            var bytesRead = await _fileManager.ReadLogAsync(readbuffer, 0, readbuffer.Length);

            Assert.Equal(4096, bytesRead);
            Assert.Equal(4, _fileManager.Checkpoint);
            Assert.Equivalent(expected, readbuffer.Take(_fileManager.Checkpoint).ToArray());
        }

        [Fact]
        public async Task MultipleWritesShouldKeepDataInline() {
            var expected = new byte[] { 1, 2, 3, 4, 4, 3, 2, 1 };
            byte[] writeBuffer = new byte[4096];

            var writes = new[] {
                new byte[] { 1, 2, 3, 4 },
                new byte[] { 4, 3, 2, 1 }
            };

            foreach (var toWrite in writes) {
                Array.Clear(writeBuffer);
                Array.Copy(toWrite, writeBuffer, toWrite.Length);
                await _fileManager.WriteAsync(toWrite);
            }


            // read
            var readbuffer = new byte[4096];
            _fileManager.Seek(0, SeekOrigin.Begin);
            var bytesRead = await _fileManager.ReadLogAsync(readbuffer, 0, readbuffer.Length);

            Assert.True(bytesRead > 0);
            Assert.Equal(8, _fileManager.Checkpoint);
            Assert.Equivalent(expected, readbuffer.Take(_fileManager.Checkpoint).ToArray());
        }
    }
}
