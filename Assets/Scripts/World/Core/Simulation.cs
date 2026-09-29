using System;
using System.Collections.Generic;

namespace NeuronWorld
{
    /// <summary>
    /// The whole world: map, creatures, species, and the rules that connect them.
    /// Pure C# (no UnityEngine) so it can also be run and tuned headless.
    /// Each tick every creature senses the world, thinks with its brain and acts.
    /// </summary>
    public sealed class Simulation
    {
        public const int MaxEvents = 60;
        const float HearingRange = 8f;
        const float TwoPi = 6.28318531f;
        const float Pi = 3.14159265f;
        const float HalfPi = 1.57079633f;
        const float SectorWidth = Pi / BrainIO.Sectors;
        const int AttackCooldownTicks = 4;
        const int AnnouncePopulation = 8;

        public readonly SimSettings settings;
        public readonly int seed;
        public readonly WorldMap map;
        public readonly List<Creature> creatures = new List<Creature>();
        public readonly SpeciesRegistry species = new SpeciesRegistry();
        public readonly StatsHistory history = new StatsHistory(512, 30);
        public readonly List<WorldEvent> events = new List<WorldEvent>();
        /// <summary>Deaths so far, by cause ("Predation", "Starved", "Old age", ...).</summary>
        public readonly Dictionary<string, int> deathCauses = new Dictionary<string, int>();
        /// <summary>Called whenever a creature dies (after its cause of death is set).</summary>
        public event Action<Creature> Died;

        readonly Rng rng;
        readonly SpatialGrid grid;
        readonly List<Creature> births = new List<Creature>();
        readonly List<Creature> neighbours = new List<Creature>();
        static readonly Predicate<Creature> IsDead = c => !c.alive;
        bool stepping;
        int nextId = 1;

        public int Tick { get; private set; }
        public int TotalBirths { get; private set; }
        public int TotalDeaths { get; private set; }
        public int MaxGeneration { get; private set; }
        /// <summary>Increases every time an event is logged, so UIs can spot new ones.</summary>
        public int EventSerial { get; private set; }
        public StatsSample Latest { get; private set; }

        public Simulation(SimSettings settings)
        {
            this.settings = settings;
            seed = settings.seed != 0 ? settings.seed : ((Environment.TickCount & 0x7fffffff) | 1);
            rng = new Rng(seed);
            map = new WorldMap(settings.width, settings.height);
            map.Generate(seed);
            grid = new SpatialGrid(map.Width, map.Height, 8f);
            SeedPopulation();
            Latest = ComputeStats();
            history.Add(Latest);
        }

        // ------------------------------------------------------------------
        // Time
        // ------------------------------------------------------------------

        public float YearPhase { get { return (Tick % settings.yearLength) / (float)settings.yearLength; } }
        public int Year { get { return Tick / settings.yearLength + 1; } }
        public float SeasonMultiplier { get { return 1f + settings.seasonStrength * MathF.Sin(TwoPi * YearPhase); } }

        public string SeasonName
        {
            get
            {
                float p = YearPhase;
                if (p < 0.25f) return "Spring";
                if (p < 0.5f) return "Summer";
                if (p < 0.75f) return "Autumn";
                return "Winter";
            }
        }

        // ------------------------------------------------------------------
        // Main loop
        // ------------------------------------------------------------------

        public void Step()
        {
            stepping = true;
            Tick++;
            creatures.RemoveAll(IsDead);
            map.Step(settings.plantGrowth * SeasonMultiplier, settings.meatDecay, rng);

            grid.Clear();
            for (int i = 0; i < creatures.Count; i++) grid.Insert(creatures[i]);

            for (int i = 0; i < creatures.Count; i++)
            {
                Creature c = creatures[i];
                if (!c.alive) continue;
                Sense(c);
                c.Brain.Think();
                Act(c);
            }

            creatures.RemoveAll(IsDead);
            creatures.AddRange(births);
            births.Clear();
            stepping = false;

            if (Tick % 200 == 0) species.ResampleRepresentatives(creatures, rng);
            if (settings.autoSeed && Tick % 200 == 0) AutoSeed();
            if (Tick % 10 == 0) Latest = ComputeStats();
            if (Tick % history.Interval == 0) history.Add(ComputeStats());
        }

