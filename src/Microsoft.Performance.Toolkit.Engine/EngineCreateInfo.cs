// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Microsoft.Performance.SDK;
using Microsoft.Performance.SDK.Auth;
using Microsoft.Performance.SDK.Options;
using Microsoft.Performance.SDK.Options.Values;
using Microsoft.Performance.SDK.Processing;
using Microsoft.Performance.SDK.Runtime;
using Microsoft.Performance.SDK.Runtime.Options;
using Microsoft.Performance.Toolkit.Engine.Options;

namespace Microsoft.Performance.Toolkit.Engine
{
    /// <summary>
    ///     Specifies a set of values that are used when
    ///     you create a <see cref="Engine"/>.
    /// </summary>
    public sealed class EngineCreateInfo
    {
        private static readonly string DefaultRuntimeName;

        private readonly Dictionary<Type, object> authProviders = new Dictionary<Type, object>();

        private ProcessorEnvironmentFactory processEnvironmentFactory;

        /// <summary>
        ///     Initializes the statc members of the <see cref="EngineCreateInfo"/>
        ///     class.
        /// </summary>
        static EngineCreateInfo()
        {
            EngineCreateInfo.DefaultRuntimeName = typeof(EngineCreateInfo).Assembly.GetName().Name;
        }

        /// <summary>
        ///     Initializes a new instance of the <see cref="EngineCreateInfo"/> class.
        /// </summary>
        /// <param name="dataSources">
        ///     The data sources to be processed in the engine.
        /// </param>
        /// <exception cref="ArgumentNullException">
        ///     <paramref name="dataSources"/> is <c>null</c>.
        /// </exception>
        public EngineCreateInfo(ReadOnlyDataSourceSet dataSources)
        {
            Guard.NotNull(dataSources, nameof(dataSources));

            this.DataSources = dataSources;
            this.OptionsResolver = new GlobalProcessingOptionsResolver(ProcessorOptions.Default);
        }

        /// <summary>
        ///     Specifies the <see cref="ProcessorOptions"/> to use when processing data sources.
        /// </summary>
        /// <param name="options"> <see cref="ProcessorOptions"/> to use for all processors that will process the data sources. </param>
        /// <returns>
        ///     The instance of <see cref="EngineCreateInfo"/>.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        ///     <paramref name="options"/> is <c>null</c>.
        /// </exception>
        public EngineCreateInfo WithProcessorOptions(ProcessorOptions options)
        {
            Guard.NotNull(options, nameof(options));

            return this.WithProcessorOptions(new GlobalProcessingOptionsResolver(options));
        }

        /// <summary>
        ///     Specifies the <see cref="ProcessorOptions"/> to use when processing data sources.
        /// </summary>
        /// <param name="optionsMap">A dictionary which maps <see cref="ProcessorOptions"/> for a given processor which will process the data sources. </param>
        /// <returns>
        ///     The instance of <see cref="EngineCreateInfo"/>.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        ///     <paramref name="optionsMap"/> is <c>null</c>.
        /// </exception>
        public EngineCreateInfo WithProcessorOptions(IDictionary<Guid, ProcessorOptions> optionsMap)
        {
            Guard.NotNull(optionsMap, nameof(optionsMap));

            return this.WithProcessorOptions(new ProcessingSourceOptionsResolver(optionsMap));
        }

        /// <summary>
        ///     Specifies an <see cref="IProcessingOptionsResolver"/> to provide <see cref="ProcessorOptions"/> to use when processing data sources.
        /// </summary>
        /// <remarks>
        ///     To set processor options for all processing sources use <see cref="WithProcessorOptions(ProcessorOptions)"/>.
        ///     To set processor options per processing source use <seealso cref="WithProcessorOptions(IDictionary{Guid, ProcessorOptions})"/>.
        /// </remarks>
        /// <returns>
        ///     The instance of <see cref="EngineCreateInfo"/>.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        ///     <paramref name="processingOptionsResolver"/> is <c>null</c>.
        /// </exception>
        public EngineCreateInfo WithProcessorOptions(IProcessingOptionsResolver processingOptionsResolver)
        {
            Guard.NotNull(processingOptionsResolver, nameof(processingOptionsResolver));

            this.OptionsResolver = processingOptionsResolver;
            return this;
        }

        /// <summary>
        ///     Registers an <see cref="IAuthProvider{TAuth, TResult}"/> of type <typeparamref name="TAuth"/> to be available to plugins
        ///     via <see cref="IApplicationEnvironment.TryGetAuthProvider{TAuth, TResult}"/>.
        /// </summary>
        /// <param name="provider">
        ///     The <see cref="IAuthProvider{TAuth, TResult}"/> to register.
        /// </param>
        /// <typeparam name="TAuth">
        ///     The type of <see cref="IAuthMethod{TResult}"/> for which the <see cref="IAuthProvider{TAuth, TResult}"/> is being registered.
        /// </typeparam>
        /// <typeparam name="TResult">
        ///     The type of the result of successful authentication requests for <typeparamref name="TAuth"/>.
        /// </typeparam>
        /// <returns>
        ///     The instance of <see cref="EngineCreateInfo"/>.
        /// </returns>
        /// <exception cref="InvalidOperationException">
        ///     An <see cref="IAuthProvider{TAuth, TResult}"/> for the specified type <typeparamref name="TAuth"/> already exists.
        /// </exception>
        public EngineCreateInfo WithAuthProvider<TAuth, TResult>(IAuthProvider<TAuth, TResult> provider)
            where TAuth : IAuthMethod<TResult>
        {
            Guard.NotNull(provider, nameof(provider));

            var toRegister = typeof(TAuth);
            if (!this.authProviders.TryAdd(toRegister, provider))
            {
                throw new InvalidOperationException($"An auth provider for the specified type {toRegister} already exists.");
            }

            return this;
        }

