//namespace LvStreamStore.ApplicationToolkit {
//    using System;

//    public interface ISubscriber {
//        /// <summary>
//        /// Allows for listening to a published event, and subsequently reacting to the event.
//        /// </summary>
//        /// <typeparam name="TMessage"></typeparam>
//        /// <param name="event"></param>
//        /// <returns></returns>
//        public IDisposable Subscribe<TMessage>(IAsyncHandler<TMessage> handler) where TMessage : Message;

//        public IDisposable Subscribe<TCommand>(IAsyncCommandHandler<TCommand> handler) where TCommand : Command;
//    }
//}
