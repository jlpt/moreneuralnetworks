using System;
using System.Collections.Generic;

namespace NeuronWorld
{
    /// <summary>Names and indices of every sense (input) and action (output) a brain is wired to.</summary>
    public static class BrainIO
    {
        // Inputs
        public const int Bias = 0;
        public const int Energy = 1;
        public const int Health = 2;
        public const int Age = 3;
        public const int FoodHere = 4;
        public const int InWater = 5;
        public const int ClockFast = 6;
        public const int ClockSlow = 7;
        public const int Pain = 8;
        public const int Hearing = 9;
        public const int Speed = 10;
        public const int Crowding = 11;
        public const int SectorStart = 12;

        // Vision is split into sectors across the 180 degrees in front of the creature,
        // from right (sector 0) to left (sector 4). Each sector reports:
        public const int Sectors = 5;
        public const int SectorFood = 0;      // food this creature can digest in that direction
        public const int SectorWater = 1;     // how much of that direction is water / world edge
        public const int SectorCreature = 2;  // closeness of the nearest creature (0 = none, 1 = touching)
        public const int SectorKin = 3;       // +1 same species, -1 other species
        public const int SectorSize = 4;      // + bigger than me, - smaller than me
        public const int PerSector = 5;

        public const int InputCount = SectorStart + Sectors * PerSector;

        // Outputs
        public const int OutForward = 0;
        public const int OutTurn = 1;
        public const int OutEat = 2;
        public const int OutAttack = 3;
        public const int OutReproduce = 4;
        public const int OutSignal = 5;
        public const int OutputCount = 6;

        public static readonly string[] SectorNames = { "right", "front-right", "front", "front-left", "left" };
        static readonly string[] SectorFieldNames = { "food", "water", "creature", "kin", "size" };

        public static readonly string[] OutputNames = { "Forward", "Turn", "Eat", "Attack", "Reproduce", "Signal" };

        public static readonly string[] InputNames = BuildInputNames();

        static string[] BuildInputNames()
        {
            var names = new string[InputCount];
            names[Bias] = "Bias";
            names[Energy] = "Energy";
            names[Health] = "Health";
            names[Age] = "Age";
            names[FoodHere] = "Food here";
            names[InWater] = "In water";
            names[ClockFast] = "Clock (fast)";
            names[ClockSlow] = "Clock (slow)";
            names[Pain] = "Pain";
            names[Hearing] = "Hearing";
            names[Speed] = "Speed";
            names[Crowding] = "Crowding";
            for (int s = 0; s < Sectors; s++)
                for (int f = 0; f < PerSector; f++)
                    names[SectorStart + s * PerSector + f] = "See " + SectorFieldNames[f] + " (" + SectorNames[s] + ")";
            return names;
        }
    }

    public struct Synapse
    {
        public int from;
        public int to;
        public float weight;

        public Synapse(int from, int to, float weight)
        {
            this.from = from;
            this.to = to;
            this.weight = weight;
        }
    }

    /// <summary>
    /// An evolvable recurrent neural network, descended from Neuron-Box's NeuralNetwork:
    /// input / hidden ("normal") / output neurons joined by sparse weighted connections,
    /// where each neuron keeps part of its previous activation (Neuron-Box used a fixed
    /// 0.98 decay; here the amount is per neuron and evolves).
    ///
    /// Neuron layout: [inputs][outputs][hidden...]. Hidden neurons can be added and
    /// removed by mutation, like the neuron add/remove code that was commented out in
    /// Neuron-Box's Organism.Mutate.
    /// </summary>
    public sealed class Brain
    {
        public const int FirstOutput = BrainIO.InputCount;
        public const int FirstHidden = BrainIO.InputCount + BrainIO.OutputCount;
        public const int MaxHidden = 24;
        public const int MaxSynapses = 160;

        // Genes
        public readonly List<float> bias = new List<float>();
        public readonly List<float> memory = new List<float>(); // 0 = no memory, 0.95 = very slow to change
        public readonly List<Synapse> synapses = new List<Synapse>();