        /// <summary>
        ///     Registers a <see cref="ProcessEnvironmentFactory"/> to use for generating a custom processor 
        ///     environment.
        /// </summary>
        /// <param name="factory">
        ///     Factory for generating a <see cref="ProcessorEnvironment"/>.
        /// </param>
        /// <returns>
        ///     The instance of <see cref="EngineCreateInfo"/>.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        ///     <paramref name="factory"/> is <c>null</c>.
        /// </exception>
        public EngineCreateInfo WithProcessorEnvironmentFactory(ProcessorEnvironmentFactory factory)
        {
            Guard.NotNull(factory, nameof(factory));

            this.ProcessEnvironmentFactory = factory;
            return this;
        }

        /// <summary>
        ///     Specifies that the <see cref="PluginOption"/> whose <see cref="PluginOption.Guid" /> corresponds to
        ///     the specified <paramref name="optionGuid"/> should have the specified <paramref name="value" />.
        /// </summary>
        /// <param name="optionGuid">
        ///     The <see cref="PluginOption.Guid" /> of the <see cref="PluginOption"/> to set the value for.
        /// </param>
        /// <param name="value">
        ///     The value to set for the <see cref="PluginOption"/>.
        /// </param>
        /// <typeparam name="T">
        ///     The concrete type of the <see cref="PluginOptionValue"/> whose value should be set (e.g. <see cref="BooleanOptionValue"/>
        ///     or <see cref="FieldOptionValue"/>).
        /// </typeparam>
        /// <typeparam name="TValue">
        ///     The type of the value of the <see cref="PluginOption"/>.
        /// </typeparam>
        /// <returns>
        ///     The instance of <see cref="EngineCreateInfo"/>.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        ///     <paramref name="value"/> is <c>null</c>.
        /// </exception>
        public EngineCreateInfo WithPluginOptionValue<T, TValue>(Guid optionGuid, TValue value)
            where T : PluginOptionValue<TValue>
        {
            Guard.NotNull(value, nameof(value));

            this.RegisteredPluginOptions.Add(new EnginePluginOptionValue<T, TValue>(optionGuid, value));
            return this;
        }

        /// <summary>
        ///     Gets or sets the name of the runtime on which the application is built.
        /// </summary>
        /// <remarks>
        ///     Defaults to the engine assembly name.
        /// </remarks>
        public string RuntimeName { get; set; } = EngineCreateInfo.DefaultRuntimeName;

        /// <summary>
        ///     Gets or sets the application name.
        /// </summary>
        public string ApplicationName { get; set; }

        /// <summary>
        ///     Gets or sets a logger factory.
        /// </summary>
        /// <remarks>
        ///     The logger factory should be able to provide each processing source a logger specific to its type.
        /// </remarks>
        public Func<Type, ILogger> LoggerFactory { get; set; }

        /// <summary>
        ///     Gets the data sources that are to be processed by an <see cref="Engine"/>
        ///     instance.
        /// </summary>
        public ReadOnlyDataSourceSet DataSources { get; }

        /// <summary>
        ///     Gets the <see cref="IProcessingOptionsResolver"/> which can be used to provide <see cref="ProcessorOptions "/>.
        /// </summary>
        public IProcessingOptionsResolver OptionsResolver { get; private set; }

        /// <summary>
        ///     Gets or sets a value indicating whether the Engine instance will
        ///     allow for user interaction. If this property is set to <c>false</c>,
        ///     then any attempts made by plugins to get interaction from a user
        ///     will fail. By default, this property is <c>false</c>.
        /// </summary>
        public bool IsInteractive { get; set; }

        /// <summary>
        ///     Gets the <see cref="EnginePluginOptionValuesRegistry"/> that tracks which <see cref="PluginOptionValue"/>
        ///     instances have been registered.
        /// </summary>
        internal EnginePluginOptionValuesRegistry RegisteredPluginOptions { get; } = new();

        internal ProcessorEnvironmentFactory ProcessEnvironmentFactory
        {
            get
            {
                // Wrap any custom factory in the default factory. If there is no custom factory, or if it returns null,
                // then a default runtime will be created.
                return new RuntimeProcessorEnvironmentFactory(
                    (type) => this.LoggerFactory?.Invoke(type) ?? Logger.Create(type),
                    this.processEnvironmentFactory);
            }
            private set
            {
                this.processEnvironmentFactory = value;
            }
        }

        internal ReadOnlyDictionary<Type, object> AuthProviders => new ReadOnlyDictionary<Type, object>(this.authProviders);
    }
}
