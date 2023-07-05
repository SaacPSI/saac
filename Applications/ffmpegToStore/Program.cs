using System.Runtime.InteropServices;
using System.Windows.Navigation;
using FFmpeg.AutoGen;
using Microsoft.Psi;
using Microsoft.Psi.Audio;
using Microsoft.Psi.Data;
using Microsoft.Psi.Imaging;

class ffmpegToStore
{
    private Pipeline pipeline;
    private Emitter<Shared<Image>> videoEmitter;
    private Emitter<AudioBuffer>? audioEmitter;

    public ffmpegToStore(string ffmpegPath, string file, DateTime startTime, string videoStoreName, string storePath, int encodingLevel, string? audioStoreName)
    {
        pipeline = Pipeline.Create();
        videoEmitter = pipeline.CreateEmitter<Shared<Image>>(pipeline, "image");
        PsiExporter videoStore = PsiStore.Create(pipeline, videoStoreName, storePath);
        videoStore.Write(videoEmitter.EncodeJpeg(encodingLevel), "video");
        if (audioStoreName != null)
        {
            PsiExporter audioStore = PsiStore.Create(pipeline, audioStoreName, storePath);
            audioEmitter = pipeline.CreateEmitter<AudioBuffer>(pipeline, "audio");
            audioStore.Write(audioEmitter, "audio");
        }
        else
            audioEmitter = null;
        pipeline.RunAsync();
        ExtractFrames(ffmpegPath, file, startTime);
        pipeline.Dispose();
    }

    unsafe int InitialiseAudio(AVFormatContext* formatContext, AVCodecParameters** codecParameters, AVCodec** audioCodec, AVCodecContext** audioCodecContext, SwrContext** audioSwrctx)
    {
        if (audioEmitter == null)
            return -1;
        int audioStreamIndex = ffmpeg.av_find_best_stream(formatContext, AVMediaType.AVMEDIA_TYPE_AUDIO, -1, -1, audioCodec, 0);
        if (audioStreamIndex < 0)
            return audioStreamIndex;
      
        *audioCodecContext = ffmpeg.avcodec_alloc_context3(*audioCodec);
        if (audioCodecContext == null)
        {
            Console.WriteLine("Failed to initialize audio codec.");
            ffmpeg.avcodec_close(*audioCodecContext);
            ffmpeg.avcodec_free_context(audioCodecContext);
            return -1;
        }

        *codecParameters = formatContext->streams[audioStreamIndex]->codecpar;

        if (ffmpeg.avcodec_parameters_to_context(*audioCodecContext, *codecParameters) < 0)
        {
            Console.WriteLine("Failed to set parameters in audio codec.");
            ffmpeg.avcodec_close(*audioCodecContext);
            ffmpeg.avcodec_free_context(audioCodecContext);
            return -1;
        }

        if (ffmpeg.avcodec_open2(*audioCodecContext, *audioCodec, null) < 0)
        {
            Console.WriteLine("Failed to open audio codec.");
            ffmpeg.avcodec_close(*audioCodecContext);
            ffmpeg.avcodec_free_context(audioCodecContext);
            return -1;
        }

        *audioSwrctx = ffmpeg.swr_alloc_set_opts(*audioSwrctx, ffmpeg.av_get_default_channel_layout((*audioCodecContext)->channels), AVSampleFormat.AV_SAMPLE_FMT_S16, (*audioCodecContext)->sample_rate, ffmpeg.av_get_default_channel_layout((*audioCodecContext)->channels), (*audioCodecContext)->sample_fmt, (*audioCodecContext)->sample_rate, 0, null);
        if (*audioSwrctx == null)
        {
            Console.WriteLine("Failed to create audio converter.");
            ffmpeg.avcodec_close(*audioCodecContext);
            ffmpeg.avcodec_free_context(audioCodecContext);
            return -1;
        }

        if(ffmpeg.swr_init(*audioSwrctx) < 0)
        {
            Console.WriteLine("Failed to initialise audio converter.");
            ffmpeg.avcodec_close(*audioCodecContext);
            ffmpeg.avcodec_free_context(audioCodecContext);
            ffmpeg.swr_close(*audioSwrctx);
            return -1;
        }

        return audioStreamIndex;
    }

