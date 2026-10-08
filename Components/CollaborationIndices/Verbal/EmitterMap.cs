using System;
using System.Collections.Generic;
using Microsoft.Psi;

namespace SAAC.CollaborationIndices
{
    /// <summary>
    /// A set of emitters of any type, indexed by an arbitrary key.
    ///
    /// This is what removes the "one member per participant" pattern: instead of declaring
    /// individualsVad1Out … individualsVad7Out and a switch to pick one, the emitters are
    /// created from the participant list and retrieved by key.
    /// </summary>
    public class EmitterMap<TKey, TValue>
    {
        private readonly Dictionary<TKey, Emitter<TValue>> emitters = new Dictionary<TKey, Emitter<TValue>>();

        public EmitterMap(Pipeline pipeline, object owner, IEnumerable<TKey> keys, string prefix)
        {
            foreach (TKey key in keys)
            {
                if (!this.emitters.ContainsKey(key))
                {
                    this.emitters[key] = pipeline.CreateEmitter<TValue>(owner, $"{prefix}-{key}");
                }
            }
        }

        public Emitter<TValue> this[TKey key]
        {
            get
            {
                if (!this.emitters.TryGetValue(key, out var emitter))
                {
                    throw new ArgumentException($"No emitter declared for key {key}.", nameof(key));
                }

                return emitter;
            }
        }

        public bool TryGet(TKey key, out Emitter<TValue> emitter) => this.emitters.TryGetValue(key, out emitter);

        public IEnumerable<TKey> Keys => this.emitters.Keys;

        public void PostAll(IReadOnlyDictionary<TKey, TValue> values, DateTime originatingTime)
        {
            foreach (var entry in values)
            {
                if (this.emitters.TryGetValue(entry.Key, out var emitter))
                {
                    emitter.Post(entry.Value, originatingTime);
                }
            }
        }
    }

    /// <summary>
    /// A set of receivers of any type, indexed by an arbitrary key. The key is captured in the
    /// closure, so a single handler serves every participant and no per participant Process
    /// method is needed.
    /// </summary>
    public class ReceiverMap<TKey, TValue>
    {
        private readonly Dictionary<TKey, Receiver<TValue>> receivers = new Dictionary<TKey, Receiver<TValue>>();

        public ReceiverMap(Pipeline pipeline, object owner, IEnumerable<TKey> keys, Action<TKey, TValue, Envelope> handler, string prefix)
        {
            foreach (TKey key in keys)
            {
                TKey captured = key;
                if (!this.receivers.ContainsKey(captured))
                {
                    this.receivers[captured] = pipeline.CreateReceiver<TValue>(
                        owner,
                        (value, envelope) => handler(captured, value, envelope),
                        $"{prefix}-{captured}");
                }
            }
        }

        public Receiver<TValue> this[TKey key]
        {
            get
            {
                if (!this.receivers.TryGetValue(key, out var receiver))
                {
                    throw new ArgumentException($"No receiver declared for key {key}.", nameof(key));
                }

                return receiver;
            }
        }

        public bool TryGet(TKey key, out Receiver<TValue> receiver) => this.receivers.TryGetValue(key, out receiver);

        public IEnumerable<TKey> Keys => this.receivers.Keys;
    }
}
