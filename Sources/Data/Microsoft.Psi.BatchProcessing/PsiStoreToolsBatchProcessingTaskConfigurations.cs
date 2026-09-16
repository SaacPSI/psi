// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.BatchProcessing
{
    using Microsoft.Psi.Data;
    using System;
    using System.ComponentModel;
    using System.Runtime.Serialization;

#pragma warning disable SA1402 // File may only contain a single type
#pragma warning disable SA1649 // File name should match first type name

    /// <summary>
    /// Defines output formats for stream message export and transport.
    /// </summary>
    public enum StreamMessageFormat
    {
        /// <summary>
        /// Json format.
        /// </summary>
        Json,

        /// <summary>
        /// Csv format.
        /// </summary>
        Csv,

        /// <summary>
        /// MessagePack format.
        /// </summary>
        Msg,
    }

    /// <summary>
    /// Represents common configuration shared by PsiStore tools batch tasks.
    /// </summary>
    public class PsiStoreToolTaskConfiguration : BatchProcessingTaskConfiguration
    {
        private string partitionName = string.Empty;
        private bool runOnDatasetCopy = true;
        private string copyStoreName = string.Empty;
        private string copyStorePath = string.Empty;

        /// <summary>
        /// Gets or sets the source partition name.
        /// </summary>
        [DataMember]
        [DisplayName("Source Partition Name")]
        [Description("Optional source partition name. If empty, the first partition in the session is used.")]
        public string PartitionName
        {
            get => this.partitionName;
            set => this.Set(nameof(this.PartitionName), ref this.partitionName, value);
        }

        /// <summary>
        /// Gets or sets a value indicating whether to run on a copy of the input store.
        /// </summary>
        [DataMember]
        [DisplayName("Run On Dataset Copy")]
        [Description("When enabled, the input store is copied and the operation runs on that copy.")]
        public bool RunOnDatasetCopy
        {
            get => this.runOnDatasetCopy;
            set => this.Set(nameof(this.RunOnDatasetCopy), ref this.runOnDatasetCopy, value);
        }

        /// <summary>
        /// Gets or sets the copy store name.
        /// </summary>
        [DataMember]
        [DisplayName("Copy Store Name")]
        [Description("Optional copy store name. If empty, a default name is generated.")]
        public string CopyStoreName
        {
            get => this.copyStoreName;
            set => this.Set(nameof(this.CopyStoreName), ref this.copyStoreName, value);
        }

        /// <summary>
        /// Gets or sets the copy store path.
        /// </summary>
        [DataMember]
        [DisplayName("Copy Store Path")]
        [Description("Optional copy store path. If empty, the source store path is used.")]
        public string CopyStorePath
        {
            get => this.copyStorePath;
            set => this.Set(nameof(this.CopyStorePath), ref this.copyStorePath, value);
        }
    }

    /// <summary>
    /// Represents configuration for remove stream task.
    /// </summary>
    public class RemoveStreamTaskConfiguration : PsiStoreToolTaskConfiguration
    {
        private string streamName = string.Empty;

        /// <summary>
        /// Initializes a new instance of the <see cref="RemoveStreamTaskConfiguration"/> class.
        /// </summary>
        public RemoveStreamTaskConfiguration()
        {
            this.OutputStoreName = string.Empty;
            this.OutputPartitionName = string.Empty;
        }

        /// <summary>
        /// Gets or sets the stream name to remove.
        /// </summary>
        [DataMember]
        [DisplayName("Stream Name")]
        [Description("The name of the stream to remove from the target store.")]
        public string StreamName
        {
            get => this.streamName;
            set => this.Set(nameof(this.StreamName), ref this.streamName, value);
        }
    }

    /// <summary>
    /// Represents configuration for save stream messages task.
    /// </summary>
    public class SaveStreamMessagesTaskConfiguration : PsiStoreToolTaskConfiguration
    {
        private string streamName = string.Empty;
        private string outputFile = "messages.json";
        private StreamMessageFormat format = StreamMessageFormat.Json;

        /// <summary>
        /// Initializes a new instance of the <see cref="SaveStreamMessagesTaskConfiguration"/> class.
        /// </summary>
        public SaveStreamMessagesTaskConfiguration()
        {
            this.OutputStoreName = string.Empty;
            this.OutputPartitionName = string.Empty;
        }

        /// <summary>
        /// Gets or sets the stream name.
        /// </summary>
        [DataMember]
        [DisplayName("Stream Name")]
        [Description("The name of the stream to export.")]
        public string StreamName
        {
            get => this.streamName;
            set => this.Set(nameof(this.StreamName), ref this.streamName, value);
        }

        /// <summary>
        /// Gets or sets the output file.
        /// </summary>
        [DataMember]
        [DisplayName("Output File")]
        [Description("Path of the output file. Relative paths are resolved against the target store path.")]
        public string OutputFile
        {
            get => this.outputFile;
            set => this.Set(nameof(this.OutputFile), ref this.outputFile, value);
        }

        /// <summary>
        /// Gets or sets the output format.
        /// </summary>
        [DataMember]
        [DisplayName("Format")]
        [Description("Format used to persist messages.")]
        public StreamMessageFormat Format
        {
            get => this.format;
            set => this.Set(nameof(this.Format), ref this.format, value);
        }
    }

    /// <summary>
    /// Represents configuration for send stream messages task.
    /// </summary>
    public class SendStreamMessagesTaskConfiguration : PsiStoreToolTaskConfiguration
    {
        private string streamName = string.Empty;
        private string topic = "stream";
        private string address = "tcp://localhost:12345";
        private StreamMessageFormat format = StreamMessageFormat.Json;

        /// <summary>
        /// Initializes a new instance of the <see cref="SendStreamMessagesTaskConfiguration"/> class.
        /// </summary>
        public SendStreamMessagesTaskConfiguration()
        {
            this.OutputStoreName = string.Empty;
            this.OutputPartitionName = string.Empty;
        }

        /// <summary>
        /// Gets or sets the stream name.
        /// </summary>
        [DataMember]
        [DisplayName("Stream Name")]
        [Description("The name of the stream to send.")]
        public string StreamName
        {
            get => this.streamName;
            set => this.Set(nameof(this.StreamName), ref this.streamName, value);
        }

        /// <summary>
        /// Gets or sets the topic.
        /// </summary>
        [DataMember]
        [DisplayName("Topic")]
        [Description("Topic used for transport.")]
        public string Topic
        {
            get => this.topic;
            set => this.Set(nameof(this.Topic), ref this.topic, value);
        }

        /// <summary>
        /// Gets or sets the address.
        /// </summary>
        [DataMember]
        [DisplayName("Address")]
        [Description("Transport address, for example tcp://localhost:12345.")]
        public string Address
        {
            get => this.address;
            set => this.Set(nameof(this.Address), ref this.address, value);
        }

        /// <summary>
        /// Gets or sets the message format.
        /// </summary>
        [DataMember]
        [DisplayName("Format")]
        [Description("Format used to serialize sent messages.")]
        public StreamMessageFormat Format
        {
            get => this.format;
            set => this.Set(nameof(this.Format), ref this.format, value);
        }
    }

    /// <summary>
    /// Represents configuration for concatenate stores task.
    /// </summary>
    public class ConcatenateStoresTaskConfiguration : PsiStoreToolTaskConfiguration
    {
        private string storeNames = string.Empty;
        private string outputStoreName = "Concatenated";
        private string outputStorePath = string.Empty;

        /// <summary>
        /// Initializes a new instance of the <see cref="ConcatenateStoresTaskConfiguration"/> class.
        /// </summary>
        public ConcatenateStoresTaskConfiguration()
        {
            this.OutputStoreName = string.Empty;
            this.OutputPartitionName = string.Empty;
        }

        /// <summary>
        /// Gets or sets the semicolon-separated store names.
        /// </summary>
        [DataMember]
        [DisplayName("Store Names")]
        [Description("Semicolon-separated list of store names to concatenate.")]
        public string StoreNames
        {
            get => this.storeNames;
            set => this.Set(nameof(this.StoreNames), ref this.storeNames, value);
        }

        /// <summary>
        /// Gets or sets output store name.
        /// </summary>
        [DataMember]
        [DisplayName("Output Store Name")]
        [Description("Name of the generated store.")]
        public string GeneratedOutputStoreName
        {
            get => this.outputStoreName;
            set => this.Set(nameof(this.GeneratedOutputStoreName), ref this.outputStoreName, value);
        }

        /// <summary>
        /// Gets or sets output store path.
        /// </summary>
        [DataMember]
        [DisplayName("Output Store Path")]
        [Description("Path of the generated store.")]
        public string GeneratedOutputStorePath
        {
            get => this.outputStorePath;
            set => this.Set(nameof(this.GeneratedOutputStorePath), ref this.outputStorePath, value);
        }
    }

    /// <summary>
    /// Represents configuration for crop store task.
    /// </summary>
    public class CropStoreTaskConfiguration : PsiStoreToolTaskConfiguration
    {
        private TimeSpan start = TimeSpan.Zero;
        private TimeSpan length = TimeSpan.Zero;
        private bool useLength = false;
        private string outputStoreName = "Cropped";
        private string outputStorePath = string.Empty;

        /// <summary>
        /// Initializes a new instance of the <see cref="CropStoreTaskConfiguration"/> class.
        /// </summary>
        public CropStoreTaskConfiguration()
        {
            this.OutputStoreName = string.Empty;
            this.OutputPartitionName = string.Empty;
        }

        /// <summary>
        /// Gets or sets the crop start offset.
        /// </summary>
        [DataMember]
        [DisplayName("Start")]
        [Description("Crop start offset relative to beginning of store.")]
        public TimeSpan Start
        {
            get => this.start;
            set => this.Set(nameof(this.Start), ref this.start, value);
        }

        /// <summary>
        /// Gets or sets a value indicating whether to use a fixed crop length.
        /// </summary>
        [DataMember]
        [DisplayName("Use Length")]
        [Description("If false, crop continues until end of store.")]
        public bool UseLength
        {
            get => this.useLength;
            set => this.Set(nameof(this.UseLength), ref this.useLength, value);
        }

        /// <summary>
        /// Gets or sets the crop length.
        /// </summary>
        [DataMember]
        [DisplayName("Length")]
        [Description("Length of interval to keep, used when Use Length is true.")]
        public TimeSpan Length
        {
            get => this.length;
            set => this.Set(nameof(this.Length), ref this.length, value);
        }

        /// <summary>
        /// Gets or sets output store name.
        /// </summary>
        [DataMember]
        [DisplayName("Output Store Name")]
        [Description("Name of the cropped store.")]
        public string GeneratedOutputStoreName
        {
            get => this.outputStoreName;
            set => this.Set(nameof(this.GeneratedOutputStoreName), ref this.outputStoreName, value);
        }

        /// <summary>
        /// Gets or sets output store path.
        /// </summary>
        [DataMember]
        [DisplayName("Output Store Path")]
        [Description("Path of the cropped store.")]
        public string GeneratedOutputStorePath
        {
            get => this.outputStorePath;
            set => this.Set(nameof(this.GeneratedOutputStorePath), ref this.outputStorePath, value);
        }
    }

    /// <summary>
    /// Represents configuration for encode store task.
    /// </summary>
    public class EncodeStoreTaskConfiguration : PsiStoreToolTaskConfiguration
    {
        private int quality = 90;
        private string outputStoreName = "Encoded";
        private string outputStorePath = string.Empty;

        /// <summary>
        /// Initializes a new instance of the <see cref="EncodeStoreTaskConfiguration"/> class.
        /// </summary>
        public EncodeStoreTaskConfiguration()
        {
            this.OutputStoreName = string.Empty;
            this.OutputPartitionName = string.Empty;
        }

        /// <summary>
        /// Gets or sets JPEG quality.
        /// </summary>
        [DataMember]
        [DisplayName("Quality")]
        [Description("Jpeg quality from 0 to 100.")]
        public int Quality
        {
            get => this.quality;
            set => this.Set(nameof(this.Quality), ref this.quality, value);
        }

        /// <summary>
        /// Gets or sets output store name.
        /// </summary>
        [DataMember]
        [DisplayName("Output Store Name")]
        [Description("Name of the encoded store.")]
        public string GeneratedOutputStoreName
        {
            get => this.outputStoreName;
            set => this.Set(nameof(this.GeneratedOutputStoreName), ref this.outputStoreName, value);
        }

        /// <summary>
        /// Gets or sets output store path.
        /// </summary>
        [DataMember]
        [DisplayName("Output Store Path")]
        [Description("Path of the encoded store.")]
        public string GeneratedOutputStorePath
        {
            get => this.outputStorePath;
            set => this.Set(nameof(this.GeneratedOutputStorePath), ref this.outputStorePath, value);
        }
    }
}