        void Sense(Creature c)
        {
            Genome g = c.genome;
            float[] s = c.Brain.State;
            int tile = map.IndexAt(c.x, c.y);

            s[BrainIO.Bias] = 1f;
            s[BrainIO.Energy] = c.EnergyFraction;
            s[BrainIO.Health] = c.HealthFraction;
            s[BrainIO.Age] = Math.Min(1f, c.age / (float)c.lifespan);
            s[BrainIO.FoodHere] = FoodValue(g, MouthTile(c));
            s[BrainIO.InWater] = TileInfo.IsWater(map.tiles[tile]) ? 1f : 0f;
            s[BrainIO.ClockFast] = MathF.Sin(c.age * (TwoPi / 40f));
            s[BrainIO.ClockSlow] = MathF.Sin(c.age * (TwoPi / 300f));
            s[BrainIO.Pain] = Math.Min(1f, c.pain * 0.2f);
            s[BrainIO.Speed] = c.velocity / settings.maxSpeed;
            c.pain = 0f;

            for (int k = BrainIO.SectorStart; k < BrainIO.InputCount; k++) s[k] = 0f;

            // Other creatures: vision (front half only), hearing and crowding (all around).
            float range = g.vision;
            float query = Math.Max(range, HearingRange);
            float cosH = MathF.Cos(c.heading), sinH = MathF.Sin(c.heading);
            grid.Query(c.x, c.y, query, neighbours);
            float heard = 0f;
            int crowd = 0;
            for (int n = 0; n < neighbours.Count; n++)
            {
                Creature o = neighbours[n];
                if (o == c || !o.alive) continue;
                float dx = o.x - c.x, dy = o.y - c.y;
                float d2 = dx * dx + dy * dy;
                if (d2 > query * query) continue;
                float d = MathF.Sqrt(d2);
                if (d < 4f) crowd++;
                if (d < HearingRange) heard += o.signal * (1f - d / HearingRange);
                if (d > range) continue;

                float fx = dx * cosH + dy * sinH;   // distance ahead
                float fy = -dx * sinH + dy * cosH;  // distance to the left
                if (fx < 0f) continue;
                int sector = SectorOf(fx, fy);

                float prox = 1f - d / range;
                int b = BrainIO.SectorStart + sector * BrainIO.PerSector;
                if (prox > s[b + BrainIO.SectorCreature])
                {
                    s[b + BrainIO.SectorCreature] = prox;
                    s[b + BrainIO.SectorKin] = o.species == c.species ? 1f : -1f;
                    s[b + BrainIO.SectorSize] = Clamp((o.genome.size - g.size) / (o.genome.size + g.size) * 3f, -1f, 1f);
                }
            }
            s[BrainIO.Hearing] = Clamp(heard, -1f, 1f);
            s[BrainIO.Crowding] = Math.Min(1f, crowd / 8f);

            // Terrain: sample food and water along one ray per sector.
            for (int k = 0; k < BrainIO.Sectors; k++)
            {
                float a = c.heading + (k - 2) * SectorWidth;
                float ca = MathF.Cos(a), sa = MathF.Sin(a);
                float food = 0f, water = 0f;
                for (int step = 1; step <= 3; step++)
                {
                    float dist = range * step / 3f;
                    float px = c.x + ca * dist, py = c.y + sa * dist;
                    if (!map.InBounds(px, py))
                    {
                        water += 1f;
                        continue;
                    }
                    int idx = map.IndexAt(px, py);
                    food += FoodValue(g, idx);
                    if (TileInfo.IsWater(map.tiles[idx])) water += 1f;
                }
                int b = BrainIO.SectorStart + k * BrainIO.PerSector;
                s[b + BrainIO.SectorFood] = food / 3f;
                s[b + BrainIO.SectorWater] = water / 3f;
            }
        }

