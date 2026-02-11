// Licensed under the CeCILL-C License. See LICENSE.md file in the project root for full license information.
// This software is distributed under the CeCILL-C FREE SOFTWARE LICENSE AGREEMENT.
// See https://cecill.info/licences/Licence_CeCILL-C_V1-en.html for details.

namespace SAAC.LabStreamLayer
{
    using Microsoft.Psi;
    using Microsoft.Psi.Components;
    using static LSL.liblsl;

    /// <summary>
    /// A generic PSI component for Lab Streaming Layer (LSL) data streams.
    /// </summary>
    /// <typeparam name="T">The type of data in the LSL stream.</typeparam>
    public class LabStreamLayerComponent<T> : ILabStreamLayerComponent, ISourceComponent, IProducer<List<T>>, IDisposable
    {
        private readonly StreamInlet input;
        private readonly Pipeline pipeline;
        private readonly int channelCount;
        private readonly int samplingDuration;
        private Thread? thread;

        /// <summary>
        /// Initializes a new instance of the <see cref="LabStreamLayerComponent{T}"/> class.
        /// </summary>
        /// <param name="parent">The parent pipeline.</param>
        /// <param name="info">The LSL stream information.</param>
        /// <param name="producer">The LSL stream inlet.</param>
        /// <param name="maxBufferLength">Maximum buffer length for the stream.</param>
        internal LabStreamLayerComponent(ref Pipeline parent, StreamInfo info, StreamInlet producer, int maxBufferLength)
        {
            this.StreamInfo = info;
            this.input = producer;
            this.pipeline = parent;
            this.MaxBufferLength = maxBufferLength;
            this.Name = $"{info.name()}-{info.type()}";
            this.Out = parent.CreateEmitter<List<T>>(this, $"{this.Name}-Out");
            this.IsRunning = false;
            this.thread = null;
            this.channelCount = this.StreamInfo.channel_count();
            this.samplingDuration = this.StreamInfo.nominal_srate() == 0.0 ? 100 : (int)(1000.0 / this.StreamInfo.nominal_srate());
        }

        /// <summary>
        /// Gets the emitter for the stream data output.
        /// </summary>
        public Emitter<List<T>> Out { get; private set; }

        /// <summary>
        /// Gets the LSL stream information.
        /// </summary>
        public StreamInfo StreamInfo { get; private set; }

        /// <summary>
        /// Gets a value indicating whether the component is running.
        /// </summary>
        public bool IsRunning { get; private set; }

        /// <summary>
        /// Gets the maximum buffer length for the stream.
        /// </summary>
        public int MaxBufferLength { get; private set; }

        /// <summary>
        /// Gets the name of the component.
        /// </summary>
        public string Name { get; private set; }

        /// <inheritdoc/>
        public override string ToString() => this.Name;

        /// <summary>
        /// Gets the LSL stream information.
        /// </summary>
        /// <returns>The stream information object.</returns>
        public StreamInfo GetStreamInfo() => this.StreamInfo;

        /// <summary>
        /// Starts the component and begins receiving data from the LSL stream.
        /// </summary>
        /// <param name="notifyCompletionTime">Delegate to notify completion time.</param>
        public void Start(Action<DateTime> notifyCompletionTime)
        {
            this.IsRunning = true;
            this.input.open_stream();
            this.thread = new Thread(new ThreadStart(this.UpdateData));
            this.thread.Start();
            notifyCompletionTime(DateTime.MaxValue);
        }

        /// <summary>
        /// Stops the component.
        /// </summary>
        /// <param name="finalOriginatingTime">The final originating time.</param>
        /// <param name="notifyCompleted">Delegate to notify completion.</param>
        public void Stop(DateTime finalOriginatingTime, Action notifyCompleted)
        {
            this.Dispose();
            notifyCompleted();
        }

        /// <summary>
        /// Disposes the component and releases resources.
        /// </summary>
        public void Dispose()
        {
            this.IsRunning = false;
            this.input.close_stream();
            this.thread?.Join();
        }

        /// <summary>
        /// Gets the type of data in the stream channels.
        /// </summary>
        /// <returns>The channel data type, or null if unknown.</returns>
        public Type? GetStreamChannelType()
        {
            switch (this.StreamInfo.channel_format())
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

        /// <summary>
        /// Creates a buffer for the LSL stream data based on the channel format.
        /// </summary>
        /// <returns>A dynamic buffer array.</returns>
        /// <exception cref="Exception">Thrown when the channel format is unknown.</exception>
        protected dynamic CreateBuffer()
        {
            switch (this.StreamInfo.channel_format())
            {
                case channel_format_t.cf_string:
                    return new string[this.MaxBufferLength, this.channelCount];
                case channel_format_t.cf_double64:
                    return new double[this.MaxBufferLength, this.channelCount];
                case channel_format_t.cf_float32:
                    return new float[this.MaxBufferLength, this.channelCount];
                case channel_format_t.cf_int64:
                    return new long[this.MaxBufferLength, this.channelCount];
                case channel_format_t.cf_int32:
                    return new int[this.MaxBufferLength, this.channelCount];
                case channel_format_t.cf_int16:
                    return new short[this.MaxBufferLength, this.channelCount];
                case channel_format_t.cf_int8:
                    return new char[this.MaxBufferLength, this.channelCount];
            }

            throw new Exception("Can't create buffer of unkown type !");
        }

        /// <summary>
        /// Main update loop that pulls data from the LSL stream and posts it to the output emitter.
        /// </summary>
        protected void UpdateData()
        {
            double clock = local_clock();
            DateTime time = this.pipeline.GetCurrentTime();
            while (this.IsRunning)
            {
                dynamic buffer = this.CreateBuffer();
                double[] timestamps = new double[this.MaxBufferLength];
                int num = this.input.pull_chunk(buffer, timestamps, 1);
                double correction = this.input.time_correction(1);
                for (int s = 0; s < num; s++)
                {
                    List<T> data = new List<T>();
                    for (int c = 0; c < this.channelCount; c++)
                    {
                        data.Add(buffer[s, c]);
                    }

                    double secondsSinceStart = correction + timestamps[s] - clock;
                    if (secondsSinceStart < 0)
                    {
                        continue; // skip samples from the past
                    }

                    this.Out.Post(data, time.AddSeconds(secondsSinceStart));
                }

                Thread.Sleep(this.samplingDuration);
            }
        }
    }
}
