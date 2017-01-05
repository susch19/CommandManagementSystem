﻿using CoMaS.Attributes;
using CoMaS.Interfaces;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace CoMaS
{
    /// <summary>
    /// An abstract base implementation of a command manager
    /// </summary>
    /// <typeparam name="TIn">Data type of command indentifiers</typeparam>
    /// <typeparam name="TParameter">Data type of command parameters</typeparam>
    /// <typeparam name="TOut">Return value of the dispatch method</typeparam>
    public abstract class CommandManager<TIn, TParameter, TOut> : ICommandManager<TIn, TParameter, TOut>
    {
        /// <summary>
        /// The main command handler
        /// </summary>
        protected CommandHandler<TIn, TParameter, TOut> commandHandler;
        /// <summary>
        /// Dictionary with waiting commands
        /// </summary>
        protected ConcurrentDictionary<TIn, Func<TParameter, TOut>> waitingDictionary;

        /// <summary>
        /// Delegate for command manager events
        /// </summary>
        /// <param name="command">Triggering command</param>
        /// <param name="arg">Passed parameters for the command</param>
        public delegate void CommandManagerEventHandler(ICommand<TParameter, TOut> command, TParameter arg);
        /// <summary>
        /// Dispatched when a command is completed
        /// </summary>
        public virtual event CommandManagerEventHandler OnFinishedCommand;
        /// <summary>
        /// Dispatched when a command is waiting
        /// </summary>
        public virtual event CommandManagerEventHandler OnWaitingCommand;

        /// <summary>
        /// An abstract base implementation of a command manager
        /// </summary>
        public CommandManager()
        {
            commandHandler = new CommandHandler<TIn, TParameter, TOut>();
            waitingDictionary = new ConcurrentDictionary<TIn, Func<TParameter, TOut>>();
            Initialize();
        }

        /// <summary>
        /// Initializes the command manager and registers the 
        /// corresponding commands at the command handler
        /// </summary>
        public virtual void Initialize()
        {
            var commandNamespace = GetType().GetCustomAttribute<CommandManagerAttribute>().CommandNamespaces;

            var commands = Assembly.GetExecutingAssembly().GetTypes().Where(
                t => t.GetCustomAttribute<CommandAttribute>() != null && commandNamespace.Contains(t.Namespace)).ToList();

            foreach (var command in commands)
            {
                commandHandler[(TIn)command.GetCustomAttribute<CommandAttribute>().Tag] += (e)
                    => InitializeCommand(command, e);
            }
        }

        /// <summary>
        /// Dispatch the specified command and pass the parameters
        /// </summary>
        /// <param name="command">The command Indentifier</param>
        /// <param name="arg">The parameters to be transferred</param>
        /// <returns>Returns the result of the dispatch</returns>
        public virtual TOut Dispatch(TIn command, TParameter arg) =>
            commandHandler.Dispatch(command, arg);

        /// <summary>
        /// Dispatch the specified command and pass the parameters asynchronous
        /// </summary>
        /// <param name="command">The command Indentifier</param>
        /// <param name="arg">The parameters to be transferred</param>
        /// <returns>Returns the result of the dispatch</returns>
        public virtual async Task<TOut> DispatchAsync(TIn command, TParameter arg) =>
            await Task.Run(() => Dispatch(command, arg));

        /// <summary>
        /// Initializes the passed command with the parameters
        /// </summary>
        /// <param name="command">The command</param>
        /// <param name="arg">The parameters to be transferred</param>
        /// <returns>Returns the result of the initialize</returns>
        public virtual TOut InitializeCommand(ICommand<TParameter, TOut> command, TParameter arg)
        {
            command.FinishEvent += Command_FinishEvent;
            command.WaitEvent += Command_WaitEvent;

            return command.Initialize(arg);
        }
        /// <summary>
        /// Initializes a command from the specified command datatype with the parameters
        /// </summary>
        /// <param name="commandType">The command data type</param>
        /// <param name="arg">The parameters to be transferred</param>
        /// <returns>Returns the result of the initialize</returns>
        public virtual TOut InitializeCommand(Type commandType, TParameter arg)
        {
            var command = (ICommand<TParameter, TOut>)Activator.CreateInstance(commandType);
            return InitializeCommand(command, arg);
        }
        /// <summary>
        /// Initializes a command from the specified command datatype with
        /// the parameters and start parameters for the constructor
        /// </summary>
        /// <param name="commandType">The command data type</param>
        /// <param name="arg">The parameters to be transferred</param>
        /// <param name="startParams">Parameters passed to the constructor</param>
        /// <returns>Returns the result of the initialize</returns>
        public virtual TOut InitializeCommand(Type commandType, TParameter arg, params object[] startParams)
        {
            var command = (ICommand<TParameter, TOut>)Activator.CreateInstance(commandType, startParams);
            return InitializeCommand(command, arg);
        }

        /// <summary>
        /// Executed when a command is finished
        /// </summary>
        /// <param name="sender">The triggering command</param>
        /// <param name="arg">The command parameters</param>
        public virtual void Command_FinishEvent(object sender, TParameter arg)
        {
            Func<TParameter, TOut> method;
            var command = (ICommand<TParameter, TOut>)sender;
            waitingDictionary.TryRemove((TIn)command.TAG, out method);
            OnFinishedCommand?.Invoke(command, arg);
        }

        /// <summary>
        /// Executed when a command is waiting
        /// </summary>
        /// <param name="sender">The triggering command</param>
        /// <param name="arg">The dispatch method</param>
        public virtual void Command_WaitEvent(object sender, Func<TParameter, TOut> arg)
        {
            if (arg == null && sender == null)
                return;
            var command = (ICommand<TParameter, TOut>)sender;

            if (!waitingDictionary.TryAdd((TIn)command.TAG, arg))
                waitingDictionary.TryUpdate((TIn)command.TAG, arg, arg);
        }
    }

    /// <summary>
    /// An abstract base implementation of a command manager with string as command indentifiers
    /// </summary>
    /// <typeparam name="TParameter">Data type of command parameters</typeparam>
    /// <typeparam name="TOut">Return value of the dispatch method</typeparam>
    public abstract class CommandManager<TParameter, TOut> : CommandManager<string, TParameter, TOut> { }

    /// <summary>
    /// An abstract base implementation of a command manager with string as command indentifiers
    /// and dynamic as result data type
    /// </summary>
    /// <typeparam name="TParameter">Data type of command parameters</typeparam>
    public abstract class CommandManager<TParameter> : CommandManager<TParameter, dynamic> { }

    /// <summary>
    /// An abstract base implementation of a command manager with string as command indentifiers
    /// and dynamic as result data type and EventArgs as parameter type
    /// </summary>
    public abstract class CommandManager : CommandManager<EventArgs> { }
}