using Microsoft.Psi;
using Microsoft.Psi.Components;
using static LSL.liblsl;

namespace SAAC.LabStreamLayer
{
    public class LabStreamLayerComponent<T> : ILabStreamLayerComponent, ISourceComponent, IProducer<List<T>>, IDisposable
    {
        public Emitter<List<T>> Out {get; private set;}
        public StreamInfo StreamInfo { get; private set; }
        public bool IsRunning { get; private set; }
        public int MaxBufferLength { get; private set; }
        public string Name { get; private set; }

        protected StreamInlet input;
        protected Thread? thread;
        protected int channelCount;
        protected int samplingDuration;

        private Pipeline pipeline;

        internal LabStreamLayerComponent(ref Pipeline parent, StreamInfo info, StreamInlet producer, int maxBufferLength) 
        {
            StreamInfo = info;
            input = producer;
            pipeline = parent;
            MaxBufferLength = maxBufferLength;
            Name = $"{info.name()}-{info.type()}";
            Out = parent.CreateEmitter<List<T>>(this, $"{this.Name}-Out");
            IsRunning = false;
            thread = null;
            channelCount = StreamInfo.channel_count();
            samplingDuration = StreamInfo.nominal_srate() == 0.0 ? 100 : (int)(1000.0 / StreamInfo.nominal_srate());
        }

        public override string ToString() => this.Name;

        public StreamInfo GetStreamInfo() => this.StreamInfo;

        public void Start(Action<DateTime> notifyCompletionTime)
        {
            IsRunning = true;
            input.open_stream();
            thread = new Thread(new ThreadStart(this.updateData));
            thread.Start();
            notifyCompletionTime(DateTime.MaxValue);
        }

        public void Stop(DateTime finalOriginatingTime, Action notifyCompleted)
        {
            Dispose();
            notifyCompleted();
        }

        public void Dispose()
        {
            IsRunning = false;
            input.close_stream();
            thread?.Join();
        }

        public Type? GetStreamChannelType()
        {
            switch (StreamInfo.channel_format())
            {
                case channel_format_t.cf_string:
                    return typeof(string);
                case channel_format_t.cf_double64:
                    return typeof(double);
                case channel_format_t.cf_float32:
                    return typeof(float);
                case channel_format_t.cf_int64:
                    return typeof(long);
                case channel_format_t.cf_int32:
                    return typeof(int);
                case channel_format_t.cf_int16:
                    return typeof(short);
                case channel_format_t.cf_int8:
                    return typeof(char);
            }
            return null;
        }

        protected dynamic CreateBuffer()
        {
            switch (StreamInfo.channel_format())
            {
                case channel_format_t.cf_string:
                    return new string[MaxBufferLength, channelCount]; 
                case channel_format_t.cf_double64:
                    return new double[MaxBufferLength, channelCount];
                case channel_format_t.cf_float32:
                    return new float[MaxBufferLength, channelCount];
                case channel_format_t.cf_int64:
                    return new long[MaxBufferLength, channelCount];
                case channel_format_t.cf_int32:
                    return new int[MaxBufferLength, channelCount];
                case channel_format_t.cf_int16:
                    return new short[MaxBufferLength, channelCount];
                case channel_format_t.cf_int8:
                    return new char[MaxBufferLength, channelCount];
            }
            throw new Exception("Can't create buffer of unkown type !");
        }

        protected void updateData()
        {
            double clock = local_clock();
            DateTime time = pipeline.GetCurrentTime();
            while (IsRunning)
            {
                dynamic buffer = CreateBuffer();
                double[] timestamps = new double[MaxBufferLength];
                int num = input.pull_chunk(buffer, timestamps, 1);
                double correction = input.time_correction(1);
                for (int s = 0; s < num; s++)
                {
                    List<T> data = new List<T>();
                    for (int c = 0; c < channelCount; c++)
                        data.Add(buffer[s, c]);

                    double secondsSinceStart = correction + timestamps[s] - clock;
                    if (secondsSinceStart < 0)
                        continue; // skip samples from the past
                    Out.Post(data, time.AddSeconds(secondsSinceStart));
                }
                Thread.Sleep(samplingDuration);
            }
        }
    }
}
