using System.Collections.Generic;

namespace NeuronWorld
{
    public struct StatsSample
    {
        public int tick;
        public int population;
        public int herbivores;
        public int omnivores;
        public int carnivores;
        public int aquatic;
        public int species;
        public int maxGeneration;
        public float avgSize;
        public float avgSpeed;
        public float avgHidden;
        public float avgSynapses;
    }

    /// <summary>
    /// Population history for graphs. Keeps the whole run by halving its resolution
    /// whenever it fills up.
    /// </summary>
    public sealed class StatsHistory
    {
        public readonly List<StatsSample> samples = new List<StatsSample>();
        readonly int capacity;
        public int Interval { get; private set; }

        public StatsHistory(int capacity, int interval)
        {
            this.capacity = capacity;
            Interval = interval;
        }

        public void Add(StatsSample s)
        {
            samples.Add(s);
            if (samples.Count < capacity) return;
            int w = 0;
            for (int r = 0; r < samples.Count; r += 2) samples[w++] = samples[r];
            samples.RemoveRange(w, samples.Count - w);
            Interval *= 2;
        }
    }

    public struct WorldEvent
    {
        public int tick;
        public string text;
        public int speciesId;

        public WorldEvent(int tick, string text, int speciesId)
        {
            this.tick = tick;
            this.text = text;
            this.speciesId = speciesId;
        }
    }
}
