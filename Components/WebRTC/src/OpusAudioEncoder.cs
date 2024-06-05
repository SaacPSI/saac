using SIPSorcery.Media;
using SIPSorceryMedia.Abstractions;
using Concentus.Structs;
using Concentus.Enums;
using Microsoft.Extensions.Logging;

// From https://github.com/sipsorcery-org/SIPSorceryMedia.SDL2/
namespace SAAC.WebRTC
{
    namespace OpusCodec
    {
        // Based on this discussion: https://github.com/sipsorcery-org/sipsorcery/issues/518#issuecomment-888639894
        public class OpusAudioEncoder : IAudioEncoder
        {
            //Chrome use in SDP two audio channels, but the audio inself contains only one channel, so we must pass it as 2 channels in SDP but create a decoder/encoder with only one channel
            public static readonly AudioFormat MEDIA_FORMAT_OPUS = new AudioFormat(111, "opus", SAMPLE_RATE, SAMPLE_RATE, 2, "a=fmtp:111 minptime=10;useinbandfec=1");


            private ILogger log = SIPSorcery.LogFactory.CreateLogger<OpusAudioEncoder>();

            private AudioEncoder audioEncoder; // The AudioEncoder available in SIPSorcery
            private List<AudioFormat> supportedFormats;

            private const int FRAME_SIZE_MILLISECONDS = 20;
            private const int MAX_DECODED_FRAME_SIZE_MULT = 6;
            private const int MAX_PACKET_SIZE = 4000;
            public const int MAX_FRAME_SIZE = MAX_DECODED_FRAME_SIZE_MULT * 960;
            private const int SAMPLE_RATE = 48000;

            private int channels = 1;
            private short[] shortBuffer;
            private byte[] byteBuffer;

            private OpusEncoder opusEncoder;
            private OpusDecoder opusDecoder;

            public OpusAudioEncoder(ILogger logger = null)
            {
                log = logger ?? SIPSorcery.LogFactory.CreateLogger<OpusAudioEncoder>();

                audioEncoder = new AudioEncoder();

                // Add OPUS in the list of AudioFormat
                supportedFormats = new List<AudioFormat> { MEDIA_FORMAT_OPUS };

                // Add also list available in the AudioEncoder available in SIPSorcery
                supportedFormats.AddRange(audioEncoder.SupportedFormats);
            }

            public List<AudioFormat> SupportedFormats => supportedFormats;

            public short[] DecodeAudio(byte[] encodedSample, AudioFormat format)
            {
                if (format.FormatName == "opus")
                {
                    if (opusDecoder == null)
                    {
                        opusDecoder = OpusDecoder.Create(SAMPLE_RATE, channels);
                        shortBuffer = new short[MAX_FRAME_SIZE * channels];
                    }

                    try
                    {
                        int numSamplesDecoded = opusDecoder.Decode(encodedSample, 0, encodedSample.Length, shortBuffer, 0, shortBuffer.Length, false);

                        if (numSamplesDecoded >= 1)
                        {
                            var buffer = new short[numSamplesDecoded];
                            Array.Copy(shortBuffer, 0, buffer, 0, numSamplesDecoded);
                            log.LogTrace($"OpusAudioEncoder -> DecodeAudio : DecodedShort:[{numSamplesDecoded}] - EncodedByte.Length:[{encodedSample.Length}]");
                            return buffer;
                        }
                    }
                    catch (Exception ex)
                    {
                        log.LogError($"OpusAudioEncoder -> DecodeAudio : Exception - {ex.Message}");
                    }
                    return new short[0];
                }
                else
                    return audioEncoder.DecodeAudio(encodedSample, format);
            }

            public byte[] EncodeAudio(float[] pcm, AudioFormat format)
            {
                if (format.FormatName == "opus")
                {
                    if (opusEncoder == null)
                    {
                        opusEncoder = OpusEncoder.Create(SAMPLE_RATE, channels, OpusApplication.OPUS_APPLICATION_RESTRICTED_LOWDELAY);
                        opusEncoder.ForceMode = OpusMode.MODE_AUTO;
                        byteBuffer = new byte[MAX_PACKET_SIZE];
                    }

                    try
                    {
                        int frameSize = GetFrameSize();
                        int size = opusEncoder.Encode(pcm, 0, frameSize, byteBuffer, 0, byteBuffer.Length);

                        if (size > 1)
                        {
                            byte[] result = new byte[size];
                            Array.Copy(byteBuffer, 0, result, 0, size);

                            log.LogTrace($"OpusAudioEncoder -> EncodeAudio : frameSize:[{frameSize}] - DecodedFloat:[{pcm.Length}] - EncodedByte.Length:[{result.Length}]");
                            return result;
                        }
                    }
                    catch (Exception ex)
                    {
                        log.LogError($"OpusAudioEncoder -> EncodeAudio : Exception - {ex.Message}");
                    }
                    return new byte[0];
                }
                else
                    return EncodeAudio(pcm, format);
            }

            public byte[] EncodeAudio(short[] pcm, AudioFormat format)
            {
                if (format.FormatName == "opus")
                {
                    if (opusEncoder == null)
                    {
                        opusEncoder = OpusEncoder.Create(SAMPLE_RATE, channels, OpusApplication.OPUS_APPLICATION_RESTRICTED_LOWDELAY);
                        opusEncoder.ForceMode = OpusMode.MODE_AUTO;
                        byteBuffer = new byte[MAX_PACKET_SIZE];
                    }

                    try
                    {
                        int frameSize = GetFrameSize();
                        int size = opusEncoder.Encode(pcm, 0, frameSize, byteBuffer, 0, byteBuffer.Length);

                        if (size > 1)
                        {
                            byte[] result = new byte[size];
                            Array.Copy(byteBuffer, 0, result, 0, size);

                            log.LogTrace($"OpusAudioEncoder -> EncodeAudio : frameSize:[{frameSize}] - DecodedShort:[{pcm.Length}] - EncodedByte.Length:[{result.Length}]");
                            return result;
                        }
                    }
                    catch (Exception ex)
                    {
                        log.LogError($"OpusAudioEncoder -> EncodeAudio : Exception - {ex.Message}");
                    }
                    return new byte[0];
                }
                else
                    return audioEncoder.EncodeAudio(pcm, format);
            }

            public int GetFrameSize()
            {
                return 960;
                //return (int)(SAMPLE_RATE * FRAME_SIZE_MILLISECONDS / 1000);
            }
        }
    }
}