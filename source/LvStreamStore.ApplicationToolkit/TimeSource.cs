namespace LvStreamStore.ApplicationToolkit {
    using System;
    using System.Threading;

    public interface ITimeSource {
        TimePosition Now();
        void WaitFor(TimePosition position, ManualResetEventSlim cancel);
    }
    public class TimeSource : ITimeSource {
        public static TimeSource System = new TimeSource();
        public TimePosition Now() {
            return new TimePosition(DateTimeOffset.UtcNow.ToUnixTimeMilliseconds());
        }
        public void WaitFor(TimePosition position, ManualResetEventSlim cancel) {
            cancel.Wait(Now().DistanceUntil(position));
        }
    }
}