        // tan(18 degrees) and tan(54 degrees): boundaries between the five 36-degree vision sectors.
        const float TanInner = 0.3249197f;
        const float TanOuter = 1.3763819f;

        /// <summary>Vision sector (0 = right ... 4 = left) of a point ahead of the creature, without atan2.</summary>
        static int SectorOf(float ahead, float left)
        {
            if (left >= 0f)
            {
                if (left <= ahead * TanInner) return 2;
                return left <= ahead * TanOuter ? 3 : 4;
            }
            float right = -left;
            if (right <= ahead * TanInner) return 2;
            return right <= ahead * TanOuter ? 1 : 0;
        }

        /// <summary>The tile just in front of a creature, where it eats from.</summary>
        int MouthTile(Creature c)
        {
            float reach = c.radius + 0.3f;
            return map.IndexAt(c.x + MathF.Cos(c.heading) * reach, c.y + MathF.Sin(c.heading) * reach);
        }

        /// <summary>How much food on a tile this particular creature can digest, 0..1.</summary>
        float FoodValue(Genome g, int tile)
        {
            float v = map.plants[tile] * (1f - g.diet) + Math.Min(map.meat[tile], 2f) * g.diet;
            return Math.Min(1f, v / 1.6f);
        }

        void Act(Creature c)
        {
            Genome g = c.genome;
            Brain brain = c.Brain;
            SimSettings s = settings;
            string hazard = null;

            float forward = brain.Output(BrainIO.OutForward);
            float turn = brain.Output(BrainIO.OutTurn);
            float eat = brain.Output(BrainIO.OutEat);
            float attack = brain.Output(BrainIO.OutAttack);
            float reproduce = brain.Output(BrainIO.OutReproduce);
            c.signal = brain.Output(BrainIO.OutSignal);

            // --- Movement ---
            c.heading += turn * s.maxTurn;
            if (c.heading > Pi) c.heading -= TwoPi;
            else if (c.heading < -Pi) c.heading += TwoPi;

            int tile = map.IndexAt(c.x, c.y);
            TileType type = map.tiles[tile];
            bool inWater = TileInfo.IsWater(type);
            float medium = inWater ? 0.25f + 0.75f * g.aquatic : 1f - 0.75f * g.aquatic;
            float topSpeed = s.maxSpeed * g.speed * medium * TileInfo.SpeedFactor[(int)type] / (0.7f + 0.3f * g.size);
            float targetSpeed = (forward > 0f ? forward : forward * 0.3f) * topSpeed;
            c.velocity += (targetSpeed - c.velocity) * 0.5f;
            c.x = Clamp(c.x + MathF.Cos(c.heading) * c.velocity, 0.01f, map.Width - 0.01f);
            c.y = Clamp(c.y + MathF.Sin(c.heading) * c.velocity, 0.01f, map.Height - 0.01f);

            // --- Upkeep: bigger bodies, faster movement, bigger brains and eyes all cost energy ---
            float speedRatio = c.velocity / s.maxSpeed;
            c.energy -= s.baseMetabolism
                        + s.sizeMetabolism * g.size
                        + s.moveCost * speedRatio * speedRatio * (0.5f + 0.5f * g.size)
                        + s.brainCost * (brain.HiddenCount * 2 + brain.synapses.Count)
                        + s.visionCost * g.vision;

            tile = map.IndexAt(c.x, c.y);
            type = map.tiles[tile];
            inWater = TileInfo.IsWater(type);

            // --- Eating: plants for herbivores, meat for carnivores, a mix for omnivores ---
            if (eat > 0f)
            {
                int mouth = MouthTile(c);
                float bite = s.biteSize * g.size * eat;
                float gain = 0f;
                float plantBite = bite * (1f - g.diet);
                if (plantBite > 0f)
                {
                    float take = Math.Min(map.plants[mouth], plantBite);
                    map.plants[mouth] -= take;
                    gain += take * s.plantEnergy;
                }
                float meatBite = bite * g.diet * 1.5f;
                if (meatBite > 0f)
                {
                    float take = Math.Min(map.meat[mouth], meatBite);
                    map.meat[mouth] -= take;
                    gain += take * s.meatEnergy;
                }
                c.energy += gain;
                c.energyEaten += gain;
            }

            // --- Attacking whatever is right in front ---
            if (c.attackCooldown > 0)
            {
                c.attackCooldown--;
            }
            else if (attack > 0.2f)
            {
                c.attackCooldown = AttackCooldownTicks;
                c.energy -= s.attackCost * g.size;
                Creature target = FindAttackTarget(c);
                if (target != null)
                {
                    // Hunters have the teeth: grazers can only fight back weakly.
                    float damage = s.attackDamage * (0.5f + g.size) * (0.1f + 0.9f * g.diet) * attack;
                    target.health -= damage;
                    target.pain += damage;
                    c.lastAttackTick = Tick;
                    c.lastTargetX = target.x;
                    c.lastTargetY = target.y;
                    if (target.health <= 0f && target.alive)
                    {
                        Kill(target, "Killed by " + Article(c.species.name), MouthTile(c));
                        c.kills++;
                    }
                }
            }

            // --- Environment ---
            float envDamage = 0f;
            if (type == TileType.DeepWater && g.aquatic < 0.35f)
            {
                envDamage += (0.35f - g.aquatic) * 1.5f;
                hazard = "Drowned";
            }
            if (!inWater && g.aquatic > 0.65f)
            {
                envDamage += (g.aquatic - 0.65f) * 1.5f;
                hazard = "Stranded on land";
            }
            if (map.fire[tile] > 0f)
            {
                envDamage += 3f * map.fire[tile];
                hazard = "Burned";
            }
            if (envDamage > 0f)
            {
                c.health -= envDamage;
                c.pain += envDamage;
            }

            // --- Energy, healing, ageing ---
            if (c.energy > c.maxEnergy) c.energy = c.maxEnergy;
            if (c.energy <= 0f)
            {
                c.energy = 0f;
                c.health -= 0.4f;
                hazard = "Starved";
            }
            else if (c.health < c.maxHealth)
            {
                float heal = Math.Min(0.08f, c.maxHealth - c.health);
                c.health += heal;
                c.energy -= heal * 0.5f;
            }

            c.age++;
            if (c.age > c.lifespan)
            {
                c.health -= 0.25f;
                hazard = "Old age";
            }
            if (c.reproduceCooldown > 0) c.reproduceCooldown--;

            if (c.health <= 0f)
            {
                Kill(c, hazard ?? "Wounds");
                return;
            }

            // --- Reproduction ---
            if (reproduce > 0f && c.age >= s.maturityAge && c.reproduceCooldown <= 0
                && creatures.Count + births.Count < s.maxPopulation)
            {
                float childEnergy = c.maxEnergy * g.offspringShare;
                if (c.energy >= childEnergy * 1.25f + c.maxEnergy * 0.1f) Reproduce(c, childEnergy);
            }
        }

