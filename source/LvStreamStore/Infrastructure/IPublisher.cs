namespace LvStreamStore {
    internal interface IPublisher {
        void Publish(IMessage message);
    }

    /// <summary>
    /// Marks <see cref="IPublisher"/> as being OK for
    /// cross-thread publishing (e.g. in replying to envelopes).
    /// </summary>
    public interface IThreadSafePublisher {
    }
}
