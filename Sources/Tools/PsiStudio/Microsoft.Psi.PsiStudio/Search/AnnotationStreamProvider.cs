// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.PsiStudio.Search
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Microsoft.Psi.Data;
    using Microsoft.Psi.Data.Annotations;
    using Microsoft.Psi.Persistence;
    using Microsoft.Psi.Visualization;
    using Microsoft.Psi.Visualization.ViewModels;

    /// <summary>
    /// Provides access to annotation streams from the active session via DataManager and SessionViewModel.
    /// </summary>
    public class AnnotationStreamProvider
    {
        /// <summary>
        /// Gets the available annotation streams from the active dataset.
        /// </summary>
        /// <returns>List of annotation stream names.</returns>
        public virtual List<(string, string)> GetAvailableAnnotationStreams()
        {
            var streamNames = new List<(string, string)>();

            try
            {
                // Attempt to get streams from the active dataset/session
                var currentDataset = this.GetCurrentDataset();
                if (currentDataset != null)
                {
                    streamNames = this.GetStreamsFromDataset(currentDataset);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error getting available streams: {ex.Message}");
            }

            return streamNames;
        }

        /// <summary>
        /// Loads annotations from the specified stream.
        /// </summary>
        /// <param name="streamName">Name of the annotation stream.</param>
        /// <returns>List of loaded annotations.</returns>
        public virtual List<TimeIntervalAnnotation> LoadAnnotationsFromStream(string streamName)
        {
            var annotations = new List<TimeIntervalAnnotation>();

            try
            {
                if (string.IsNullOrWhiteSpace(streamName))
                {
                    return annotations;
                }

                var streams = this.GetStreamsFromDataset(this.GetCurrentDataset()).Where(s => s.Item1 == streamName).ToList();
                if (streams.Count != 1)
                {
                    return annotations;
                }

                var partitions = this.GetCurrentDataset().CurrentSessionViewModel.Session.Partitions.Where(p => p.Name == streams.First().Item2);
                if (partitions.Count() != 1)
                {
                    return annotations;
                }

                var partition = partitions.First();
                var stream = partition.AvailableStreams.FirstOrDefault(s => s.Name == streamName);
                PsiStoreStreamReader psiStoreReader = new PsiStoreStreamReader(stream.StoreName, stream.StorePath);
                psiStoreReader.OpenStream<TimeIntervalAnnotationSet>(streamName, (annotation, envelope) =>
                {
                    annotations.AddRange(annotation.Annotations);
                });
                psiStoreReader.ReadAll(ReplayDescriptor.ReplayAll, default);
                psiStoreReader.Dispose();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading annotations from stream: {ex.Message}");
            }

            return annotations;
        }

        /// <summary>
        /// Watches a stream for changes and invalidates the cache when changes occur.
        /// </summary>
        /// <param name="cache">The cache to invalidate.</param>
        /// <param name="streamName">Name of the stream to watch.</param>
        public virtual void WatchStreamForChanges(AnnotationStreamCache cache, string streamName)
        {
            // This method can be extended to watch for annotation changes
            // and automatically invalidate the cache
            // Example implementation would subscribe to AnnotationsChanged events
        }

        /// <summary>
        /// Gets the current active dataset (to be overridden by implementation).
        /// </summary>
        /// <returns>The current dataset object, or null if none is active.</returns>
        protected virtual DatasetViewModel GetCurrentDataset()
        {
            return VisualizationContext.Instance.DatasetViewModel;
        }

        /// <summary>
        /// Gets annotation streams from the dataset.
        /// </summary>
        /// <param name="dataset">The dataset object.</param>
        /// <returns>List of tuples containing stream names and their partition.</returns>
        protected virtual List<(string, string)> GetStreamsFromDataset(DatasetViewModel dataset)
        {
            var streamNames = new List<(string, string)>();

            if (dataset.CurrentSessionViewModel != null && dataset.CurrentSessionViewModel != null)
            {
                foreach (var partition in dataset.CurrentSessionViewModel.Session.Partitions)
                {
                    foreach (var stream in partition.AvailableStreams)
                    {
                        if (stream.TypeName.Contains("TimeIntervalAnnotation"))
                        {
                            streamNames.Add((stream.Name, partition.Name));
                        }
                    }
                }
            }

            return streamNames;
        }
    }
}
