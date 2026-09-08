// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.PsiStudio.Search
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Microsoft.Psi.Data.Annotations;

    /// <summary>
    /// Maintains an in-memory cache of annotations from a stream for efficient searching.
    /// </summary>
    /// <remarks>
    /// This cache stores a sorted copy of annotations from the active stream. It includes versioning
    /// to support cache invalidation when the underlying stream is modified. The cache is ordered
    /// by annotation start time for efficient forward and backward searches.
    /// </remarks>
    public class AnnotationStreamCache
    {
        private List<TimeIntervalAnnotation> cachedAnnotations;
        private int cacheVersion;
        private DateTime lastUpdateTime;

        /// <summary>
        /// Initializes a new instance of the <see cref="AnnotationStreamCache"/> class.
        /// </summary>
        public AnnotationStreamCache()
        {
            this.cachedAnnotations = new List<TimeIntervalAnnotation>();
            this.cacheVersion = 0;
            this.lastUpdateTime = DateTime.UtcNow;
        }

        /// <summary>
        /// Gets the current cache version. This increments each time the cache is invalidated.
        /// </summary>
        public int CacheVersion => this.cacheVersion;

        /// <summary>
        /// Gets the time when the cache was last updated.
        /// </summary>
        public DateTime LastUpdateTime => this.lastUpdateTime;

        /// <summary>
        /// Gets the number of annotations currently cached.
        /// </summary>
        public int Count => this.cachedAnnotations.Count;

        /// <summary>
        /// Gets a value indicating whether the cache is empty.
        /// </summary>
        public bool IsEmpty => this.cachedAnnotations.Count == 0;

        /// <summary>
        /// Loads annotations into the cache from a provided enumerable collection.
        /// </summary>
        /// <param name="annotations">The annotations to cache.</param>
        public void LoadAnnotations(IEnumerable<TimeIntervalAnnotation> annotations)
        {
            if (annotations == null)
            {
                throw new ArgumentNullException(nameof(annotations));
            }

            // Sort annotations by start time for efficient searching
            this.cachedAnnotations = annotations
                .OrderBy(a => a.Interval.Left)
                .ToList();

            this.lastUpdateTime = DateTime.UtcNow;
            this.cacheVersion++;
        }

        /// <summary>
        /// Invalidates the cache, incrementing its version number.
        /// </summary>
        public void Invalidate()
        {
            this.cachedAnnotations.Clear();
            this.cacheVersion++;
            this.lastUpdateTime = DateTime.UtcNow;
        }

        /// <summary>
        /// Gets all cached annotations.
        /// </summary>
        /// <returns>A read-only list of all cached annotations.</returns>
        public IReadOnlyList<TimeIntervalAnnotation> GetAllAnnotations()
        {
            return this.cachedAnnotations.AsReadOnly();
        }

        /// <summary>
        /// Finds the first annotation whose start time is at or after the specified time.
        /// </summary>
        /// <param name="time">The time to search from.</param>
        /// <returns>The index of the first matching annotation, or -1 if none found.</returns>
        public int FindFirstAnnotationIndexAtOrAfter(DateTime time)
        {
            if (this.cachedAnnotations.Count == 0)
            {
                return -1;
            }

            // Binary search for efficiency
            int left = 0;
            int right = this.cachedAnnotations.Count - 1;
            int result = -1;

            while (left <= right)
            {
                int mid = left + ((right - left) / 2);
                int comparison = this.cachedAnnotations[mid].Interval.Left.CompareTo(time);

                if (comparison >= 0)
                {
                    result = mid;
                    right = mid - 1;
                }
                else
                {
                    left = mid + 1;
                }
            }

            return result;
        }

        /// <summary>
        /// Finds the last annotation whose start time is before the specified time.
        /// </summary>
        /// <param name="time">The time to search from.</param>
        /// <returns>The index of the last matching annotation, or -1 if none found.</returns>
        public int FindLastAnnotationIndexBefore(DateTime time)
        {
            if (this.cachedAnnotations.Count == 0)
            {
                return -1;
            }

            // Binary search for efficiency
            int left = 0;
            int right = this.cachedAnnotations.Count - 1;
            int result = -1;

            while (left <= right)
            {
                int mid = left + ((right - left) / 2);
                int comparison = this.cachedAnnotations[mid].Interval.Left.CompareTo(time);

                if (comparison < 0)
                {
                    result = mid;
                    left = mid + 1;
                }
                else
                {
                    right = mid - 1;
                }
            }

            return result;
        }

        /// <summary>
        /// Gets annotations within a specified time interval.
        /// </summary>
        /// <param name="interval">The time interval to query.</param>
        /// <returns>A list of annotations that fall within the specified interval.</returns>
        public List<TimeIntervalAnnotation> GetAnnotationsInInterval(TimeInterval interval)
        {
            if (interval == null)
            {
                throw new ArgumentNullException(nameof(interval));
            }

            return this.cachedAnnotations
                .Where(a => a.Interval.IntersectsWith(interval))
                .ToList();
        }

        /// <summary>
        /// Gets an annotation at a specific index in the sorted cache.
        /// </summary>
        /// <param name="index">The zero-based index.</param>
        /// <returns>The annotation at the specified index.</returns>
        public TimeIntervalAnnotation GetAnnotationAt(int index)
        {
            if (index < 0 || index >= this.cachedAnnotations.Count)
            {
                throw new ArgumentOutOfRangeException(nameof(index));
            }

            return this.cachedAnnotations[index];
        }
    }
}
