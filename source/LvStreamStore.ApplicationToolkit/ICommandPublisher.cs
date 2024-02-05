//namespace LvStreamStore.ApplicationToolkit {
//    using System.Threading.Tasks;

//    public interface ICommandPublisher {
//        /// <summary>
//        /// Sends a command to a handler for processing.
//        /// </summary>
//        /// <typeparam name="TCommand">The command type that will be sent.</typeparam>
//        /// <param name="command">The command</param>
//        /// <returns>A <see cref="CommandResult"/> that will indicate success or failure.</returns>
//        /// <remarks>A developer may inherit from <see cref="CommandCompleted"/> or <see cref="CommandFailed"/> to provide richer command responses, if so desired.</remarks>
//        public ValueTask<CommandResult> SendAsync<TCommand>(TCommand @command) where TCommand : Command;

//        /// <summary>
//        /// Sends a command to a handler for processing.
//        /// </summary>
//        /// <typeparam name="TCommand">The command type that will be sent.</typeparam>
//        /// <param name="command">The command</param>
//        /// <returns>A <see cref="CommandResult"/> that will indicate success or failure.</returns>
//        /// <remarks>A developer may inherit from <see cref="CommandCompleted"/> or <see cref="CommandFailed"/> to provide richer command responses, if so desired.</remarks>
//        public ValueTask<CommandResult> SendAsync<TCommand>(TCommand @command, TimeSpan timeout) where TCommand : Command;
//    }
//}