        Creature FindAttackTarget(Creature c)
        {
            // Uses the neighbour list filled by Sense() for this creature.
            // Only other species within a 45 degree cone in front can be bitten, so a hunter
            // never mauls its own newborns (which appear right behind it).
            float cosH = MathF.Cos(c.heading), sinH = MathF.Sin(c.heading);
            Creature best = null;
            float bestD = float.MaxValue;
            for (int n = 0; n < neighbours.Count; n++)
            {
                Creature o = neighbours[n];
                if (o == c || !o.alive || o.species == c.species) continue;
                float dx = o.x - c.x, dy = o.y - c.y;
                float reach = c.radius + o.radius + 0.8f;
                float d2 = dx * dx + dy * dy;
                if (d2 > reach * reach || d2 >= bestD) continue;
                float ahead = dx * cosH + dy * sinH;
                if (ahead <= 0f || ahead * ahead < 0.5f * d2) continue;
                bestD = d2;
                best = o;
            }
            return best;
        }

        void Reproduce(Creature parent, float childEnergy)
        {
            Genome g = parent.genome.Clone();
            g.Mutate(rng);

            Species sp = parent.species;
            if (Genome.Distance(g, sp.representative) > settings.speciesThreshold)
                sp = species.Create(g, parent.species, Tick, parent.generation + 1, rng);

            float bx = parent.x - MathF.Cos(parent.heading) * parent.radius * 2f;
            float by = parent.y - MathF.Sin(parent.heading) * parent.radius * 2f;
            Creature child = AddCreature(g, sp, bx, by, parent.generation + 1);
            child.parentId = parent.id;
            child.energy = Math.Min(childEnergy, child.maxEnergy);
            child.bodyEnergy = childEnergy;

            parent.energy -= childEnergy * 1.25f;
            parent.children++;
            parent.reproduceCooldown = settings.reproduceCooldown;
            TotalBirths++;
        }

