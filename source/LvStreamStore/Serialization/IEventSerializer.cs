namespace LvStreamStore.Serialization {
    public interface IEventSerializer {
        Stream Serialize<T>(T @event);
        T Deserialize<T>(Stream stream);
        T Deserialize<T>(Span<byte> bytes);
    }
}
