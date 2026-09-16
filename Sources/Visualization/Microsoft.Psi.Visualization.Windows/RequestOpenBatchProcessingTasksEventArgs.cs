// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Visualization
{
    using System;
    using Microsoft.Psi.Data.Annotations;

    /// <summary>
    /// Represents event arguments for requesting the batch processing tasks picker.
    /// </summary>
    public class RequestOpenBatchProcessingTasksEventArgs : EventArgs
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="RequestOpenBatchProcessingTasksEventArgs"/> class.
        /// </summary>
        /// <param name="taskAnnotation">The task annotation requested from the annotation context.</param>
        /// <param name="saveConfigurationToAnnotation">Action used to save serialized configuration JSON to the target annotation.</param>
        public RequestOpenBatchProcessingTasksEventArgs(TaskAnnotation taskAnnotation, Action<string> saveConfigurationToAnnotation)
        {
            this.TaskAnnotation = taskAnnotation;
            this.SaveConfigurationToAnnotation = saveConfigurationToAnnotation;
        }

        /// <summary>
        /// Gets the task annotation requested from the annotation context.
        /// </summary>
        public TaskAnnotation TaskAnnotation { get; }

        /// <summary>
        /// Gets the action used to save serialized configuration JSON to the target annotation.
        /// </summary>
        public Action<string> SaveConfigurationToAnnotation { get; }
    }
}
