// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using Microsoft.Performance.SDK.Auth;
using Microsoft.Performance.SDK.Extensibility.DataCooking;
using Microsoft.Performance.SDK.Extensibility.SourceParsing;

namespace Microsoft.Performance.SDK.Processing
{
    /// <summary>
    ///     Exposes information about the application environment in
    ///     which the <see cref="IProcessingSource"/> is being executed.
    /// </summary>
    public interface IApplicationEnvironment
    {
        /// <summary>
        /// The name of the application, if specified. This value may be <c>null</c>.
        /// </summary>
        string ApplicationName { get; }

        /// <summary>
        /// The name of the runtime in which this application runs. This value may be <c>null</c>.
        /// </summary>
        string RuntimeName { get; }

        /// <summary>
        ///     Gets a value indicating whether the current process supports
        ///     user interaction.
        /// </summary>
        /// <remarks>
        ///     This flag is useful to determine whether the code is
        ///     running in context where user interaction is available.
        /// </remarks>
        bool IsInteractive { get; }

        /// <summary>
        ///     Gets the serializer to use for deserializing <see cref="TableConfiguration"/> instances.
        /// </summary>
        ITableConfigurationsSerializer Serializer { get; }

        /// <summary>
        ///     Provides the interface to be used to notify that data in
        ///     a table has changed.
        /// </summary>
        ITableDataSynchronization TableDataSynchronizer { get; }

        /// <summary>
        ///     Gets a value indicating whether Verbose output has
        ///     been enabled 
        /// </summary>
        bool VerboseOutput { get; }

        /// <summary>
        ///     Used to get a factory for a given source data cooker.
        /// </summary>
        ISourceDataCookerFactoryRetrieval SourceDataCookerFactoryRetrieval { get; }

        /// <summary>
        ///     A factory to create a SourceSession.
        /// </summary>
        ISourceSessionFactory SourceSessionFactory { get; }

        /// <summary>
        ///     Displays the given message of the given type to the user,
        ///     using the formatting information specified by the <paramref name="formatProvider"/>.
        ///     The message is displayed in a message box.
        /// </summary>
        /// <param name="messageType">
        ///     The type of message being displayed.
        /// </param>
        /// <param name="formatProvider">
        ///     An object that supplies culture-specific formatting information.
        /// </param>
        /// <param name="format">
        ///     A composite format string.
        /// </param>
        /// <param name="args">
        ///     The objects to format using <paramref name="format"/>.
        /// </param>
        void DisplayMessage(
            MessageType messageType,
            IFormatProvider formatProvider,
            string format,
            params object[] args);

        /// <summary>
        ///     Displays the given message of the given type to the user,
        ///     using the formatting information specified by the <paramref name="formatProvider"/>.
        ///     The message is displayed in a message box with buttons.
        /// </summary>
        /// <param name="messageType">
        ///     The type of message being displayed.
        /// </param>
        /// <param name="formatProvider">
        ///     An object that supplies culture-specific formatting information.
        /// </param>
        /// <param name="buttons">
        ///     The buttons on the message box.
        /// </param>
        /// <param name="caption">
        ///     A simple description about what is being asked.
        /// </param>
        /// <param name="format">
        ///     A composite format string.
        /// </param>
        /// <param name="args">
        ///     The objects to format using <paramref name="format"/>.
        /// </param>
        /// <returns>
        ///     <c>true</c> if the user selects 'yes'; <c>false</c>
        ///     otherwise.
        /// </returns>
        ButtonResult MessageBox(
            MessageType messageType,
            IFormatProvider formatProvider,
            Buttons buttons,
            string caption,
            string format,
            params object[] args);

        /// <summary>
        ///     Attempts to get an <see cref="IAuthProvider{TAuth, TResult}"/> that can provide authentication
        ///     for <see cref="IAuthMethod{TResult}"/> of type <typeparamref name="TAuth"/>.
        /// </summary>
        /// <param name="provider">
        ///     The found provider, or <c>null</c> if no registered provider can provide authentication for
        ///     <typeparamref name="TAuth"/>.
        /// </param>
        /// <typeparam name="TAuth">
        ///     The type of the <see cref="IAuthMethod{TResult}"/> for which to attempt to get a provider.
        /// </typeparam>
        /// <typeparam name="TResult">
        ///     The type of the result of a successful authentication for <typeparamref name="TAuth"/>.
        /// </typeparam>
        /// <returns>
        ///     <c>true</c> if a provider was found; <c>false</c> otherwise. If <c>false</c> is returned,
        ///     <paramref name="provider"/> will be <c>null</c>.
        /// </returns>
        bool TryGetAuthProvider<TAuth, TResult>(out IAuthProvider<TAuth, TResult> provider)
            where TAuth : IAuthMethod<TResult>;
    }
}