        Creature AddCreature(Genome g, Species sp, float x, float y, int generation)
        {
            var c = new Creature(nextId++, g);
            c.x = Clamp(x, 0.01f, map.Width - 0.01f);
            c.y = Clamp(y, 0.01f, map.Height - 0.01f);
            c.heading = rng.Range(-Pi, Pi);
            c.species = sp;
            c.generation = generation;
            species.OnBorn(sp);
            if (!sp.announced && sp.population >= AnnouncePopulation)
            {
                sp.announced = true;
                string origin = sp.parentName != null ? " evolved from " + sp.parentName : " is spreading";
                AddEvent(sp.name + origin + " (" + g.DietLabel.ToLowerInvariant() + WaterNote(g) + ")", sp.id);
            }
            if (generation > MaxGeneration) MaxGeneration = generation;
            if (stepping) births.Add(c);
            else creatures.Add(c);
            return c;
        }

        public void Kill(Creature c, string cause)
        {
            Kill(c, cause, -1);
        }

        /// <param name="meatTile">Where the body ends up (a predator's mouth), or -1 for where it fell.</param>
        void Kill(Creature c, string cause, int meatTile)
        {
            if (!c.alive) return;
            c.alive = false;
            c.causeOfDeath = cause;
            c.deathTick = Tick;
            int tile = meatTile >= 0 ? meatTile : map.IndexAt(c.x, c.y);
            // The body becomes meat. It returns less energy than went into the creature,
            // so predators can only live off what plants produced (no energy from nothing).
            map.meat[tile] += (0.8f * c.bodyEnergy + 0.9f * c.energy) / settings.meatEnergy;
            TotalDeaths++;
            string key = cause.StartsWith("Killed by", StringComparison.Ordinal) ? "Predation" : cause;
            int count;
            deathCauses.TryGetValue(key, out count);
            deathCauses[key] = count + 1;
            if (species.OnDied(c.species, Tick) && c.species.announced)
                AddEvent(c.species.name + " went extinct", c.species.id);
            Died?.Invoke(c);
        }

        // ------------------------------------------------------------------
        // Population seeding
        // ------------------------------------------------------------------

        void SeedPopulation()
        {
            int herbGroups = Math.Max(1, settings.initialHerbivores / 20);
            for (int i = 0; i < herbGroups; i++)
            {
                float x, y;
                FindFertileLand(out x, out y);
                SpawnNewSpecies(x, y, settings.initialHerbivores / herbGroups, rng.Range(0f, 0.15f), 4f, false);
            }
            if (settings.initialCarnivores > 0)
            {
                int carnGroups = Math.Max(1, settings.initialCarnivores / 8);
                for (int i = 0; i < carnGroups; i++)
                {
                    float x, y;
                    FindFertileLand(out x, out y);
                    SpawnNewSpecies(x, y, settings.initialCarnivores / carnGroups, rng.Range(0.8f, 1f), 4f, false);
                }
            }
        }

        void AutoSeed()
        {
            float x, y;
            if (creatures.Count < settings.autoSeedMinimum)
            {
                FindFertileLand(out x, out y);
                SpawnNewSpecies(x, y, 16, rng.Range(0f, 0.15f), 4f, true);
            }
            else if (Tick % 6000 == 0 && Latest.carnivores == 0 && Latest.population > 200 && settings.initialCarnivores > 0)
            {
                // Keep the food chain going: reintroduce a predator if every carnivore died out.
                FindFertileLand(out x, out y);
                SpawnNewSpecies(x, y, 8, rng.Range(0.8f, 1f), 4f, true);
            }
        }