        // Runtime (rebuilt by Compile)
        float[] state = new float[0];
        float[] sums = new float[0];
        float[] biasArr = new float[0];
        float[] memoryArr = new float[0];
        int[] synFrom = new int[0];
        int[] synTo = new int[0];
        float[] synWeight = new float[0];

        /// <summary>Current activation of every neuron. Inputs are written here before Think().</summary>
        public float[] State { get { return state; } }
        public int NeuronCount { get { return bias.Count; } }
        public int HiddenCount { get { return bias.Count - FirstHidden; } }

        public static Brain CreateRandom(Rng rng)
        {
            var b = new Brain();
            int hidden = rng.Range(2, 7);
            int total = FirstHidden + hidden;
            for (int i = 0; i < total; i++)
            {
                bool isInput = i < FirstOutput;
                b.bias.Add(isInput ? 0f : rng.Gaussian() * 0.4f);
                b.memory.Add(isInput ? 0f : rng.Range(0f, 0.6f));
            }
            int count = rng.Range(16, 30);
            for (int i = 0; i < count; i++) b.TryAddRandomSynapse(rng);
            b.Compile();
            return b;
        }

        /// <summary>
        /// A random brain plus a few "instinct" connections so a new species can survive long enough
        /// to evolve: herbivores steer towards plants and eat, carnivores chase other species and bite.
        /// The instincts are ordinary connections, so evolution can strengthen, rewire or drop them.
        /// </summary>
        public static Brain CreateWithInstincts(Rng rng, float diet, float aquatic)
        {
            var b = new Brain();
            int hidden = rng.Range(1, 5);
            for (int i = 0; i < FirstHidden + hidden; i++)
            {
                bool isInput = i < FirstOutput;
                b.bias.Add(isInput ? 0f : rng.Gaussian() * 0.2f);
                b.memory.Add(isInput ? 0f : rng.Range(0f, 0.4f));
            }

            const int Front = BrainIO.SectorStart + 2 * BrainIO.PerSector;
            const int FrontRight = BrainIO.SectorStart + 1 * BrainIO.PerSector;
            const int FrontLeft = BrainIO.SectorStart + 3 * BrainIO.PerSector;
            const int Right = BrainIO.SectorStart;
            const int Left = BrainIO.SectorStart + 4 * BrainIO.PerSector;
            int forward = FirstOutput + BrainIO.OutForward;
            int turn = FirstOutput + BrainIO.OutTurn;
            int eat = FirstOutput + BrainIO.OutEat;
            int attack = FirstOutput + BrainIO.OutAttack;
            int reproduce = FirstOutput + BrainIO.OutReproduce;

            float v = 1f + rng.Gaussian() * 0.15f; // individual variation in instinct strength

            // Shared: wander, breed when well fed, eat what is in front of you.
            b.Instinct(BrainIO.Bias, forward, 0.5f * v);
            b.Instinct(BrainIO.ClockSlow, turn, 0.4f * v);
            b.Instinct(BrainIO.Energy, reproduce, 3f * v);
            b.Instinct(BrainIO.Bias, reproduce, -1.6f * v);
            b.Instinct(BrainIO.FoodHere, eat, 3f * v);
            b.Instinct(BrainIO.FoodHere, forward, -1.2f * v);

            // Steer towards food.
            b.Instinct(Front + BrainIO.SectorFood, forward, 1.2f * v);
            b.Instinct(FrontLeft + BrainIO.SectorFood, turn, 1.5f * v);
            b.Instinct(Left + BrainIO.SectorFood, turn, 1f * v);
            b.Instinct(FrontRight + BrainIO.SectorFood, turn, -1.5f * v);
            b.Instinct(Right + BrainIO.SectorFood, turn, -1f * v);

            if (aquatic > 0.5f)
            {
                // Sea animals turn towards open water.
                b.Instinct(Front + BrainIO.SectorWater, forward, 0.8f * v);
                b.Instinct(FrontLeft + BrainIO.SectorWater, turn, 1.2f * v);
                b.Instinct(FrontRight + BrainIO.SectorWater, turn, -1.2f * v);
            }
            else
            {
                // Land animals turn away from water.
                b.Instinct(Front + BrainIO.SectorWater, turn, 1.5f * v);
                b.Instinct(FrontRight + BrainIO.SectorWater, turn, 1.2f * v);
                b.Instinct(FrontLeft + BrainIO.SectorWater, turn, -1.2f * v);
            }

            if (diet > 0.5f)
            {
                // Hunters: close in on other species and bite; leave their own kind alone.
                b.Instinct(Front + BrainIO.SectorCreature, forward, 1.5f * v);
                b.Instinct(Front + BrainIO.SectorCreature, attack, 2.5f * v);
                b.Instinct(Front + BrainIO.SectorKin, attack, -2f * v);
                b.Instinct(Front + BrainIO.SectorSize, attack, -1.5f * v); // prefer prey smaller than yourself
                b.Instinct(FrontLeft + BrainIO.SectorCreature, turn, 1.5f * v);
                b.Instinct(Left + BrainIO.SectorCreature, turn, 1f * v);
                b.Instinct(FrontRight + BrainIO.SectorCreature, turn, -1.5f * v);
                b.Instinct(Right + BrainIO.SectorCreature, turn, -1f * v);
                b.bias[attack] = -0.8f;
            }
            else
            {
                b.bias[attack] = -1f;
            }

            int extra = rng.Range(4, 12);
            for (int i = 0; i < extra; i++) b.TryAddRandomSynapse(rng);
            b.Compile();
            return b;
        }

