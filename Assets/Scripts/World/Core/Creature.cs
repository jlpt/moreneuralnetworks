namespace NeuronWorld
{
    /// <summary>One living agent in the world. Position is in tile units.</summary>
    public sealed class Creature
    {
        public readonly int id;
        public readonly Genome genome;
        public Species species;
        public int generation;
        public int parentId;

        public float x, y;
        public float heading;   // radians, 0 = +x (east)
        public float velocity;  // tiles per tick

        public float energy;
        /// <summary>Energy its parent invested at birth. Part of it comes back as meat when it dies.</summary>
        public float bodyEnergy;
        public float health;
        public float maxEnergy;
        public float maxHealth;
        public float radius;
        public int age;
        public int lifespan;
        public int reproduceCooldown;
        public int attackCooldown;

        public float signal;
        public float pain;       // damage received since the last time it sensed the world

        // Last attack, so renderers can draw a strike.
        public int lastAttackTick = -1000;
        public float lastTargetX, lastTargetY;

        public bool alive = true;
        public string causeOfDeath;
        public int deathTick;

        // Life statistics (shown in the inspector).
        public int children;
        public int kills;
        public float energyEaten;

        public Creature(int id, Genome genome)
        {
            this.id = id;
            this.genome = genome;
            float s = genome.size;
            maxEnergy = 100f * s;
            maxHealth = 30f + 50f * s;
            radius = 0.3f + 0.22f * s;
            lifespan = (int)(1200f + 600f * s);
            health = maxHealth;
        }

        public Brain Brain { get { return genome.brain; } }

        public float EnergyFraction { get { return maxEnergy > 0f ? energy / maxEnergy : 0f; } }
        public float HealthFraction { get { return maxHealth > 0f ? health / maxHealth : 0f; } }
    }
}
