namespace LvStreamStore.Tests {
    using Xunit;

    public class SinglyLinkedListTests {
        [Fact]
        public void A_list_can_be_constructed() {
            _ = new SinglyLinkedList<object>();
        }

        [Fact]
        public void Appending_an_item_does_not_fail() {
            var list = new SinglyLinkedList<object>();
            list.Append(new object());
        }

        [Fact]
        public void An_item_can_be_retrieved() {
            var item = new object();
            var list = new SinglyLinkedList<object>();

            list.Append(item);

            var appended = list[0];

            Assert.Same(item, appended);
        }

        [Fact]
        public void An_item_can_be_retrieved_from_a_page() {
            var item = new object();
            var list = new SinglyLinkedList<object>(10);

            for (var i = 0; i < 13; i++) {
                list.Append(new object());
            }
            list.Append(item);

            var appended = list[13];

            Assert.Same(item, appended);
        }

        [Fact]
        public void List_can_be_enumerated() {
            var numberOfObjects = 15;
            var list = new SinglyLinkedList<object>(10);
            for (var i = 0; i < numberOfObjects; i++) { list.Append(new object()); }

            var numberOfRecordedObjects = list.Count;
            var numberOfRecordedObjectsFromEnumerator = list.Count();

            Assert.Equal(numberOfObjects, numberOfRecordedObjects);
            Assert.Equal(numberOfObjects, numberOfRecordedObjectsFromEnumerator);
        }
    }
}