namespace LvStreamStore.Tests;

using Xunit;

public class KeyTests {
    [Fact]
    public void A_stream_id_can_convert_to_a_stream_key_when_casted() {
        var key = new StreamKey(new[] { "stream", "id" });
        var id = new StreamId("stream", Array.Empty<string>(), "id");

        Assert.Equal((StreamKey)id, key);
    }

    [Fact]
    public void A_stream_id_is_equivalent_to_a_stream_key() {
        var key = new StreamKey(new[] { "stream", "id" });
        var id = new StreamId("stream", Array.Empty<string>(), "id");

        Assert.True(key.Equals(id));
    }

    [Fact]
    public void Category_key_is_equivalent_to_object_key() {
        var category = new StreamKey(new[] { "first", "second" });
        var objectId = new StreamKey(new[] { "first", "second", "third" });

        Assert.Equal(category, objectId);
    }

    [Fact]
    public void Object_key_is_not_equivalent_to_stream_key() {
        var objectId = new StreamKey(new[] { "first", "second", "third" });
        var category = new StreamKey(new[] { "first", "second" });

        Assert.NotEqual(objectId, category);
    }

    [Fact]
    public void Stream_keys_can_have_its_hierarchy_iterated_upon() {
        var baseKey = new StreamKey(new[] { "first", "second", "third" });
        var ancestors = baseKey.GetAncestors();
        foreach (var ancestor in ancestors) {
            Assert.Equal(ancestor, baseKey);
        }
    }

    [Fact]
    public void A_known_stream_key_should_not_be_equivalent_to_null() {
        Assert.False((new StreamKey(new[] { "first", "second", "third" })).Equals(null));
    }

    [Fact]
    public void StreamId_and_StreamKey_can_be_equivalent_with_same_values() {
        var streamId = new StreamId("tenant", new[] { "hello", "world" }, "id");
        var streamKey = new StreamKey(new[] { "tenant", "hello", "world", "id" });
        Assert.True(streamKey == streamId);
    }

    [Fact]
    public void StreamId_and_StreamKey_cannot_be_equivalent_with_different_values() {
        var streamId = new StreamId("tenant", new[] { "hello", "world" }, "id");
        var streamKey = new StreamKey(new[] { "tenant", "hello", "john", "id" });
        Assert.True(streamKey != streamId);
    }

    [Fact]
    public void Two_StreamKey_instances_should_have_equivalent_hash_codes() {
        var streamKey1 = new StreamKey(new[] { "tenant", "hello", "world", "id" });
        var streamKey2 = new StreamKey(new[] { "tenant", "hello", "world", "id" });
        Assert.Equal(streamKey1.GetHashCode(), streamKey2.GetHashCode());
    }

    [Fact]
    public void Stream_keys_can_be_enumerated_to_see_hierarchy() {
        var streamKey = new StreamKey(new[] { "tenant", "hello", "world", "id" });
        Assert.Equal(3, streamKey.GetAncestors().Count());
    }

    [Theory]
    [InlineData(2, new[] { "tenant", "hello", "world" })]
    [InlineData(1, new[] { "tenant", "hello" })]
    [InlineData(0, new[] { "tenant" })]
    public void Stream_key_enumerator_can_step_through(int idx, string[] hello) {
        var streamKey = new StreamKey(new[] { "tenant", "hello", "world", "id" });
        var ancestors = streamKey.GetAncestors().ToArray();
        var ancestor = ancestors.Skip(idx).First();
        Assert.True(ancestor.Equals(new StreamKey(hello)));
    }
}