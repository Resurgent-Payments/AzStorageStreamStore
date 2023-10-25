namespace LvStreamStore.ApplicationToolkit {
    using System.Threading.Tasks;

    public class WideningHandler<TInput, TOutput> : IAsyncHandler<TInput>
        where TInput : TOutput
        where TOutput : Message {
        private readonly IAsyncHandler<TOutput> _handler;

        public WideningHandler(IAsyncHandler<TOutput> handler) {
            _handler = handler;
        }

        public ValueTask HandleAsync(TInput @event) => _handler.HandleAsync(@event);
    }
}