        bool FindFertileLand(out float x, out float y)
        {
            for (int attempt = 0; attempt < 500; attempt++)
            {
                x = rng.Range(0f, map.Width);
                y = rng.Range(0f, map.Height);
                TileType t = map.TileAt(x, y);
                if (t == TileType.Grassland || t == TileType.Forest) return true;
            }
            x = map.Width * 0.5f;
            y = map.Height * 0.5f;
            return false;
        }

        // ------------------------------------------------------------------
        // God powers (called from the UI between ticks)
        // ------------------------------------------------------------------

        /// <summary>Create a brand-new species with a random brain. Spawned in water it becomes aquatic.</summary>
        public Species SpawnNewSpecies(float x, float y, int count, float diet, float spread, bool announce = true)
        {
            // Spawned in the sea, a species starts out aquatic.
            float aquatic = TileInfo.IsWater(map.TileAt(x, y)) ? rng.Range(0.8f, 1f) : rng.Range(0f, 0.15f);
            Genome founder = Genome.CreateRandom(rng, diet, aquatic, settings.startWithInstincts);
            Species sp = species.Create(founder, null, Tick, 0, rng);
            sp.announced = true;
            if (announce) AddEvent(sp.name + " appeared (" + founder.DietLabel.ToLowerInvariant() + WaterNote(founder) + ")", sp.id);
            for (int i = 0; i < count; i++)
            {
                Genome g = founder.Clone();
                if (i > 0) g.brain.Mutate(rng, 0.1f);
                Creature c = AddCreature(g, sp, x + rng.Gaussian() * spread, y + rng.Gaussian() * spread, 0);
                c.energy = c.maxEnergy * 0.7f;
                c.bodyEnergy = c.maxEnergy * g.offspringShare;
                c.age = rng.Range(0, settings.maturityAge);
            }
            return sp;
        }

        /// <summary>Spawn exact copies of an existing creature's genome.</summary>
        public void SpawnClones(Creature template, float x, float y, int count, float spread)
        {
            for (int i = 0; i < count; i++)
            {
                Creature c = AddCreature(template.genome.Clone(), template.species,
                    x + rng.Gaussian() * spread, y + rng.Gaussian() * spread, template.generation);
                c.energy = c.maxEnergy * 0.7f;
                c.bodyEnergy = c.maxEnergy * c.genome.offspringShare;
                c.age = rng.Range(0, settings.maturityAge);
            }
        }

        public int KillInRadius(float x, float y, float r, string cause)
        {
            int killed = 0;
            for (int i = 0; i < creatures.Count; i++)
            {
                Creature c = creatures[i];
                if (!c.alive) continue;
                float dx = c.x - x, dy = c.y - y;
                if (dx * dx + dy * dy > r * r) continue;
                Kill(c, cause);
                killed++;
            }
            return killed;
        }

        public void Lightning(float x, float y, float r)
        {
            KillInRadius(x, y, r, "Struck by lightning");
            IgniteArea(x, y, r * 0.7f, 1f);
        }

        public void Meteor(float x, float y, float r)
        {
            int killed = KillInRadius(x, y, r * 1.3f, "Hit by a meteor");
            map.ModifyHeight(x, y, r * 1.2f, -0.3f);
            IgniteArea(x, y, r * 1.8f, 0.35f);
            AddEvent("A meteor struck" + (killed > 0 ? ", killing " + killed : ""), -1);
        }

        public void IgniteArea(float x, float y, float r, float chance)
        {
            ForTilesInCircle(x, y, r, i =>
            {
                if (rng.Chance(chance)) map.Ignite(i);
            });
        }

        public void Rain(float x, float y, float r, float strength)
        {
            ForTilesInCircle(x, y, r, i =>
            {
                map.Extinguish(i);
                float cap = map.Capacity(i);
                map.plants[i] = Math.Min(cap * 1.5f, map.plants[i] + cap * 0.05f * strength);
            });
        }

