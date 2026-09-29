using System;
using System.Collections.Generic;

namespace NeuronWorld
{
    /// <summary>
    /// The tile world: terrain height, biomes, plant food, meat (corpses) and fire.
    /// Tile (x, y) covers world space [x, x+1) x [y, y+1).
    /// </summary>
    public sealed class WorldMap
    {
        public readonly int Width;
        public readonly int Height;
        public readonly int Size;

        public readonly float[] height;
        public readonly float[] moisture;
        public readonly TileType[] tiles;
        public readonly float[] plants;
        public readonly float[] meat;
        public readonly float[] fire;

        readonly float[] capacity;
        readonly float[] growth;
        readonly List<int> burning = new List<int>();
        readonly List<int> burningNext = new List<int>();
        readonly bool[] listedBurning;

        /// <summary>Incremented whenever terrain changes, so renderers know to rebuild shading.</summary>
        public int TerrainVersion { get; private set; }

        public int BurningCount { get { return burning.Count; } }

        public WorldMap(int width, int height)
        {
            Width = Math.Max(16, width);
            Height = Math.Max(16, height);
            Size = Width * Height;
            this.height = new float[Size];
            moisture = new float[Size];
            tiles = new TileType[Size];
            plants = new float[Size];
            meat = new float[Size];
            fire = new float[Size];
            capacity = new float[Size];
            growth = new float[Size];
            listedBurning = new bool[Size];
        }

        public void Generate(int seed)
        {
            var elevation = new Noise(seed);
            var wetness = new Noise(seed * 31 + 7);
            var rng = new Rng(seed ^ 0x5bd1e995);
            float ox = rng.Range(0f, 1000f), oy = rng.Range(0f, 1000f);
            float mx = rng.Range(0f, 1000f), my = rng.Range(0f, 1000f);

            for (int y = 0; y < Height; y++)
            {
                for (int x = 0; x < Width; x++)
                {
                    int i = y * Width + x;
                    float e = elevation.Fbm(x / 70f + ox, y / 70f + oy, 5);
                    float h = 0.52f + e * 0.75f;

                    // Push the map edges down into ocean so the world reads as continents and islands.
                    float dx = Math.Abs(x / (float)(Width - 1) * 2f - 1f);
                    float dy = Math.Abs(y / (float)(Height - 1) * 2f - 1f);
                    float edge = Math.Max(dx, dy);
                    float falloff = SmoothStep(0.62f, 1.0f, edge);
                    h = h * (1f - falloff) + 0.15f * falloff;

                    height[i] = Clamp01(h);
                    moisture[i] = Clamp01(0.5f + wetness.Fbm(x / 55f + mx, y / 55f + my, 4) * 0.9f);
                }
            }

            for (int i = 0; i < Size; i++)
            {
                Reclassify(i);
                plants[i] = capacity[i] * rng.Range(0.5f, 1f);
                meat[i] = 0f;
                fire[i] = 0f;
                listedBurning[i] = false;
            }
            burning.Clear();
            TerrainVersion++;
        }

        void Reclassify(int i)
        {
            TileType t = TileInfo.Classify(height[i], moisture[i]);
            tiles[i] = t;
            capacity[i] = TileInfo.PlantCapacity[(int)t];
            growth[i] = TileInfo.PlantGrowth[(int)t];
            if (TileInfo.IsWater(t)) fire[i] = 0f;
        }

        public float Capacity(int i)
        {
            return capacity[i];
        }

        public bool InBounds(float x, float y)
        {
            return x >= 0f && y >= 0f && x < Width && y < Height;
        }

        public int IndexAt(float x, float y)
        {
            int ix = (int)x;
            int iy = (int)y;
            if (ix < 0) ix = 0; else if (ix >= Width) ix = Width - 1;
            if (iy < 0) iy = 0; else if (iy >= Height) iy = Height - 1;
            return iy * Width + ix;
        }

        public TileType TileAt(float x, float y)
        {
            return tiles[IndexAt(x, y)];
        }

