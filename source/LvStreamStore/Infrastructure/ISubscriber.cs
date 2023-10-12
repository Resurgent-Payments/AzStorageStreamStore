namespace LvStreamStore {
    using System;

    internal interface ISubscriber {
        /// <summary>
        /// Register to be called when a message is published of the type T or
        /// of a derived type from T
        /// </summary>
        /// <typeparam name="T">the type to be notified of</typeparam>
        /// <param name="handler">The object implementing IHandle T indicating function to be called</param>
        /// <param name="includeDerived">Register handlers on derived types</param>
        /// <returns>IDisposable wrapper to calling Dispose on the wrapper will unsubscribe</returns>
        IDisposable Subscribe<T>(IHandle<T> handler, bool includeDerived = true) where T : class, IMessage;

        /// <summary>
        /// Register to be called when any message is published 
        /// </summary>
        /// <param name="handler">The object implementing IHandle IMessage indicating function to be called</param>
        /// <returns>IDisposable wrapper to calling Dispose on the wrapper will unsubscribe</returns>
        IDisposable SubscribeToAll(IHandle<IMessage> handler);
    }
}
