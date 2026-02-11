// Licensed under the CeCILL-C License. See LICENSE.md file in the project root for full license information.
// This software is distributed under the CeCILL-C FREE SOFTWARE LICENSE AGREEMENT.
// See https://cecill.info/licences/Licence_CeCILL-C_V1-en.html for details.

// From https://github.com/sipsorcery-org/SIPSorceryMedia.SDL2/
namespace SAAC.WebRTC.OpusCodec
{
    using Concentus.Enums;
    using Concentus.Structs;
    using Microsoft.Extensions.Logging;
    using SIPSorcery.Media;
    using SIPSorceryMedia.Abstractions;

#pragma warning disable SA1310 // Field names should not contain underscore
#pragma warning disable SA1203 // ConstantsMustAppearBeforeFields

    /// <summary>
    /// Opus audio encoder/decoder implementation based on Concentus.
    /// </summary>
    /// <remarks>
    /// Based on this discussion: https://github.com/sipsorcery-org/sipsorcery/issues/518#issuecomment-888639894.
    /// </remarks>
    public class OpusAudioEncoder : IAudioEncoder
    {
        /// <summary>
        /// Chrome uses two audio channels in SDP, but audio contains only one channel.
        /// </summary>
        public static readonly AudioFormat MEDIA_FORMAT_OPUS = new AudioFormat(111, "opus", SAMPLE_RATE, SAMPLE_RATE, 2, "a=fmtp:111 minptime=10;useinbandfec=1");

        /// <summary>
        /// The maximum frame size.
        /// </summary>
        public const int MAX_FRAME_SIZE = MAX_DECODED_FRAME_SIZE_MULT * 960;

        private const int FRAME_SIZE_MILLISECONDS = 20;
        private const int MAX_DECODED_FRAME_SIZE_MULT = 6;
        private const int MAX_PACKET_SIZE = 4000;
        private const int SAMPLE_RATE = 48000;

        private ILogger log;
        private AudioEncoder audioEncoder;
        private List<AudioFormat> supportedFormats;
        private int channels = 1;
        private short[] shortBuffer;
        private byte[] byteBuffer;
        private OpusEncoder opusEncoder;
        private OpusDecoder opusDecoder;

        /// <summary>
        /// Initializes a new instance of the <see cref="OpusAudioEncoder"/> class.
        /// </summary>
        /// <param name="logger">Optional logger instance.</param>
        public OpusAudioEncoder(ILogger? logger = null)
        {
            this.log = logger ?? SIPSorcery.LogFactory.CreateLogger<OpusAudioEncoder>();

            this.audioEncoder = new AudioEncoder();

            // Add OPUS in the list of AudioFormat
            this.supportedFormats = new List<AudioFormat> { MEDIA_FORMAT_OPUS };

            // Add also list available in the AudioEncoder available in SIPSorcery
            this.supportedFormats.AddRange(this.audioEncoder.SupportedFormats);
        }

        /// <inheritdoc/>
        public List<AudioFormat> SupportedFormats => this.supportedFormats;

        /// <inheritdoc/>
        public short[] DecodeAudio(byte[] encodedSample, AudioFormat format)
        {
            if (format.FormatName == "opus")
            {
                if (this.opusDecoder == null)
                {
                    this.opusDecoder = OpusDecoder.Create(SAMPLE_RATE, this.channels);
                    this.shortBuffer = new short[MAX_FRAME_SIZE * this.channels];
                }

                try
                {
                    int numSamplesDecoded = this.opusDecoder.Decode(encodedSample, 0, encodedSample.Length, this.shortBuffer, 0, this.shortBuffer.Length, false);

                    if (numSamplesDecoded >= 1)
                    {
                        var buffer = new short[numSamplesDecoded];
                        Array.Copy(this.shortBuffer, 0, buffer, 0, numSamplesDecoded);
                        this.log.LogTrace($"OpusAudioEncoder -> DecodeAudio : DecodedShort:[{numSamplesDecoded}] - EncodedByte.Length:[{encodedSample.Length}]");
                        return buffer;
                    }
                }
                catch (Exception ex)
                {
                    this.log.LogError($"OpusAudioEncoder -> DecodeAudio : Exception - {ex.Message}");
                }

                return new short[0];
            }
            else
            {
                return this.audioEncoder.DecodeAudio(encodedSample, format);
            }
        }

