namespace LvStreamStore.Tests.Messaging;

using LvStreamStore.Messaging;

using Xunit;

public class TypeCastReceiverTests {


    [Fact]
    public async Task Can_type_cast_a_message() {
        TaskCompletionSource tcs = new();
        var msg = new SomeOutMsg();
        var receiver = new AdHocReceiver<SomeOutMsg>((msg) => {
            tcs.SetResult();
            return Task.CompletedTask;
        });
        var caster = new TypeCastReceiver<OutMsg, SomeOutMsg>(receiver);
        await caster.Receive(msg);
        await tcs.Task.WithTimeout(TimeSpan.FromMilliseconds(100));
    }

    record InMsg : Message;
    record OutMsg : Message;
    record SomeOutMsg : OutMsg;
}
