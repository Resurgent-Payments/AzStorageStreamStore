namespace LvStreamStore.LocalStorage.Tests;
using LvStreamStore.Tests;

using Xunit;

public class LocalStorageEventStreamTests : ClientTestBase, IClassFixture<LocalStorageTestFixtureWithoutCaching> {

    public LocalStorageEventStreamTests(LocalStorageTestFixtureWithoutCaching fixture) : base(fixture) {

    }
}
