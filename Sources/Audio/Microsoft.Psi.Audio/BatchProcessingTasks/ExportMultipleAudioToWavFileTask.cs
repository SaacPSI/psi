// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Audio
{
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.IO;
    using System.Linq;
    using System.Runtime.Serialization;
    using Microsoft.Psi;
    using Microsoft.Psi.Data;

    /// <summary>
    /// Batch task that exports multiple audio streams to wav files.
    /// </summary>
    [BatchProcessingTask(
        "Export Multiple Audio to Wav File",
        Description = "This task exports multiple audio streams to wav files.",
        InputType = typeof(AudioBuffer))]
    public class ExportMultipleAudioToWavFileTask : BatchProcessingTask<ExportMultipleAudioToWavFileTaskConfiguration>
    {
        /// <inheritdoc/>
        public override void Run(Pipeline pipeline, SessionImporter sessionImporter, Exporter exporter, ExportMultipleAudioToWavFileTaskConfiguration configuration)
        {
            foreach (var streamName in configuration.StreamList)
            {
                var audio = sessionImporter.OpenStream<AudioBuffer>(streamName);
                var partition = sessionImporter.PartitionImporters.Values.First();
                var wavFileWriter = new WaveFileWriter(pipeline, Path.Combine(partition.StorePath, $"{streamName}.wav"));
                var streamlinedAudio = audio.Streamline(configuration.AudioStreamlineMethod, configuration.MaxOffsetBeforeUnpleatedRealignmentMs);
                if (exporter != null)
                {
                    streamlinedAudio.Write(streamName, exporter);
                }

                streamlinedAudio.PipeTo(wavFileWriter);
            }
        }
    }

    /// <summary>
    /// Represents the configuration for the <see cref="ExportMultipleAudioToWavFileTask"/>.
    /// </summary>
#pragma warning disable SA1402 // File may only contain a single type
    public class ExportMultipleAudioToWavFileTaskConfiguration : BatchProcessingTaskConfiguration
#pragma warning restore SA1402 // File may only contain a single type
    {
        private List<string> streamList = new List<string>();
        private AudioStreamlineMethod audioStreamlineMethod = AudioStreamlineMethod.Unpleat;
        private double maxOffsetBeforeUnpleatedRealignmentMs = 20;

        /// <summary>
        /// Initializes a new instance of the <see cref="ExportMultipleAudioToWavFileTaskConfiguration"/> class.
        /// </summary>
        public ExportMultipleAudioToWavFileTaskConfiguration()
            : base()
        {
            this.OutputStoreName = string.Empty;
            this.OutputPartitionName = string.Empty;
            this.DeliveryPolicySpec = DeliveryPolicySpec.Throttle;
            this.ReplayAllRealTime = false;
        }

        /// <summary>
        /// Gets or sets the list of the audio streams.
        /// </summary>
        [DataMember]
        [DisplayName("Audio Streams Name")]
        [Description("The name of the audio streams.")]
        public List<string> StreamList
        {
            get => this.streamList;
            set { this.Set(nameof(this.StreamList), ref this.streamList, value); }
        }

        /// <summary>
        /// Gets or sets the method used to streamline the audio stream.
        /// </summary>
        [DataMember]
        [DisplayName("Audio Streamline Method")]
        [Description("The method used to streamline the audio stream.")]
        public AudioStreamlineMethod AudioStreamlineMethod
        {
            get => this.audioStreamlineMethod;
            set { this.Set(nameof(this.AudioStreamlineMethod), ref this.audioStreamlineMethod, value); }
        }

        /// <summary>
        /// Gets or sets the maximum offset before realignment in milliseconds.
        /// </summary>
        [DataMember]
        [DisplayName("Max Offset Before Realignment (ms)")]
        [Description("The maximum time offset between the unpleated stream and originating times before a realignment is enforced (in milliseconds).")]
        public double MaxOffsetBeforeUnpleatedRealignmentMs
        {
            get => this.maxOffsetBeforeUnpleatedRealignmentMs;
            set { this.Set(nameof(this.MaxOffsetBeforeUnpleatedRealignmentMs), ref this.maxOffsetBeforeUnpleatedRealignmentMs, value); }
        }
    }
}