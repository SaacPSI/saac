using System.Numerics;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using Microsoft.Psi;
using Microsoft.Psi.Audio;
using OpenSmile.Utility;
using Environment = OpenSmileInterop.Environment;

namespace OpenSmile {

    public class OpenSmile  {

        private Environment env;

        private DateTimeOffset StartTime;

        private string syncInputName;

        public ImmutableDictionary<string, Receiver<AudioBuffer>> In { private set; get; } = ImmutableDictionary<string, Receiver<AudioBuffer>>.Empty;

        public ImmutableDictionary<string, Emitter<Vector<float>>> Out { private set; get; } = ImmutableDictionary<string, Emitter<Vector<float>>>.Empty;

        public OpenSmile(Pipeline pipeline, OpenSmileInterop.Configuration config) {
#if !DEBUG
			config.PrintLogToConsole = false;
#endif
            env = new Environment(config ?? throw new ArgumentNullException(nameof(config)));
            foreach (var name in env.RawDataSinkInstanceNames()) {
                var receiver = pipeline.CreateEmitter<Vector<float>>(this, name);
                Out = Out.Add(name, receiver);
                env.Hook(name, CreateRawDataHandler(name));
            }
            foreach (var name in env.RawWaveSourceInstanceNames()) {
                var emitter = pipeline.CreateReceiver<AudioBuffer>(this, CreateReceiveCallBack(name), name);
                In = In.Add(name, emitter);
                syncInputName = name;// record the last wavesource instance's name
            }
            pipeline.PipelineRun += OnPipeRun;
            pipeline.PipelineCompleted += OnPipeCompleted;
        }

        private void OnPipeRun(object sender, PipelineRunEventArgs e) {
            StartTime = DateTimeOffset.Now;
        }

        private void OnPipeCompleted(object sender, PipelineCompletedEventArgs e) {
            env?.Dispose();
            env = null;
        }

        /// <summary>
        /// Anonymous functions does not support attributes,
        /// since we want to catch underlying exceptions when calling RunOneIteration() using [HandleProcessCorruptedStateExceptions]
        /// so we need this closure to add that attribute
        /// </summary>
        private class Closure {
            private OpenSmile opensmile;
            private string sourceName;

            public Closure(OpenSmile opensmile, string sourceName) {
                this.opensmile = opensmile;
                this.sourceName = sourceName;
            }

            public void Porcess(AudioBuffer input, Envelope envelope) {
                try {
                    opensmile.env.Feed(sourceName, input.Data);
                    if (sourceName == opensmile.syncInputName) {
                        var tick = opensmile.env.RunOneIteration();
                    }
                } 
                catch (Exception ex) 
                {
                    Console.Error.WriteLine($"An exception is raised in {GetType().Name}[source: {sourceName}]: {ex.ToString()}");
                }
            }
        }

        protected Action<AudioBuffer, Envelope> CreateReceiveCallBack(string sourceName) {
            return new Closure(this, sourceName).Porcess;
        }

        protected OpenSmileInterop.RawDataHandler CreateRawDataHandler(string sinkName) {
            return (data) => {
                try {
                    var time = StartTime + TimeSpan.FromSeconds(data.Time);//TODO: examine time
                    Out[sinkName].Post(data.CreateVector_Single(), time.DateTime);
                } catch (Exception ex) {
                    Console.Error.WriteLine($"An exception is raised in {GetType().Name}[sink: {sinkName}]: {ex.ToString()}");
                }
            };
        }
    }

}
