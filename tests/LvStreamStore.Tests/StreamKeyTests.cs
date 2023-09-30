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
}