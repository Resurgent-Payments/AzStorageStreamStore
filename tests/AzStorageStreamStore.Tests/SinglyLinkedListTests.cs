namespace AzStorageStreamStore.Tests;

using Xunit;

public class SinglyLinkedListTests {
    [Fact]
    public void Should_hold_a_value() {
        SinglyLinkedList<object> objects = new();
        objects.Append(new());

        Assert.Equal(1, objects.Length);
    }

    [Fact]
    public void Should_support_IEnumerable_interface() {
        SinglyLinkedList<object> objects = new();
        objects.Append(new());
        objects.Append(new());
        objects.Append(new());

        long counter = 0;
        foreach (var x in objects) {
            counter += 1;
        }

        Assert.Equal(objects.Length, counter);
    }

    [Fact]
    public void Should_loop() {
        SinglyLinkedList<object> objects = new();
        objects.Append(new());
        objects.Append(new());
        objects.Append(new());

        long counter = 0;
        var enumerator = objects.GetEnumerator();
        while (enumerator.MoveNext()) {
            counter += 1;
        }

        Assert.Equal(objects.Length, counter);
    }

    [Fact]
    public void Should_reset() {
        SinglyLinkedList<object> objects = new();
        objects.Append(new());
        objects.Append(new());
        objects.Append(new());

        var enumerator = objects.GetEnumerator();
        Assert.True(enumerator.MoveNext());

        enumerator.Reset();

        Assert.Null(enumerator.Current);
    }

    [Fact]
    public void Can_get_at_index() {
        var obj = new object();
        SinglyLinkedList<object> objects = new();
        objects.Append(new());
        objects.Append(obj);
        objects.Append(new());

        Assert.Same(objects[1], obj);
    }

    [Fact]
    public void Does_not_move_next_without_items() {
        SinglyLinkedList<object> objects = new();
        var enumerator = objects.GetEnumerator();
        Assert.False(enumerator.MoveNext());
    }


    [Fact]
    public void Returns_correct_index_after_append() {
        SinglyLinkedList<object> objects = new();
        objects.Append(new());
        objects.Append(new());
        objects.Append(new());

        Assert.Equal(3, objects.Append(new()));
    }

    [Fact]
    public void Should_initialize_with_known_elements() {
        var list = new[]
        {
            new object(),
            new object(),
            new object()
        };

        SinglyLinkedList<object> objects = new(list);

        Assert.Equal(3, objects.Length);
    }

    [Fact]
    public void Enumerators_are_unique() {
        var list = new[]
        {
            new object(),
            new object(),
            new object()
        };

        SinglyLinkedList<object> objects = new(list);

        var enum1 = objects.GetEnumerator();
        var enum2 = objects.GetEnumerator();

        Assert.NotSame(enum1, enum2);
    }
}