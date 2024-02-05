namespace LvStreamStore.Tests;

using Xunit;

public class LocalClientWithMemoryEventStreamTests : ClientTestBase, IClassFixture<MemoryClientTestFixture> {

    public LocalClientWithMemoryEventStreamTests(MemoryClientTestFixture fixture) : base(fixture) {

    }
}
