namespace LvStreamStore.ApplicationToolkit {
    using System;
    using System.Collections.Generic;
    using System.Text.Json;

    using LvStreamStore.Messaging;

    internal class EventDeserializationReceiver<TEvent> : IReceiver<RecordedEvent> where TEvent : Event {
        IReceiver<TEvent> _inner;
        JsonSerializerOptions _options;

        public EventDeserializationReceiver(IReceiver<TEvent> inner, JsonSerializerOptions options) {
            _inner = inner;
            _options = options;
        }

        public async Task Receive(RecordedEvent msg) {

            // get dictionary.
            var eventOptions = JsonSerializer.Deserialize<Dictionary<string, string>>(new ReadOnlySpan<byte>(msg.Metadata), _options)!;

            // find key/value for event type
            if (!eventOptions.TryGetValue("AssemblyQualifiedName", out var aqName)) throw new InvalidOperationException("Missing clr type from serialized event information.");

            // get the clr type that should be decoded.
            var resolvedClrType = Type.GetType(aqName, true, true)!;

            if (resolvedClrType is not null && typeof(TEvent) != resolvedClrType) { return; }

            //todo: custom deserializer to pickup messageid.
            var @event = (TEvent)JsonSerializer.Deserialize(new ReadOnlySpan<byte>(msg.Data), resolvedClrType, _options)!;

            await _inner.Receive(@event);
        }
    }
}