    unsafe void ExtractFrames(string ffmpegPath, string videoPath, DateTime startTime)
    {
        ffmpeg.RootPath = ffmpegPath;
        ffmpeg.avdevice_register_all();
        ffmpeg.av_log_set_level(ffmpeg.AV_LOG_INFO);
        av_log_set_callback_callback value = (p0, level, format, vl) =>
        {
            if (level <= ffmpeg.av_log_get_level())
            {
                int num = 1024;
                byte* ptr = stackalloc byte[(int)(uint)num];
                int num2 = 1;
                ffmpeg.av_log_format_line(p0, level, format, vl, ptr, num, &num2);
                string text = Marshal.PtrToStringAnsi((IntPtr)ptr);
                Console.WriteLine(text);
            }
        };
        ffmpeg.av_log_set_callback(value);

        AVInputFormat* formatInputFormat = ffmpeg.av_find_input_format(videoPath.Substring(videoPath.LastIndexOf(".")+1));

        AVFormatContext* formatContext = null;
        if (ffmpeg.avformat_open_input(&formatContext, videoPath, formatInputFormat, null) != 0)
        {
            Console.WriteLine($"Error while opening the video : {videoPath}");
            return;
        }

        /// VIDEO
        AVCodec* videoCodec = null;
        int videoStreamIndex = ffmpeg.av_find_best_stream(formatContext, AVMediaType.AVMEDIA_TYPE_VIDEO, -1, -1, &videoCodec, 0);
        if (videoStreamIndex < 0)
        {
            Console.WriteLine("Video stream not found in file.");
            ffmpeg.avformat_close_input(&formatContext);
            return;
        }
       
        if (ffmpeg.avformat_find_stream_info(formatContext, null) < 0)
        {
            Console.WriteLine("Stream info not found.");
            ffmpeg.avformat_close_input(&formatContext);
            return;
        }

        AVCodecParameters* videoCodecParameters = formatContext->streams[videoStreamIndex]->codecpar;
        var pixelFormat = (AVPixelFormat)formatContext->streams[videoStreamIndex]->codecpar->format;
    
        if (videoCodec == null)
        {
            Console.WriteLine("Codec not found.");
            ffmpeg.avformat_close_input(&formatContext);
            return;
        }

        AVCodecContext* videoCodecContext = null;
        videoCodecContext = ffmpeg.avcodec_alloc_context3(videoCodec);
        if (videoCodecContext == null)
        {
            Console.WriteLine("Failed to initialize codec.");
            ffmpeg.avformat_close_input(&formatContext);
            return;
        }

        if (ffmpeg.avcodec_parameters_to_context(videoCodecContext, videoCodecParameters) < 0)
        {
            Console.WriteLine("Failed to set parameters in codec.");
            ffmpeg.avcodec_free_context(&videoCodecContext);
            ffmpeg.avformat_close_input(&formatContext);
            return;
        }

        if (ffmpeg.avcodec_open2(videoCodecContext, videoCodec, null) < 0)
        {
            Console.WriteLine("Failed to open codec.");
            ffmpeg.avcodec_free_context(&videoCodecContext);
            ffmpeg.avformat_close_input(&formatContext);
            return;
        }

        SwsContext* videoSwsctx = ffmpeg.sws_getContext(videoCodecParameters->width, videoCodecParameters->height, pixelFormat, videoCodecParameters->width, videoCodecParameters->height, AVPixelFormat.AV_PIX_FMT_BGR24, 0, null, null, null);
        if (videoSwsctx == null)
        {
            Console.WriteLine("Failed to open SwsContext.");
            return;
        }

        int dataSize = videoCodecParameters->width * videoCodecParameters->height * 3;
        byte[] bgrData = new byte[dataSize];
        byte_ptrArray4 dstData = new byte_ptrArray4();
        int[] dstLinesize = new int[4];
        dstLinesize[0] = videoCodecParameters->width * 3;

        /// AUDIO
        AVCodec* audioCodec = null;
        AVCodecContext* audioCodecContext = null;
        SwrContext* audioSwrctx = null;
        int audioStreamIndex = -1;

        double timeRatio = 1.0;
        AVCodecParameters* audioCodecParameters;
        if (audioEmitter != null)
        {
            audioStreamIndex = InitialiseAudio(formatContext, &audioCodecParameters, &audioCodec, &audioCodecContext, &audioSwrctx);
            var stream = formatContext->streams[audioStreamIndex];
            timeRatio = stream->time_base.num / (double)stream->time_base.den;
        }

        /// PROCESSING
        AVPacket* packet = ffmpeg.av_packet_alloc();
        while (ffmpeg.av_read_frame(formatContext, packet) >= 0)
        {
            if (packet->stream_index == videoStreamIndex)
            {
                ffmpeg.avcodec_send_packet(videoCodecContext, packet);
                AVFrame* frame = ffmpeg.av_frame_alloc();
                Shared<Image> image = ImagePool.GetOrCreate(videoCodecParameters->width, videoCodecParameters->height, PixelFormat.BGR_24bpp);
                while (ffmpeg.avcodec_receive_frame(videoCodecContext, frame) == 0)
                {
                    fixed (byte* bgrPtr = bgrData)
                    {
                        dstData[0] = bgrPtr;
                        if (ffmpeg.sws_scale(videoSwsctx, frame->data, frame->linesize, 0, frame->height, dstData, dstLinesize) < 0)
                            continue;
                    }
                    image.Resource.CopyFrom(bgrData);
                    DateTime date = startTime.AddSeconds(frame->coded_picture_number / (double)formatContext->streams[videoStreamIndex]->avg_frame_rate.num);
                    videoEmitter.Post(image, date);
                }
                ffmpeg.av_frame_free(&frame);
            }
            else if (packet->stream_index == audioStreamIndex)
            {
                ffmpeg.avcodec_send_packet(audioCodecContext, packet);
                AVFrame* frame = ffmpeg.av_frame_alloc();

                while (ffmpeg.avcodec_receive_frame(audioCodecContext, frame) == 0)
                {
                    var convertedSamples = new byte[frame->nb_samples * 2 * audioCodecContext->channels];
                    fixed (byte* convertedSamplesPtr = convertedSamples)
                    {
                        byte** convertedSamplesData = &convertedSamplesPtr;
                        int convertedSamplesCount = ffmpeg.swr_convert(audioSwrctx, convertedSamplesData, frame->nb_samples, frame->extended_data, frame->nb_samples);

                        DateTime dateA = startTime.AddSeconds(frame->pts * timeRatio);
                        WaveFormat wave = WaveFormat.Create16BitPcm(audioCodecContext->sample_rate, audioCodecContext->channels);
                        audioEmitter.Post(new AudioBuffer(convertedSamples, wave), dateA);
                    }
                }
                ffmpeg.av_frame_free(&frame);
            }
        }
        ffmpeg.av_packet_free(&packet);
        ffmpeg.avcodec_close(videoCodecContext);
        ffmpeg.avcodec_free_context(&videoCodecContext);
        ffmpeg.sws_freeContext(videoSwsctx);
        if (audioStreamIndex >= 0)
        {
            ffmpeg.avcodec_close(audioCodecContext);
            ffmpeg.avcodec_free_context(&audioCodecContext);
            ffmpeg.swr_free(&audioSwrctx);
         }
        ffmpeg.avformat_close_input(&formatContext);
    }
}

class Program
{
    static public void Main(string[] args)
    {
        DateTime time = DateTime.Now;
        DateTime.TryParse(args[2], out time);
        int encodingLevel = 50;
        int.TryParse(args[5], out encodingLevel);
        var process = new ffmpegToStore(args[0], args[1], time, args[3], args[4], encodingLevel, args.Length >= 7 ? args[6] : null);
    }
}