        public void Bless(float x, float y, float r)
        {
            for (int i = 0; i < creatures.Count; i++)
            {
                Creature c = creatures[i];
                if (!c.alive) continue;
                float dx = c.x - x, dy = c.y - y;
                if (dx * dx + dy * dy > r * r) continue;
                c.energy = c.maxEnergy;
                c.health = c.maxHealth;
            }
        }

        public void ShapeLand(float x, float y, float r, float amount)
        {
            map.ModifyHeight(x, y, r, amount);
        }

        /// <summary>The living creature under a point, if any.</summary>
        public Creature CreatureAt(float x, float y, float tolerance)
        {
            Creature best = null;
            float bestD = float.MaxValue;
            for (int i = 0; i < creatures.Count; i++)
            {
                Creature c = creatures[i];
                if (!c.alive) continue;
                float dx = c.x - x, dy = c.y - y;
                float d2 = dx * dx + dy * dy;
                float reach = c.radius + tolerance;
                if (d2 <= reach * reach && d2 < bestD)
                {
                    bestD = d2;
                    best = c;
                }
            }
            return best;
        }

        /// <summary>The living member of a species with the most descendants-to-be (highest generation).</summary>
        public Creature FindMember(Species sp)
        {
            Creature best = null;
            for (int i = 0; i < creatures.Count; i++)
            {
                Creature c = creatures[i];
                if (c.alive && c.species == sp && (best == null || c.generation > best.generation)) best = c;
            }
            return best;
        }

        void ForTilesInCircle(float cx, float cy, float r, Action<int> action)
        {
            int x0 = Math.Max(0, (int)(cx - r)), x1 = Math.Min(map.Width - 1, (int)(cx + r));
            int y0 = Math.Max(0, (int)(cy - r)), y1 = Math.Min(map.Height - 1, (int)(cy + r));
            for (int y = y0; y <= y1; y++)
            {
                for (int x = x0; x <= x1; x++)
                {
                    float dx = x + 0.5f - cx, dy = y + 0.5f - cy;
                    if (dx * dx + dy * dy <= r * r) action(y * map.Width + x);
                }
            }
        }

        // ------------------------------------------------------------------
        // Stats & events
        // ------------------------------------------------------------------

        public StatsSample ComputeStats()
        {
            var st = new StatsSample { tick = Tick, species = species.AliveCount, maxGeneration = MaxGeneration };
            float size = 0f, speed = 0f, hidden = 0f, syn = 0f;
            for (int i = 0; i < creatures.Count; i++)
            {
                Creature c = creatures[i];
                if (!c.alive) continue;
                st.population++;
                float d = c.genome.diet;
                if (d < 0.35f) st.herbivores++;
                else if (d > 0.65f) st.carnivores++;
                else st.omnivores++;
                if (c.genome.aquatic > 0.5f) st.aquatic++;
                size += c.genome.size;
                speed += c.genome.speed;
                hidden += c.Brain.HiddenCount;
                syn += c.Brain.synapses.Count;
            }
            if (st.population > 0)
            {
                float n = st.population;
                st.avgSize = size / n;
                st.avgSpeed = speed / n;
                st.avgHidden = hidden / n;
                st.avgSynapses = syn / n;
            }
            return st;
        }

        void AddEvent(string text, int speciesId)
        {
            events.Add(new WorldEvent(Tick, text, speciesId));
            if (events.Count > MaxEvents) events.RemoveAt(0);
            EventSerial++;
        }

        static string WaterNote(Genome g)
        {
            return g.aquatic > 0.5f ? ", aquatic" : "";
        }

        static string Article(string name)
        {
            if (string.IsNullOrEmpty(name)) return "a creature";
            char f = char.ToUpperInvariant(name[0]);
            bool vowel = f == 'A' || f == 'E' || f == 'I' || f == 'O' || f == 'U';
            return (vowel ? "an " : "a ") + name;
        }

        static float Clamp(float v, float min, float max)
        {
            return v < min ? min : (v > max ? max : v);
        }
    }
}
