using System;

namespace NeuronWorld
{
    /// <summary>
    /// Tunable rules of the world. Rates are per simulation tick. Editable in the WorldManager inspector.
    /// Defaults were balanced with Tools/HeadlessSim so herbivores, omnivores and carnivores all persist
    /// and the population swings with the seasons without hitting maxPopulation.
    /// </summary>
    [Serializable]
    public sealed class SimSettings
    {
        public int width = 256;
        public int height = 160;
        public int seed; // 0 = random every time

        public int initialHerbivores = 160;
        public int initialCarnivores = 24;
        public int maxPopulation = 2500;
        public bool autoSeed = true;
        public int autoSeedMinimum = 40;
        /// <summary>New species start with basic survival wiring. Off = fully random brains (much slower start).</summary>
        public bool startWithInstincts = true;

        public float plantGrowth = 1f;
        public float plantEnergy = 40f;
        public float meatEnergy = 100f;
        public float meatDecay = 0.002f;
        public int yearLength = 3000;
        public float seasonStrength = 0.5f;

        public float baseMetabolism = 0.08f;
        public float sizeMetabolism = 0.12f;
        public float moveCost = 0.25f;
        public float brainCost = 0.001f;
        public float visionCost = 0.004f;
        public float biteSize = 0.06f;
        public float attackDamage = 30f;
        public float attackCost = 0.2f;
        public float maxSpeed = 0.14f;
        public float maxTurn = 0.3f;
        public int maturityAge = 150;
        public int reproduceCooldown = 90;

        public float speciesThreshold = 3.5f;
    }
}
