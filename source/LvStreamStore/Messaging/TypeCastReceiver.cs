namespace LvStreamStore.Messaging;

internal class TypeCastReceiver<TIn, TOut> : IReceiver<TIn> where TIn : Message where TOut : Message {
    IReceiver<TOut> _inner;

    public TypeCastReceiver(IReceiver<TOut> inner) {
        _inner = inner;
    }
    public async Task Receive(TIn inMsg) {
        if (inMsg is TOut outMsg) { await _inner.Receive(outMsg); }
    }
}