        void Instinct(int from, int to, float weight)
        {
            synapses.Add(new Synapse(from, to, weight));
        }

        public Brain Clone()
        {
            var b = new Brain();
            b.bias.AddRange(bias);
            b.memory.AddRange(memory);
            b.synapses.AddRange(synapses);
            b.Compile();
            return b;
        }

        /// <summary>Rebuild the fast runtime arrays after the genes change. Resets activations.</summary>
        public void Compile()
        {
            int n = bias.Count;
            state = new float[n];
            sums = new float[n];
            biasArr = bias.ToArray();
            memoryArr = memory.ToArray();
            int s = synapses.Count;
            synFrom = new int[s];
            synTo = new int[s];
            synWeight = new float[s];
            for (int i = 0; i < s; i++)
            {
                synFrom[i] = synapses[i].from;
                synTo[i] = synapses[i].to;
                synWeight[i] = synapses[i].weight;
            }
        }

        /// <summary>One step of thinking. Signals take one tick to cross each connection.</summary>
        public void Think()
        {
            int n = state.Length;
            for (int j = FirstOutput; j < n; j++) sums[j] = biasArr[j];
            for (int k = 0; k < synFrom.Length; k++) sums[synTo[k]] += synWeight[k] * state[synFrom[k]];
            for (int j = FirstOutput; j < n; j++)
            {
                float m = memoryArr[j];
                state[j] = m * state[j] + (1f - m) * MathF.Tanh(sums[j]);
            }
        }

        public float Output(int output)
        {
            return state[FirstOutput + output];
        }

