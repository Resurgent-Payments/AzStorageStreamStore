namespace LvStreamStore.Serialization {
    public interface IEventSerializer {
        Stream Serialize<T>(T @event);
        T Deserialize<T>(Stream stream);
    }
}
