using System;

namespace NeuronWorld
{
    /// <summary>Everything a creature inherits: body traits plus its brain wiring.</summary>
    public sealed class Genome
    {
        public const float MinSize = 0.5f, MaxSize = 2.5f;
        public const float MinSpeed = 0.5f, MaxSpeed = 2f;
        public const float MinVision = 3f, MaxVision = 14f;
        public const float MinMutation = 0.01f, MaxMutation = 0.25f;
        public const float MinOffspring = 0.15f, MaxOffspring = 0.7f;

        /// <summary>Body size: more health, damage and energy storage, but higher upkeep.</summary>
        public float size = 1f;
        /// <summary>Top speed multiplier. Moving fast costs energy quadratically.</summary>
        public float speed = 1f;
        /// <summary>0 = eats plants only, 1 = eats meat only. Also scales attack damage.</summary>
        public float diet;
        /// <summary>0 = land animal, 1 = water animal. Affects speed and survival in each medium.</summary>
        public float aquatic;
        /// <summary>How far the creature can see, in tiles.</summary>
        public float vision = 7f;
        /// <summary>Colour. Drifts a little every generation, like Neuron-Box organisms.</summary>
        public float hue, saturation = 0.7f, value = 0.9f;
        /// <summary>How strongly children differ from their parent. Itself evolves.</summary>
        public float mutationRate = 0.08f;
        /// <summary>Fraction of max energy given to each child (few big children vs. many small ones).</summary>
        public float offspringShare = 0.35f;

        public Brain brain;

        /// <param name="diet">0 = herbivore, 1 = carnivore.</param>
        /// <param name="aquatic">0 = land, 1 = sea.</param>
        /// <param name="instincts">Start with basic survival wiring instead of a fully random brain.</param>
        public static Genome CreateRandom(Rng rng, float diet, float aquatic, bool instincts)
        {
            var g = new Genome();
            g.diet = Clamp(diet, 0f, 1f);
            bool hunter = g.diet > 0.5f;
            g.size = hunter ? rng.Range(0.8f, 1.0f) : rng.Range(0.8f, 1.2f);
            g.speed = hunter ? rng.Range(1.2f, 1.5f) : rng.Range(0.8f, 1.2f);
            g.aquatic = Clamp(aquatic, 0f, 1f);
            g.vision = rng.Range(5f, 9f);
            g.hue = g.diet > 0.5f ? rng.Range(-0.08f, 0.08f) : rng.Range(0.12f, 0.9f);
            if (g.hue < 0f) g.hue += 1f;
            g.saturation = rng.Range(0.55f, 0.9f);
            g.value = rng.Range(0.75f, 1f);
            g.mutationRate = rng.Range(0.05f, 0.12f);
            g.offspringShare = rng.Range(0.25f, 0.45f);
            g.brain = instincts ? Brain.CreateWithInstincts(rng, g.diet, g.aquatic) : Brain.CreateRandom(rng);
            return g;
        }

        public Genome Clone()
        {
            var g = (Genome)MemberwiseClone();
            g.brain = brain.Clone();
            return g;
        }

        public void Mutate(Rng rng)
        {
            float r = mutationRate;
            size = MutateTrait(rng, size, MinSize, MaxSize, r);
            speed = MutateTrait(rng, speed, MinSpeed, MaxSpeed, r);
            diet = MutateTrait(rng, diet, 0f, 1f, r);
            aquatic = MutateTrait(rng, aquatic, 0f, 1f, r);
            vision = MutateTrait(rng, vision, MinVision, MaxVision, r);
            offspringShare = MutateTrait(rng, offspringShare, MinOffspring, MaxOffspring, r);
            mutationRate = MutateTrait(rng, mutationRate, MinMutation, MaxMutation, r * 0.5f);

            // Colour always drifts slightly (Neuron-Box's HSV drift) so family lines stay visible.
            hue = (hue + rng.Range(-0.02f, 0.02f) + 1f) % 1f;
            saturation = Clamp(saturation + rng.Range(-0.01f, 0.01f), 0.4f, 1f);
            value = Clamp(value + rng.Range(-0.01f, 0.01f), 0.6f, 1f);

            brain.Mutate(rng, r);
        }

        static float MutateTrait(Rng rng, float v, float min, float max, float rate)
        {
            if (!rng.Chance(rate * 3f)) return v;
            return Clamp(v + rng.Gaussian() * (max - min) * 0.05f, min, max);
        }

        /// <summary>Genetic distance between two genomes (body traits + brain wiring).</summary>
        public static float Distance(Genome a, Genome b)
        {
            float d = 0f;
            d += Math.Abs(a.size - b.size) / (MaxSize - MinSize);
            d += Math.Abs(a.speed - b.speed) / (MaxSpeed - MinSpeed);
            d += Math.Abs(a.diet - b.diet) * 2f;
            d += Math.Abs(a.aquatic - b.aquatic) * 2f;
            d += Math.Abs(a.vision - b.vision) / (MaxVision - MinVision);
            d += Math.Abs(a.offspringShare - b.offspringShare) / (MaxOffspring - MinOffspring);
            d += Brain.Distance(a.brain, b.brain);
            return d;
        }

        public string DietLabel
        {
            get { return DietName(diet); }
        }

        public static string DietName(float diet)
        {
            if (diet < 0.35f) return "Herbivore";
            if (diet > 0.65f) return "Carnivore";
            return "Omnivore";
        }

        static float Clamp(float v, float min, float max)
        {
            return v < min ? min : (v > max ? max : v);
        }
    }
}
