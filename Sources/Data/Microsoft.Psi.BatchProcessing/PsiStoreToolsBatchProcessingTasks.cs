// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.BatchProcessing
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using Microsoft.Psi.Data;
    using Microsoft.Psi.Imaging;
    using Microsoft.Psi.Interop.Format;
    using Microsoft.Psi.Interop.Serialization;
    using Microsoft.Psi.Interop.Transport;

#pragma warning disable SA1402 // File may only contain a single type
#pragma warning disable SA1649 // File name should match first type name

    /// <summary>
    /// Batch task that removes a stream from a store.
    /// </summary>
    [BatchProcessingTask("RemoveStream", Description = "Removes a stream from a store.")]
    public class RemoveStreamTask : BatchProcessingTask<RemoveStreamTaskConfiguration>
    {
        /// <inheritdoc/>
        public override void Run(Pipeline pipeline, SessionImporter sessionImporter, Exporter exporter, RemoveStreamTaskConfiguration configuration)
        {
            if (string.IsNullOrWhiteSpace(configuration.StreamName))
            {
                throw new InvalidOperationException("Stream Name must be specified.");
            }

            var sourceStore = PsiStoreTaskHelper.ResolveSourceStore(sessionImporter, configuration.PartitionName);
            PsiStoreTaskHelper.RemoveStreamFromStore(sourceStore, configuration.StreamName, configuration.OutputStorePath, configuration.OutputStoreName);
        }

        /// <inheritdoc/>
        public override bool IsDerivedPartitionNeeded() => false;
    }

    /// <summary>
    /// Batch task that saves stream messages to a file.
    /// </summary>
    [BatchProcessingTask("SaveStreamMessages", Description = "Exports stream messages to a file.")]
    public class SaveStreamMessagesTask : BatchProcessingTask<SaveStreamMessagesTaskConfiguration>
    {
        /// <inheritdoc/>
        public override void Run(Pipeline pipeline, SessionImporter sessionImporter, Exporter exporter, SaveStreamMessagesTaskConfiguration configuration)
        {
            if (string.IsNullOrWhiteSpace(configuration.StreamName))
            {
                throw new InvalidOperationException("Stream Name must be specified.");
            }

            var sourceStore = PsiStoreTaskHelper.ResolveSourceStore(sessionImporter, configuration.PartitionName);
            var workingStore = PsiStoreTaskHelper.PrepareWorkingStore(sourceStore, configuration);
            var outputFile = PsiStoreTaskHelper.ResolveOutputFilePath(workingStore, configuration.OutputFile, configuration.Format);

            var sourceImporter = PsiStore.Open(pipeline, workingStore.Name, workingStore.Path);
            var messages = sourceImporter.OpenDynamicStream(configuration.StreamName);
            messages.PipeTo(new FileWriter<dynamic>(pipeline, outputFile, PsiStoreTaskHelper.GetPersistentFormatSerializer(configuration.Format)));
        }

        /// <inheritdoc/>
        public override bool IsDerivedPartitionNeeded() => false;
    }

    /// <summary>
    /// Batch task that sends stream messages to a NetMQ endpoint.
    /// </summary>
    [BatchProcessingTask("SendStreamMessages", Description = "Sends stream messages to a transport endpoint.")]
    public class SendStreamMessagesTask : BatchProcessingTask<SendStreamMessagesTaskConfiguration>
    {
        /// <inheritdoc/>
        public override void Run(Pipeline pipeline, SessionImporter sessionImporter, Exporter exporter, SendStreamMessagesTaskConfiguration configuration)
        {
            if (string.IsNullOrWhiteSpace(configuration.StreamName))
            {
                throw new InvalidOperationException("Stream Name must be specified.");
            }

            if (string.IsNullOrWhiteSpace(configuration.Topic))
            {
                throw new InvalidOperationException("Topic must be specified.");
            }

            if (string.IsNullOrWhiteSpace(configuration.Address))
            {
                throw new InvalidOperationException("Address must be specified.");
            }

            var sourceStore = PsiStoreTaskHelper.ResolveSourceStore(sessionImporter, configuration.PartitionName);
            var workingStore = PsiStoreTaskHelper.PrepareWorkingStore(sourceStore, configuration);
            var sourceImporter = PsiStore.Open(pipeline, workingStore.Name, workingStore.Path);
            var messages = sourceImporter.OpenDynamicStream(configuration.StreamName);
            messages.PipeTo(new NetMQWriter<dynamic>(pipeline, configuration.Topic, configuration.Address, PsiStoreTaskHelper.GetFormatSerializer(configuration.Format)));
        }

        /// <inheritdoc/>
        public override bool IsDerivedPartitionNeeded() => false;
    }

    /// <summary>
    /// Batch task that concatenates stores.
    /// </summary>
    [BatchProcessingTask("ConcatenateStores", Description = "Concatenates multiple stores into one.")]
    public class ConcatenateStoresTask : BatchProcessingTask<ConcatenateStoresTaskConfiguration>
    {
        /// <inheritdoc/>
        public override void Run(Pipeline pipeline, SessionImporter sessionImporter, Exporter exporter, ConcatenateStoresTaskConfiguration configuration)
        {
            var sourceStore = PsiStoreTaskHelper.ResolveSourceStore(sessionImporter, configuration.PartitionName);
            var outputName = string.IsNullOrWhiteSpace(configuration.GeneratedOutputStoreName) ? $"{sourceStore.Name}-Concatenated" : configuration.GeneratedOutputStoreName;
            var outputPath = string.IsNullOrWhiteSpace(configuration.GeneratedOutputStorePath) ? sourceStore.Path : configuration.GeneratedOutputStorePath;

            var storeNames = string.IsNullOrWhiteSpace(configuration.StoreNames)
                ? new[] { sourceStore.Name }
                : configuration.StoreNames.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries).Select(s => s.Trim()).ToArray();

            var inputStores = storeNames.Select(name => (name, sourceStore.Path));
            PsiStore.Concatenate(inputStores, (outputName, outputPath), false);
        }

        /// <inheritdoc/>
        public override bool IsDerivedPartitionNeeded() => false;
    }

    /// <summary>
    /// Batch task that crops a store.
    /// </summary>
    [BatchProcessingTask("CropStore", Description = "Crops a store and writes the result to a new store.")]
    public class CropStoreTask : BatchProcessingTask<CropStoreTaskConfiguration>
    {
        /// <inheritdoc/>
        public override void Run(Pipeline pipeline, SessionImporter sessionImporter, Exporter exporter, CropStoreTaskConfiguration configuration)
        {
            var sourceStore = PsiStoreTaskHelper.ResolveSourceStore(sessionImporter, configuration.PartitionName);
            var inputStore = configuration.RunOnDatasetCopy ? PsiStoreTaskHelper.PrepareWorkingStore(sourceStore, configuration) : sourceStore;
            var outputName = string.IsNullOrWhiteSpace(configuration.OutputStoreName) ? $"{inputStore.Name}-Cropped" : configuration.OutputStoreName;
            var outputPath = string.IsNullOrWhiteSpace(configuration.OutputStorePath) ? inputStore.Path : configuration.OutputStorePath;
            PsiStore.Crop(inputStore, (outputName, outputPath), configuration.CropInterval, false, null, (log) => Console.Write(log));
        }

        /// <inheritdoc/>
        public override bool IsDerivedPartitionNeeded() => false;
    }

    /// <summary>
    /// Batch task that encodes image streams in a store.
    /// </summary>
    [BatchProcessingTask("EncodeStore", Description = "Encodes image streams to JPEG in a new store.")]
    public class EncodeStoreTask : BatchProcessingTask<EncodeStoreTaskConfiguration>
    {
        /// <inheritdoc/>
        public override void Run(Pipeline pipeline, SessionImporter sessionImporter, Exporter exporter, EncodeStoreTaskConfiguration configuration)
        {
            var sourceStore = PsiStoreTaskHelper.ResolveSourceStore(sessionImporter, configuration.PartitionName);
            var inputStore = configuration.RunOnDatasetCopy ? PsiStoreTaskHelper.PrepareWorkingStore(sourceStore, configuration) : sourceStore;
            var outputName = string.IsNullOrWhiteSpace(configuration.GeneratedOutputStoreName) ? $"{inputStore.Name}-Encoded" : configuration.GeneratedOutputStoreName;
            var outputPath = string.IsNullOrWhiteSpace(configuration.GeneratedOutputStorePath) ? inputStore.Path : configuration.GeneratedOutputStorePath;

            static bool IsImageStream(IStreamMetadata streamInfo)
                => streamInfo.TypeName.StartsWith("Microsoft.Psi.Shared`1[[Microsoft.Psi.Imaging.Image,", StringComparison.Ordinal);

            void EncodeImageStreams(IStreamMetadata streamInfo, PsiImporter importer, Exporter storeExporter)
            {
                importer
                    .OpenStream<Shared<Image>>(streamInfo.Name)
                    .Convert(PixelFormat.BGRA_32bpp)
                    .EncodeJpeg(configuration.Quality)
                    .Write(streamInfo.Name, storeExporter, true);
            }

            PsiStore.Process(IsImageStream, EncodeImageStreams, inputStore, (outputName, outputPath), false);
        }

        /// <inheritdoc/>
        public override bool IsDerivedPartitionNeeded() => false;
    }

    /// <summary>
    /// Provides helper methods for PsiStore tools batch tasks.
    /// </summary>
    internal static class PsiStoreTaskHelper
    {
        /// <summary>
        /// Resolves the source store name and path from a session importer and optional partition name.
        /// </summary>
        /// <param name="sessionImporter">The session importer.</param>
        /// <param name="partitionName">Optional partition name.</param>
        /// <returns>The source store tuple.</returns>
        public static (string Name, string Path) ResolveSourceStore(SessionImporter sessionImporter, string partitionName)
        {
            if (sessionImporter == null)
            {
                throw new ArgumentNullException(nameof(sessionImporter));
            }

            var importer = string.IsNullOrWhiteSpace(partitionName)
                ? sessionImporter.PartitionImporters.Values.FirstOrDefault()
                : sessionImporter.PartitionImporters.TryGetValue(partitionName, out var namedImporter) ? namedImporter : null;

            if (importer == null)
            {
                throw new InvalidOperationException("Could not resolve source partition importer.");
            }

            return (importer.StoreName, importer.StorePath);
        }

        /// <summary>
        /// Prepares a working store based on configuration. If copy mode is enabled, creates a copy and returns it.
        /// </summary>
        /// <param name="sourceStore">The source store.</param>
        /// <param name="configuration">The task configuration.</param>
        /// <returns>The working store tuple.</returns>
        public static (string Name, string Path) PrepareWorkingStore((string Name, string Path) sourceStore, PsiStoreToolTaskConfiguration configuration)
        {
            if (!configuration.RunOnDatasetCopy)
            {
                return sourceStore;
            }

            var copyPath = string.IsNullOrWhiteSpace(configuration.OutputStorePath) ? sourceStore.Path : configuration.OutputStorePath;
            var copyName = string.IsNullOrWhiteSpace(configuration.OutputStoreName) ? $"{sourceStore.Name}-BatchCopy" : configuration.OutputStoreName;

            if (PsiStore.Exists(copyName, copyPath))
            {
                copyName = $"{copyName}-{DateTime.UtcNow:yyyyMMddHHmmss}";
            }

            PsiStore.Copy(sourceStore, (copyName, copyPath), createSubdirectory: false);
            return (copyName, copyPath);
        }

        /// <summary>
        /// Removes a stream from an existing store by recreating store files without that stream.
        /// </summary>
        /// <param name="store">The target store.</param>
        /// <param name="streamName">The stream name to remove.</param>
        /// <param name="copyStorePath">The path to the copy store.</param>
        /// <param name="copyStoreName">The name of the copy store.</param>
        public static void RemoveStreamFromStore((string Name, string Path) store, string streamName, string copyStorePath, string copyStoreName)
        {
            PsiStore.Copy(store, (copyStoreName, copyStorePath), null, metadata => metadata.Name != streamName, false);
        }

        /// <summary>
        /// Resolves the output file path for save operations.
        /// </summary>
        /// <param name="store">The reference store.</param>
        /// <param name="outputFile">The configured output file.</param>
        /// <param name="format">The selected format.</param>
        /// <returns>The full output path.</returns>
        public static string ResolveOutputFilePath((string Name, string Path) store, string outputFile, StreamMessageFormat format)
        {
            var fileName = string.IsNullOrWhiteSpace(outputFile) ? $"messages.{format.ToString().ToLowerInvariant()}" : outputFile;
            return Path.IsPathRooted(fileName) ? fileName : Path.Combine(store.Path, fileName);
        }

        /// <summary>
        /// Gets the serializer for transport components.
        /// </summary>
        /// <param name="format">The selected format.</param>
        /// <returns>The serializer.</returns>
        public static IFormatSerializer GetFormatSerializer(StreamMessageFormat format)
            => format switch
            {
                StreamMessageFormat.Msg => MessagePackFormat.Instance,
                StreamMessageFormat.Csv => CsvFormat.Instance,
                _ => JsonFormat.Instance,
            };

        /// <summary>
        /// Gets the persistent serializer for file export components.
        /// </summary>
        /// <param name="format">The selected format.</param>
        /// <returns>The persistent serializer.</returns>
        public static IPersistentFormatSerializer GetPersistentFormatSerializer(StreamMessageFormat format)
            => format switch
            {
                StreamMessageFormat.Msg => MessagePackFormat.Instance,
                StreamMessageFormat.Csv => CsvFormat.Instance,
                _ => JsonFormat.Instance,
            };
    }
}