        /// <summary>Advance plants, meat and fire by one tick.</summary>
        public void Step(float plantGrowthMultiplier, float meatDecay, Rng rng)
        {
            float keepMeat = 1f - meatDecay;
            for (int i = 0; i < Size; i++)
            {
                float cap = capacity[i];
                float p = plants[i];
                if (p < cap)
                {
                    plants[i] = p + growth[i] * plantGrowthMultiplier * (cap - p);
                }
                else if (p > cap)
                {
                    // Surplus from rain slowly dies back.
                    plants[i] = p - (p - cap) * 0.01f;
                }

                float m = meat[i];
                if (m > 0f)
                {
                    m *= keepMeat;
                    meat[i] = m < 0.01f ? 0f : m;
                }
            }
            StepFire(rng);
        }

        void StepFire(Rng rng)
        {
            if (burning.Count == 0) return;
            burningNext.Clear();
            for (int b = 0; b < burning.Count; b++)
            {
                int i = burning[b];
                if (fire[i] <= 0f)
                {
                    fire[i] = 0f;
                    listedBurning[i] = false;
                    continue;
                }

                float p = plants[i];
                if (p > 0.02f)
                {
                    plants[i] = Math.Max(0f, p - 0.03f);
                    fire[i] = Math.Min(1f, fire[i] + 0.05f);
                }
                else
                {
                    fire[i] -= 0.05f;
                }
                meat[i] *= 0.95f;

                if (fire[i] <= 0f)
                {
                    fire[i] = 0f;
                    listedBurning[i] = false;
                    continue;
                }
                burningNext.Add(i);

                int x = i % Width;
                int y = i / Width;
                TrySpread(x + 1, y, fire[i], rng);
                TrySpread(x - 1, y, fire[i], rng);
                TrySpread(x, y + 1, fire[i], rng);
                TrySpread(x, y - 1, fire[i], rng);
            }
            burning.Clear();
            burning.AddRange(burningNext);
        }

        void TrySpread(int x, int y, float intensity, Rng rng)
        {
            if (x < 0 || y < 0 || x >= Width || y >= Height) return;
            int j = y * Width + x;
            if (listedBurning[j]) return;
            if (!TileInfo.Flammable[(int)tiles[j]]) return;
            float fuel = plants[j];
            if (fuel < 0.2f) return;
            if (rng.Chance(0.035f * fuel * intensity))
            {
                fire[j] = 0.3f;
                listedBurning[j] = true;
                burningNext.Add(j);
            }
        }

        public void Ignite(int i)
        {
            if (TileInfo.IsWater(tiles[i])) return;
            fire[i] = Math.Max(fire[i], 0.6f);
            if (!listedBurning[i])
            {
                listedBurning[i] = true;
                burning.Add(i);
            }
        }

        public void Extinguish(int i)
        {
            fire[i] = 0f; // removed from the burning list on the next fire step
        }

        /// <summary>Raise (positive) or lower (negative) terrain in a soft circle.</summary>
        public void ModifyHeight(float cx, float cy, float radius, float amount)
        {
            int x0 = Math.Max(0, (int)(cx - radius)), x1 = Math.Min(Width - 1, (int)(cx + radius));
            int y0 = Math.Max(0, (int)(cy - radius)), y1 = Math.Min(Height - 1, (int)(cy + radius));
            bool changed = false;
            for (int y = y0; y <= y1; y++)
            {
                for (int x = x0; x <= x1; x++)
                {
                    float dx = x + 0.5f - cx, dy = y + 0.5f - cy;
                    float d = MathF.Sqrt(dx * dx + dy * dy);
                    if (d > radius) continue;
                    float f = 1f - d / radius;
                    f = f * f * (3f - 2f * f);
                    int i = y * Width + x;
                    height[i] = Clamp01(height[i] + amount * f);
                    Reclassify(i);
                    if (plants[i] > capacity[i]) plants[i] = capacity[i];
                    changed = true;
                }
            }
            if (changed) TerrainVersion++;
        }

        public static float Clamp01(float v)
        {
            return v < 0f ? 0f : (v > 1f ? 1f : v);
        }

        static float SmoothStep(float a, float b, float x)
        {
            float t = Clamp01((x - a) / (b - a));
            return t * t * (3f - 2f * t);
        }
    }
}
