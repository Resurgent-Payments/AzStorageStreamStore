//namespace LvStreamStore.ApplicationToolkit {
//    using System;

//    public record DelaySendEnvelope : Message {
//        public readonly TimePosition At;
//        public readonly Message ToSend;

//        public DelaySendEnvelope(TimePosition at, Message toSend) : base() {
//            At = at;
//            ToSend = toSend;
//        }

//        public DelaySendEnvelope(ITimeSource timeSource, TimeSpan delay, Message toSend) :
//            this(timeSource.Now() + delay, toSend) { }
//    }
//}
