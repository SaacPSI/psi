// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Data
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;

    /// <summary>
    /// Represents metadata about a dynamically loaded batch processing task
    /// and provides functionality for configuring and executing the task.
    /// </summary>
    public class BatchProcessingTaskMetadata
    {
        private readonly BatchProcessingTaskAttribute batchProcessingTaskAttribute = null;
        private readonly Type batchProcessingTaskType;
        private readonly string batchProcessingTaskConfigurationsPath;

        /// <summary>
        /// Initializes a new instance of the <see cref="BatchProcessingTaskMetadata"/> class.
        /// </summary>
        /// <param name="batchProcessingTaskType">The batch processing task type.</param>
        /// <param name="batchProcessingTaskAttribute">The batch processing task attribute.</param>
        /// <param name="batchProcessingTaskConfigurationsPath">The folder in which batch processing task configurations are saved.</param>
        public BatchProcessingTaskMetadata(Type batchProcessingTaskType, BatchProcessingTaskAttribute batchProcessingTaskAttribute, string batchProcessingTaskConfigurationsPath)
        {
            this.batchProcessingTaskType = batchProcessingTaskType;
            this.batchProcessingTaskAttribute = batchProcessingTaskAttribute;
            this.batchProcessingTaskConfigurationsPath = Path.Combine(batchProcessingTaskConfigurationsPath, batchProcessingTaskAttribute.Name);
        }

        /// <summary>
        /// Gets the batch processing task name.
        /// </summary>
        public string Name => this.batchProcessingTaskAttribute.Name;

        /// <summary>
        /// Gets the batch processing task description.
        /// </summary>
        public string Description => this.batchProcessingTaskAttribute.Description;

        /// <summary>
        /// Gets the batch processing task icon source path.
        /// </summary>
        public string IconSourcePath => this.batchProcessingTaskAttribute.IconSourcePath;

        /// <summary>
        /// Gets the type of the input, null if any.
        /// </summary>
        public Type InputType => this.batchProcessingTaskAttribute.InputType;

        /// <summary>
        /// Gets the folder under which configurations for this batch processing task should be stored.
        /// </summary>
        public string ConfigurationsPath => this.batchProcessingTaskConfigurationsPath;

        /// <summary>
        /// Gets the namespace for the batch processing task type.
        /// </summary>
        public string Namespace => this.batchProcessingTaskType.Namespace;

        /// <summary>
        /// Gets or sets the name of the most recently used configuration.
        /// </summary>
        public string MostRecentlyUsedConfiguration { get; set; }

        /// <summary>
        /// Gets a value indicating whether the batch processing task is compatible with the specified input type.
        /// </summary>
        /// <param name="streamDataType">The type of the input stream.</param>
        /// <returns>true if the batch processing task is compatible with the specified input type; otherwise, false.</returns>
        public bool IsCompatibleWith(Type streamDataType)
            => this.InputType != null && this.InputType.IsAssignableFrom(streamDataType);

        /// <summary>
        /// Gets the default configuration for the batch processing task.
        /// </summary>
        /// <returns>The default configuration.</returns>
        public BatchProcessingTaskConfiguration GetDefaultConfiguration()
        {
            var batchProcessingTask = Activator.CreateInstance(this.batchProcessingTaskType) as IBatchProcessingTask;
            return batchProcessingTask.GetDefaultConfiguration();
        }

        /// <summary>
        /// Gets the default configuration for the batch processing task.
        /// </summary>
        /// <param name="attributes">Dictionnary of attributes to set in the configuration.</param>
        /// <returns>The default configuration.</returns>
        public BatchProcessingTaskConfiguration GetDefaultConfigurationWithAttributes(Dictionary<string, object> attributes)
        {
            var batchProcessingTask = Activator.CreateInstance(this.batchProcessingTaskType) as IBatchProcessingTask;
            return batchProcessingTask.GetConfiguration(attributes);
        }

        /// <summary>
        /// Creates a corresponding batch processing task instance.
        /// </summary>
        /// <returns>The batch processing task instance.</returns>
        public IBatchProcessingTask CreateBatchProcessingTask()
            => Activator.CreateInstance(this.batchProcessingTaskType) as IBatchProcessingTask;
    }
}
