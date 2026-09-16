// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Data.Annotations
{
    using System.Collections.Generic;

    /// <summary>
    /// Represents a task annotation.
    /// </summary>
    public class TaskAnnotation : TimeIntervalAnnotation
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="TaskAnnotation"/> class.
        /// </summary>
        /// <param name="interval">The interval over which the annotation occurs.</param>
        /// <param name="track">The name of the annotation track.</param>
        /// <param name="task">The task name of the annotation.</param>
        /// <param name="attributeValues">The set of attribute values for the annotation.</param>
        /// <param name="file">The file of the annotation.</param>
        public TaskAnnotation(TimeInterval interval, string track, string task, Dictionary<string, IAnnotationValue> attributeValues, string file = "")
            : base(interval, track, attributeValues)
        {
            this.Task = task;
            this.File = file;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="TaskAnnotation"/> class.
        /// </summary>
        /// <param name="interval">The interval annotation to create task annotations from.</param>
        /// <param name="task">The task name of the annotation.</param>
        /// <param name="file">The file of the annotation.</param>
        public TaskAnnotation(TimeIntervalAnnotation interval, string task, string file = "")
            : base(interval.Interval, interval.Track, interval.AttributeValues)
        {
            this.Task = task;
            this.File = file;
        }

        /// <summary>
        /// Gets or sets the task name of the this annotation.
        /// </summary>
        public string Task { get; set; }

        /// <summary>
        /// Gets or sets the file of the this annotation.
        /// </summary>
        public string File { get; set; }
    }
}