        /// <summary>
        /// Encode the audio in the given format class.
        /// </summary>
        /// <param name="pcm">Audio data.</param>
        /// <param name="format">Audio metadata.</param>
        /// <returns>Encoded audio data.</returns>
        public byte[] EncodeAudio(float[] pcm, AudioFormat format)
        {
            if (format.FormatName == "opus")
            {
                if (this.opusEncoder == null)
                {
                    this.opusEncoder = OpusEncoder.Create(SAMPLE_RATE, this.channels, OpusApplication.OPUS_APPLICATION_RESTRICTED_LOWDELAY);
                    this.opusEncoder.ForceMode = OpusMode.MODE_AUTO;
                    this.byteBuffer = new byte[MAX_PACKET_SIZE];
                }

                try
                {
                    int frameSize = this.GetFrameSize();
                    int size = this.opusEncoder.Encode(pcm, 0, frameSize, this.byteBuffer, 0, this.byteBuffer.Length);

                    if (size > 1)
                    {
                        byte[] result = new byte[size];
                        Array.Copy(this.byteBuffer, 0, result, 0, size);

                        this.log.LogTrace($"OpusAudioEncoder -> EncodeAudio : frameSize:[{frameSize}] - DecodedFloat:[{pcm.Length}] - EncodedByte.Length:[{result.Length}]");
                        return result;
                    }
                }
                catch (Exception ex)
                {
                    this.log.LogError($"OpusAudioEncoder -> EncodeAudio : Exception - {ex.Message}");
                }

                return new byte[0];
            }
            else
            {
                return this.EncodeAudio(pcm, format);
            }
        }

        /// <inheritdoc/>
        public byte[] EncodeAudio(short[] pcm, AudioFormat format)
        {
            if (format.FormatName == "opus")
            {
                if (this.opusEncoder == null)
                {
                    this.opusEncoder = OpusEncoder.Create(SAMPLE_RATE, this.channels, OpusApplication.OPUS_APPLICATION_RESTRICTED_LOWDELAY);
                    this.opusEncoder.ForceMode = OpusMode.MODE_AUTO;
                    this.byteBuffer = new byte[MAX_PACKET_SIZE];
                }

                try
                {
                    int frameSize = this.GetFrameSize();
                    int size = this.opusEncoder.Encode(pcm, 0, frameSize, this.byteBuffer, 0, this.byteBuffer.Length);

                    if (size > 1)
                    {
                        byte[] result = new byte[size];
                        Array.Copy(this.byteBuffer, 0, result, 0, size);

                        this.log.LogTrace($"OpusAudioEncoder -> EncodeAudio : frameSize:[{frameSize}] - DecodedShort:[{pcm.Length}] - EncodedByte.Length:[{result.Length}]");
                        return result;
                    }
                }
                catch (Exception ex)
                {
                    this.log.LogError($"OpusAudioEncoder -> EncodeAudio : Exception - {ex.Message}");
                }

                return new byte[0];
            }
            else
            {
                return this.audioEncoder.EncodeAudio(pcm, format);
            }
        }

        /// <summary>
        /// Gets the frame size for encoding.
        /// </summary>
        /// <returns>The frame size in samples.</returns>
        public int GetFrameSize()
        {
            return 960;
        }
    }

#pragma warning restore SA1203 // ConstantsMustAppearBeforeFields
#pragma warning restore SA1310 // Field names should not contain underscore
}
