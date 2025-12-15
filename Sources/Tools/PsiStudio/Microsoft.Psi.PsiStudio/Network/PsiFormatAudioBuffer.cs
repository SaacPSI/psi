// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.PsiStudio.Format
{
    using System.IO;
    using Microsoft.Psi.Interop.Serialization;

    /// <summary>
    /// Class for (de)serializing audio buffer for TcpWriter/Source.
    /// </summary>
    public class PsiFormatAudioBuffer
    {
        /// <summary>
        /// (De)Serialize audio buffer.
        /// </summary>
        /// <returns>The Fromat corresponding.</returns>
        public static Format<Microsoft.Psi.Audio.AudioBuffer> GetFormat()
        {
            return new Format<Microsoft.Psi.Audio.AudioBuffer>(WriteAudioBuffer, ReadAudioBuffer);
        }

        /// <summary>
        /// Serialize an audio buffer.
        /// </summary>
        /// <param name="buffer">The audio buffer to serialize.</param>
        /// <param name="writer">The binary writers.</param>
        public static void WriteAudioBuffer(Microsoft.Psi.Audio.AudioBuffer buffer, BinaryWriter writer)
        {
            writer.Write(buffer.Format.Channels);
            writer.Write(buffer.Format.SamplesPerSec);
            writer.Write(buffer.Format.BitsPerSample);
            writer.Write(buffer.Length);
            writer.Write(buffer.Data);
        }

        /// <summary>
        /// Deserialize and create an audio buffer.
        /// </summary>
        /// <param name="reader">The binary writers.</param>
        /// <returns>The AudioBuffer object.</returns>
        public static Microsoft.Psi.Audio.AudioBuffer ReadAudioBuffer(BinaryReader reader)
        {
            int channels = reader.ReadInt32();
            int samplesPerSec = reader.ReadInt32();
            int bitsPerSample = reader.ReadInt32();
            int length = reader.ReadInt32();
            byte[] data = reader.ReadBytes(length);
            return new Microsoft.Psi.Audio.AudioBuffer(data, Audio.WaveFormat.CreatePcm(samplesPerSec, bitsPerSample, channels));
        }
    }
}