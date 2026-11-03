// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Data.Annotations
{
    using System.Collections.Generic;
    using System.Drawing;

    /// <summary>
    /// Represents a task annotation.
    /// </summary>
    public class TaskAnnotation : TimeIntervalAnnotation
    {
        /// <summary>
        /// Task name keys for attributeValues dictionnary.
        /// </summary>
        public const string TaskAttributeName = "Task";

        /// <summary>
        /// Task parameters keys for attributeValues dictionnary.
        /// </summary>
        public const string TaskParametersAttributeName = "TaskParameters";

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
            if (this.Task == string.Empty)
            {
                if (this.AttributeValues.TryGetValue(TaskAttributeName, out IAnnotationValue taskname))
                {
                    this.Task = taskname.ValueAsString;
                }
                else
                {
                    throw new System.Exception("Creating TaskAnnotation from TimeIntervalAnnotation without the correct attributeValue.");
                }
            }
        }

        /// <summary>
        /// Gets or sets the task name of the this annotation.
        /// </summary>
        public string Task { get; set; }

        /// <summary>
        /// Gets or sets the file of the this annotation.
        /// </summary>
        public string File { get; set; }

        /// <summary>
        /// Gets the task parameters of the this annotation.
        /// </summary>
        /// <returns>The task parameters of the this annotation.</returns>
        public string GetParameters()
        {
            if (this.AttributeValues.TryGetValue(TaskParametersAttributeName, out IAnnotationValue parameters))
            {
                return parameters.ValueAsString;
            }

            return string.Empty;
        }

        /// <summary>
        /// Sets the task parameters of the this annotation.
        /// </summary>
        /// <param name="parameters">The task parameters to set.</param>
        /// <param name="fillColor">The fill color of the parameters.</param>
        /// <param name="textColor">The text color of the parameters.</param>
        /// <returns>True if the parameters were set successfully, false otherwise.</returns>
        public bool SetParameters(string parameters, Color fillColor, Color textColor)
        {
            if (this.AttributeValues.ContainsKey(TaskParametersAttributeName))
            {
                this.AttributeValues[TaskParametersAttributeName] = new AnnotationValue<string>(parameters, fillColor, textColor);
                return true;
            }

            return false;
        }
    }
}