        public void Mutate(Rng rng, float rate)
        {
            // Nudge connection weights (occasionally re-roll one completely).
            for (int i = 0; i < synapses.Count; i++)
            {
                if (!rng.Chance(rate)) continue;
                Synapse s = synapses[i];
                s.weight = rng.Chance(0.1f) ? rng.Range(-2f, 2f) : Clamp(s.weight + rng.Gaussian() * 0.35f, -4f, 4f);
                synapses[i] = s;
            }

            // Nudge neuron biases and memory.
            for (int j = FirstOutput; j < bias.Count; j++)
            {
                if (rng.Chance(rate * 0.5f)) bias[j] = Clamp(bias[j] + rng.Gaussian() * 0.25f, -3f, 3f);
                if (rng.Chance(rate * 0.25f)) memory[j] = Clamp(memory[j] + rng.Gaussian() * 0.1f, 0f, 0.95f);
            }

            // Structural mutations.
            if (rng.Chance(rate * 2f)) TryAddRandomSynapse(rng);
            if (rng.Chance(rate * 1f) && synapses.Count > 0) synapses.RemoveAt(rng.Range(0, synapses.Count));
            if (rng.Chance(rate * 0.4f)) AddHiddenNeuron(rng);
            if (rng.Chance(rate * 0.25f)) RemoveHiddenNeuron(rng);

            Compile();
        }

        bool TryAddRandomSynapse(Rng rng)
        {
            if (synapses.Count >= MaxSynapses) return false;
            int n = bias.Count;
            for (int attempt = 0; attempt < 8; attempt++)
            {
                int from = rng.Range(0, n);
                int to = rng.Range(FirstOutput, n);
                if (HasSynapse(from, to)) continue;
                synapses.Add(new Synapse(from, to, rng.Gaussian() * 1.2f));
                return true;
            }
            return false;
        }

        bool HasSynapse(int from, int to)
        {
            for (int i = 0; i < synapses.Count; i++)
                if (synapses[i].from == from && synapses[i].to == to) return true;
            return false;
        }

        /// <summary>Insert a new hidden neuron in the middle of an existing connection.</summary>
        void AddHiddenNeuron(Rng rng)
        {
            if (HiddenCount >= MaxHidden || synapses.Count == 0 || synapses.Count >= MaxSynapses) return;
            int k = rng.Range(0, synapses.Count);
            Synapse old = synapses[k];
            int neuron = bias.Count;
            bias.Add(0f);
            memory.Add(rng.Range(0f, 0.5f));
            synapses[k] = new Synapse(old.from, neuron, 1f);
            synapses.Add(new Synapse(neuron, old.to, old.weight));
        }

        /// <summary>Delete a hidden neuron and every connection touching it, then re-index the rest.</summary>
        void RemoveHiddenNeuron(Rng rng)
        {
            if (HiddenCount <= 0) return;
            int removed = FirstHidden + rng.Range(0, HiddenCount);
            bias.RemoveAt(removed);
            memory.RemoveAt(removed);
            for (int i = synapses.Count - 1; i >= 0; i--)
            {
                Synapse s = synapses[i];
                if (s.from == removed || s.to == removed)
                {
                    synapses.RemoveAt(i);
                    continue;
                }
                if (s.from > removed) s.from--;
                if (s.to > removed) s.to--;
                synapses[i] = s;
            }
        }

        /// <summary>How different two brains are, used to decide when a new species has formed.</summary>
        public static float Distance(Brain a, Brain b)
        {
            var weights = new Dictionary<long, float>(a.synapses.Count);
            foreach (var s in a.synapses) weights[Key(s)] = s.weight;

            int matching = 0;
            float weightDiff = 0f;
            foreach (var s in b.synapses)
            {
                float w;
                if (weights.TryGetValue(Key(s), out w))
                {
                    matching++;
                    weightDiff += Math.Abs(w - s.weight);
                }
            }
            int disjoint = (a.synapses.Count - matching) + (b.synapses.Count - matching);
            int larger = Math.Max(1, Math.Max(a.synapses.Count, b.synapses.Count));
            float d = disjoint / (float)larger;
            if (matching > 0) d += 0.5f * weightDiff / matching;
            d += 0.05f * Math.Abs(a.HiddenCount - b.HiddenCount);
            return d;
        }

        static long Key(Synapse s)
        {
            return ((long)s.from << 32) | (uint)s.to;
        }

        static float Clamp(float v, float min, float max)
        {
            return v < min ? min : (v > max ? max : v);
        }
    }
}
