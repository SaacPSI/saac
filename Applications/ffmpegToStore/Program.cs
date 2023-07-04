using System.Collections;
using System.Runtime.InteropServices;
using FFmpeg.AutoGen;
using Microsoft.Psi;
using Microsoft.Psi.Data;
using Microsoft.Psi.Imaging;

class ffmpegToStore
{
    private Pipeline pipeline;
    private Emitter<Shared<Image>> emitter;
    private PsiExporter store;

    public ffmpegToStore(string ffmpegPath, string file, DateTime startTime, string storeName, string storePath, int encodingLevel)
    {
        pipeline = Pipeline.Create();
        emitter = pipeline.CreateEmitter<Shared<Image>>(pipeline, "image");
        store = PsiStore.Create(pipeline, storeName, storePath);
        store.Write(emitter.EncodeJpeg(encodingLevel), "video");
        pipeline.RunAsync();
        ExtractFrames(ffmpegPath, file, startTime);
        pipeline.Dispose();
    }
    unsafe void ExtractFrames(string ffmpegPath, string videoPath, DateTime startTime)
    {
        ffmpeg.RootPath = ffmpegPath;
        ffmpeg.avdevice_register_all();
        ffmpeg.av_log_set_level(ffmpeg.AV_LOG_ERROR);
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

        AVFormatContext* formatContext = null;
        AVInputFormat* formatInputFormat = ffmpeg.av_find_input_format(videoPath.Substring(videoPath.LastIndexOf(".")+1));

        if (ffmpeg.avformat_open_input(&formatContext, videoPath, formatInputFormat, null) != 0)
        {
            Console.WriteLine($"Error while opening the video : {videoPath}");
            return;
        }

        AVCodecContext* codecContext = null;
        AVCodec* codec = null;
        int videoStreamIndex = ffmpeg.av_find_best_stream(formatContext, AVMediaType.AVMEDIA_TYPE_VIDEO, -1, -1, &codec, 0);
        if (videoStreamIndex < 0)
        {
            Console.WriteLine("Stream not found in file.");
            ffmpeg.avformat_close_input(&formatContext);
            return;
        }
        if (ffmpeg.avformat_find_stream_info(formatContext, null) < 0)
        {
            Console.WriteLine("Stream info not found.");
            ffmpeg.avformat_close_input(&formatContext);
            return;
        }

        AVCodecParameters* codecParameters = formatContext->streams[videoStreamIndex]->codecpar;
        var pixelFormat = (AVPixelFormat)formatContext->streams[videoStreamIndex]->codecpar->format;
    
        if (codec == null)
        {
            Console.WriteLine("Codec not found.");
            ffmpeg.avformat_close_input(&formatContext);
            return;
        }

        codecContext = ffmpeg.avcodec_alloc_context3(codec);
        if (codecContext == null)
        {
            Console.WriteLine("Failed to initialize codec.");
            ffmpeg.avformat_close_input(&formatContext);
            return;
        }

        if (ffmpeg.avcodec_parameters_to_context(codecContext, codecParameters) < 0)
        {
            Console.WriteLine("Failed to set parameters in codec.");
            ffmpeg.avcodec_free_context(&codecContext);
            ffmpeg.avformat_close_input(&formatContext);
            return;
        }

        if (ffmpeg.avcodec_open2(codecContext, codec, null) < 0)
        {
            Console.WriteLine("Failed to open codec.");
            ffmpeg.avcodec_free_context(&codecContext);
            ffmpeg.avformat_close_input(&formatContext);
            return;
        }
        AVPacket* packet = ffmpeg.av_packet_alloc();

        AVPixelFormat fmt = codec->pix_fmts == null ? pixelFormat : *codec->pix_fmts;
        SwsContext* swsctx = ffmpeg.sws_getContext(codecParameters->width, codecParameters->height, fmt,
        codecParameters->width, codecParameters->height, AVPixelFormat.AV_PIX_FMT_BGR24, 0, null, null, null);
        if (swsctx == null)
        {
            Console.WriteLine("Failed to open SwsContext.");
            return ;
        }

        int dataSize = codecParameters->width * codecParameters->height * 3;
        byte[] bgrData = new byte[dataSize];
        byte_ptrArray4 dstData = new byte_ptrArray4();
        int[] dstLinesize = new int[4];
        dstLinesize[0] = codecParameters->width * 3;
        while (ffmpeg.av_read_frame(formatContext, packet) >= 0)
        {
            if (packet->stream_index == videoStreamIndex)
            {
                ffmpeg.avcodec_send_packet(codecContext, packet);
                AVFrame* frame = ffmpeg.av_frame_alloc();
                Shared<Image> image = ImagePool.GetOrCreate(codecParameters->width, codecParameters->height, PixelFormat.BGR_24bpp);
                while (ffmpeg.avcodec_receive_frame(codecContext, frame) == 0)
                {
                    fixed (byte* bgrPtr = bgrData)
                    {
                        dstData[0] = bgrPtr;
                        if(ffmpeg.sws_scale(swsctx, frame->data, frame->linesize, 0, frame->height, dstData, dstLinesize) < 0)
                            continue;
                    }
                    image.Resource.CopyFrom(bgrData);
                    DateTime date = startTime.AddSeconds(frame->coded_picture_number / (double)formatContext->streams[videoStreamIndex]->avg_frame_rate.num);
                    emitter.Post(image, date);
                }
                ffmpeg.av_frame_free(&frame);
            }
        }
        ffmpeg.av_packet_free(&packet);
        ffmpeg.avcodec_close(codecContext);
        ffmpeg.sws_freeContext(swsctx);
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
        var process = new ffmpegToStore(args[0], args[1], time, args[3], args[4], encodingLevel);
    }
}