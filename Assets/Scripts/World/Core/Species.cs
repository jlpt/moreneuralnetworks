using System.Collections.Generic;

namespace NeuronWorld
{
    /// <summary>
    /// A group of related creatures. A child founds a new species when its genome has
    /// drifted far enough from the founder of its parent's species.
    /// </summary>
    public sealed class Species
    {
        public int id;
        public string name;
        /// <summary>Genome of the first member (kept for reference, e.g. species colour).</summary>
        public Genome founder;
        /// <summary>A recently sampled living member. New species split off when they drift too far from it.</summary>
        public Genome representative;
        internal int sampleCount;
        internal Genome sample;
        public int parentId = -1;
        public string parentName;
        public int bornTick;
        public int extinctTick = -1;
        public int population;
        public int peakPopulation;
        public int totalBorn;
        public int founderGeneration;
        /// <summary>Whether the world has been told about this species yet (only once it has a foothold).</summary>
        public bool announced;

        public bool Extinct { get { return extinctTick >= 0; } }
    }

    public sealed class SpeciesRegistry
    {
        public readonly List<Species> all = new List<Species>();
        public int AliveCount { get; private set; }

        static readonly string[] Starts =
        {
            "Zor", "Ka", "Mel", "Vex", "Ru", "Tal", "Ori", "Gri", "Fen", "Lu", "Mor", "Sca", "Thy", "Bra",
            "Qui", "Dra", "Ny", "Pel", "Sil", "Xan", "Bo", "Cri", "Eld", "Hu", "Ix", "Jar", "Ko", "Ul", "Wy", "Ze"
        };
        static readonly string[] Middles = { "", "", "a", "o", "i", "u", "e", "ra", "li", "no", "ta", "ve", "ko", "mi" };
        static readonly string[] Ends =
        {
            "pod", "morph", "saur", "lith", "vore", "nid", "opsis", "ling", "ite", "ax", "us", "on", "ia", "ops",
            "fin", "crawler", "back", "maw", "tail", "wing", "grub", "horn"
        };

        public Species Create(Genome founder, Species parent, int tick, int generation, Rng rng)
        {
            var s = new Species
            {
                id = all.Count,
                name = MakeName(rng),
                founder = founder,
                representative = founder,
                parentId = parent != null ? parent.id : -1,
                parentName = parent != null ? parent.name : null,
                bornTick = tick,
                founderGeneration = generation
            };
            all.Add(s);
            AliveCount++;
            return s;
        }

        public void OnBorn(Species s)
        {
            if (s.Extinct)
            {
                // A spawned clone can bring a species back from extinction.
                s.extinctTick = -1;
                AliveCount++;
            }
            s.population++;
            s.totalBorn++;
            if (s.population > s.peakPopulation) s.peakPopulation = s.population;
        }

        /// <summary>Returns true if this death made the species extinct.</summary>
        public bool OnDied(Species s, int tick)
        {
            s.population--;
            if (s.population <= 0 && !s.Extinct)
            {
                s.population = 0;
                s.extinctTick = tick;
                AliveCount--;
                return true;
            }
            return false;
        }

        /// <summary>
        /// Re-pick each living species' representative from its current members, so a species
        /// follows its own gradual drift and only true offshoots become new species.
        /// </summary>
        public void ResampleRepresentatives(List<Creature> creatures, Rng rng)
        {
            for (int i = 0; i < creatures.Count; i++)
            {
                Creature c = creatures[i];
                if (!c.alive) continue;
                Species s = c.species;
                s.sampleCount++;
                if (rng.Range(0, s.sampleCount) == 0) s.sample = c.genome;
            }
            for (int i = 0; i < all.Count; i++)
            {
                Species s = all[i];
                if (s.sample != null) s.representative = s.sample;
                s.sample = null;
                s.sampleCount = 0;
            }
        }

        static string MakeName(Rng rng)
        {
            string start = Starts[rng.Range(0, Starts.Length)];
            string middle = Middles[rng.Range(0, Middles.Length)];
            string end = Ends[rng.Range(0, Ends.Length)];
            return start + middle + end;
        }
    }
}
