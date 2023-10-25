namespace LvStreamStore.ApplicationToolkit {
    using System.Threading.Tasks;

    public interface IPublisher {
        /// <summary>
        /// Publishes an event to consumers.
        /// </summary>
        /// <typeparam name="TMessage">The "type" of event</typeparam>
        /// <param name="message">The event to be published.</param>
        /// <returns>A <see cref="ValueTask"/> that can be awaited.</returns>
        public ValueTask PublishAsync<TMessage>(TMessage message) where TMessage : Event;
    }
}
