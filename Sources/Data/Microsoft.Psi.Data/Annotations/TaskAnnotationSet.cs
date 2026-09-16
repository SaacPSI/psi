// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Data.Annotations
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    /// <summary>
    /// Represents a set of overlapping task annotations that belong to separate tracks but end at the same time.
    /// </summary>
    /// <remarks>
    /// This data structure provides the basis for persisting overlapping time interval annotations
    /// in \psi streams. It captures a set of overlapping time interval annotations that are on
    /// different tracks but end at the same time, captured by <see cref="EndTime"/>.
    /// When persisted to a stream, the originating time of the <see cref="Message{TaskAnnotationSet}"/>
    /// should correspond to the <see cref="EndTime"/>.
    /// </remarks>
    public class TaskAnnotationSet
    {
        private readonly Dictionary<string, TaskAnnotation> data = new ();

        /// <summary>
        /// Initializes a new instance of the <see cref="TaskAnnotationSet"/> class.
        /// </summary>
        /// <param name="taskAnnotation">The time interval annotation.</param>
        public TaskAnnotationSet(TaskAnnotation taskAnnotation)
        {
            this.data.Add(taskAnnotation.Track, taskAnnotation);
        }

        /// <summary>
        /// Gets the end time for the annotation set.
        /// </summary>
        public DateTime EndTime => this.data.Values.First().Interval.Right;

        /// <summary>
        /// Gets the set of tracks spanned by these time interval annotations.
        /// </summary>
        public IEnumerable<string> Tracks => this.data.Keys;

        /// <summary>
        /// Gets the set of annotations.
        /// </summary>
        public IEnumerable<TaskAnnotation> Annotations => this.data.Values;

        /// <summary>
        /// Gets the set of annotations in TimeIntervalAnnotation format.
        /// </summary>
        public IEnumerable<TimeIntervalAnnotation> ToTimeIntervalAnnotations => this.data.Values.Select(d => d as TimeIntervalAnnotation);

        /// <summary>
        /// Gets the time interval annotation for a specified track name.
        /// </summary>
        /// <param name="track">The track name.</param>
        /// <returns>The corresponding time interval annotation.</returns>
        public TaskAnnotation this[string track] => this.data[track];

        /// <summary>
        /// Adds a specified time interval annotation.
        /// </summary>
        /// <param name="taskAnnotation">The time interval annotation to add.</param>
        public void AddAnnotation(TaskAnnotation taskAnnotation)
        {
            if (taskAnnotation.Interval.Right != this.EndTime)
            {
                throw new ArgumentException("Cannot add a time interval annotation with a different end time to a time interval annotation set.");
            }

            this.data.Add(taskAnnotation.Track, taskAnnotation);
        }

        /// <summary>
        /// Removes an annotation specified by a track name.
        /// </summary>
        /// <param name="track">The track name for the annotation to remove.</param>
        public void RemoveAnnotation(string track)
        {
            if (this.data.Count() == 1)
            {
                throw new InvalidOperationException("Cannot remove the last time interval annotation from a time interval annotation set.");
            }

            this.data.Remove(track);
        }

        /// <summary>
        /// Gets a value indicating whether the annotation set contains an annotation for the specified track.
        /// </summary>
        /// <param name="track">The track name.</param>
        /// <returns>True if the annotation set contains an annotation for the specified track, otherwise false.</returns>
        public bool ContainsTrack(string track) => this.data.ContainsKey(track);

        /// <summary>
        /// Gets the set of annotations in TimeIntervalAnnotation format.
        /// </summary>
        /// <returns>The set of annotations in TimeIntervalAnnotation format.</returns>
        public TimeIntervalAnnotationSet ToTimeIntervalAnnotationSet()
        {
            TimeIntervalAnnotationSet result = null;
            foreach (var annotation in this.data.Values)
            {
                if (result == null)
                {
                    result = new TimeIntervalAnnotationSet(annotation as TimeIntervalAnnotation);
                    continue;
                }

                result.AddAnnotation(annotation as TimeIntervalAnnotation);
            }

            return result;
        }
    }
}
