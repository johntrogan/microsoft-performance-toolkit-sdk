﻿// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Performance.SDK;
using Microsoft.Performance.SDK.Processing;
using Microsoft.Performance.Toolkit.Plugins.Core;
using Microsoft.Performance.Toolkit.Plugins.Core.Metadata;
using Microsoft.Performance.Toolkit.Plugins.Core.Packaging;
using Microsoft.Performance.Toolkit.Plugins.Core.Serialization;
using Microsoft.Performance.Toolkit.Plugins.Runtime.Exceptions;

namespace Microsoft.Performance.Toolkit.Plugins.Runtime.Installation
{
    /// <summary>
    ///     A file system based implementation of <see cref="IInstalledPluginStorage"/>.
    /// </summary>
    public sealed class FileSystemInstalledPluginStorage
        : IInstalledPluginStorage
    {
        private readonly IPluginsStorageDirectory storageDirectory;
        private readonly ISerializer<PluginContentsMetadata> contentsMetadataSerializer;
        private readonly IDirectoryChecksumCalculator checksumCalculator;
        private readonly ILogger logger;

        /// <summary>
        ///     Creates an instance of the <see cref="FileSystemInstalledPluginStorage"/>.
        /// </summary>
        /// <param name="storageDirectory">
        ///     The directory where the plugins are stored.
        /// </param>
        /// <param name="contentsMetadataSerializer">
        ///     The serializer to use to serialize and deserialize the plugin contents metadata.
        /// </param>
        /// <param name="checksumCalculator">
        ///     The checksum calculator to use to calculate the checksum of the plugin content.
        /// </param>
        /// <param name="loggerFactory">
        ///     The logger factory.
        /// </param>
        internal FileSystemInstalledPluginStorage(
            IPluginsStorageDirectory storageDirectory,
            ISerializer<PluginContentsMetadata> contentsMetadataSerializer,
            IDirectoryChecksumCalculator checksumCalculator,
            Func<Type, ILogger> loggerFactory)
        {
            Guard.NotNull(storageDirectory, nameof(storageDirectory));
            Guard.NotNull(checksumCalculator, nameof(checksumCalculator));
            Guard.NotNull(contentsMetadataSerializer, nameof(contentsMetadataSerializer));
            Guard.NotNull(loggerFactory, nameof(loggerFactory));

            this.storageDirectory = storageDirectory;
            this.contentsMetadataSerializer = contentsMetadataSerializer;
            this.checksumCalculator = checksumCalculator;
            this.logger = loggerFactory(typeof(FileSystemInstalledPluginStorage));
        }

        /// <inheritdoc/>
        public async Task<string> AddAsync(
            PluginPackage package,
            CancellationToken cancellationToken,
            IProgress<int> progress)
        {
            Guard.NotNull(package, nameof(package));
            Guard.NotNull(cancellationToken, nameof(cancellationToken));

            const int bufferSize = 4096;
            const int defaultAsyncBufferSize = 81920;

            string rootInstallDir = this.storageDirectory.GetRootDirectory(package.Metadata.Identity);

            long totalCopied = 0;
            long totalBytesToCopy = package.Entries.Select(e => e.InstalledSize).Sum();

            progress?.Report(0);

            try
            {
                foreach (PluginPackageEntry entry in package.Entries)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    string destPath;
                    bool isDirectory = false;

                    switch (entry.EntryType)
                    {
                        case PluginPackageEntryType.ContentFile:
                            string contentDir = this.storageDirectory.GetContentDirectory(package.Metadata.Identity);
                            destPath = Path.GetFullPath(Path.Combine(contentDir, entry.ContentRelativePath));

                            if (!destPath.StartsWith(contentDir))
                            {
                                // This can happen if the content relative path contains ".." or other invalid characters.
                                throw new PluginPackageExtractionException($"Invalid file path: {destPath}");
                            }

                            isDirectory = entry.RawPath.EndsWith("/");
                            break;
                        case PluginPackageEntryType.MetadataJsonFile:
                            destPath = this.storageDirectory.GetMetadataFilePath(package.Metadata.Identity);
                            break;
                        case PluginPackageEntryType.ContentsMetadataJsonFile:
                            destPath = this.storageDirectory.GetContentsMetadataFilePath(package.Metadata.Identity);
                            break;
                        case PluginPackageEntryType.Unknown:
                        default:
                            continue;
                    }

                    if (!destPath.StartsWith(rootInstallDir))
                    {
                        // This can happen if the package entry path contains ".." or other invalid characters.
                        throw new PluginPackageExtractionException($"Invalid file path: {destPath}");
                    }

                    string destDir = isDirectory ? destPath : Path.GetDirectoryName(destPath);
                    Directory.CreateDirectory(destDir);

                    if (isDirectory)
                    {
                        continue;
                    }

                    using (Stream entryStream = entry.Open())
                    using (var destStream = new FileStream(
                        destPath,
                        FileMode.Create,
                        FileAccess.Write,
                        FileShare.None,
                        bufferSize,
                        FileOptions.Asynchronous | FileOptions.SequentialScan))
                    {
                        byte[] buffer = new byte[defaultAsyncBufferSize];

                        int read = 0;
                        while ((read = await entryStream.ReadAsync(buffer, 0, buffer.Length, cancellationToken)) > 0)
                        {
                            if (cancellationToken.IsCancellationRequested)
                            {
                                throw new OperationCanceledException();
                            }

                            totalCopied += read;
                            await destStream.WriteAsync(buffer, 0, read, cancellationToken);

                            // report progress back
                            progress?.Report((int)(totalCopied / (double)totalBytesToCopy * 100));
                        }
                    }
                }

                progress?.Report(100);
            }
            catch (Exception e) when (!(e is OperationCanceledException))
            {
                string errorMsg = $"Unable to extract plugin content to {rootInstallDir}";
                this.logger.Error(e, errorMsg);
                throw new PluginPackageExtractionException(errorMsg, e);
            }

            string checksum = await this.checksumCalculator.GetDirectoryChecksumAsync(rootInstallDir);

            return checksum;
        }

        /// <inheritdoc/>
        public Task RemoveAsync(PluginIdentity pluginIdentity, CancellationToken cancellationToken)
        {
            Guard.NotNull(pluginIdentity, nameof(pluginIdentity));

            string installDir = this.storageDirectory.GetRootDirectory(pluginIdentity);

            return Task.Run(() =>
            {
                if (Directory.Exists(installDir))
                {
                    Directory.Delete(installDir, true);
                }
            }, cancellationToken);
        }

        /// <inheritdoc/>
        public async Task<PluginContentsMetadata> TryGetPluginContentsMetadataAsync(
            PluginIdentity installedPlugin,
            CancellationToken cancellationToken)
        {
            Guard.NotNull(installedPlugin, nameof(installedPlugin));

            string contentsMetadataFilePath = this.storageDirectory.GetContentsMetadataFilePath(installedPlugin);

            if (!File.Exists(contentsMetadataFilePath))
            {
                return null;
            }

            PluginContentsMetadata contentsMetadata;
            using (var fileStream = new FileStream(contentsMetadataFilePath, FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                contentsMetadata = await this.contentsMetadataSerializer.DeserializeAsync(
                    fileStream,
                    cancellationToken);
            }

            return contentsMetadata;
        }
    }
}
