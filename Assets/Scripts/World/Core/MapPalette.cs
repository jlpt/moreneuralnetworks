using System;

namespace NeuronWorld
{
    /// <summary>
    /// Turns the map into RGBA pixels (one per tile): terrain colour blended with plant cover,
    /// meat and fire, with relief shading on land. Engine independent, so the Unity renderer and
    /// the headless tool's PNG previews look the same.
    /// </summary>
    public static class MapPalette
    {
        struct Rgb
        {
            public readonly float r, g, b;

            public Rgb(float r, float g, float b)
            {
                this.r = r;
                this.g = g;
                this.b = b;
            }

            public static Rgb Lerp(Rgb a, Rgb b, float t)
            {
                if (t < 0f) t = 0f; else if (t > 1f) t = 1f;
                return new Rgb(a.r + (b.r - a.r) * t, a.g + (b.g - a.g) * t, a.b + (b.b - a.b) * t);
            }
        }

        static readonly Rgb DeepLow = new Rgb(12, 36, 88);
        static readonly Rgb DeepHigh = new Rgb(24, 68, 138);
        static readonly Rgb ShallowLow = new Rgb(36, 96, 166);
        static readonly Rgb ShallowHigh = new Rgb(72, 142, 196);
        static readonly Rgb Algae = new Rgb(34, 118, 110);
        static readonly Rgb Beach = new Rgb(222, 206, 150);
        static readonly Rgb BeachGreen = new Rgb(186, 198, 122);
        static readonly Rgb GrassDry = new Rgb(178, 164, 98);
        static readonly Rgb GrassLush = new Rgb(84, 158, 60);
        static readonly Rgb ForestDry = new Rgb(124, 118, 74);
        static readonly Rgb ForestLush = new Rgb(32, 102, 46);
        static readonly Rgb Desert = new Rgb(222, 190, 118);
        static readonly Rgb DesertGreen = new Rgb(186, 178, 106);
        static readonly Rgb HillsDry = new Rgb(152, 132, 98);
        static readonly Rgb HillsLush = new Rgb(104, 132, 76);
        static readonly Rgb Mountain = new Rgb(126, 118, 112);
        static readonly Rgb Snow = new Rgb(236, 240, 246);
        static readonly Rgb Meat = new Rgb(122, 20, 26);

        /// <summary>Relief shading from the height gradient, lit from the top-left. Recompute when terrain changes.</summary>
        public static void ComputeShade(WorldMap map, float[] shade)
        {
            int w = map.Width, h = map.Height;
            float[] height = map.height;
            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    float dx = height[y * w + Math.Min(w - 1, x + 1)] - height[y * w + Math.Max(0, x - 1)];
                    float dy = height[Math.Min(h - 1, y + 1) * w + x] - height[Math.Max(0, y - 1) * w + x];
                    float s = 1f + (dx - dy) * 8f;
                    shade[y * w + x] = s < 0.72f ? 0.72f : (s > 1.28f ? 1.28f : s);
                }
            }
        }

        /// <summary>Writes RGBA32 pixels, row by row from the bottom (Unity texture order).</summary>
        public static void Fill(WorldMap map, float[] shade, byte[] rgba, int frame)
        {
            float[] height = map.height;
            float[] plants = map.plants;
            float[] meat = map.meat;
            float[] fire = map.fire;
            TileType[] tiles = map.tiles;

            for (int i = 0; i < map.Size; i++)
            {
                TileType t = tiles[i];
                float cap = map.Capacity(i);
                // Square root so partly grazed land still reads as green.
                float lush = cap > 0f ? MathF.Sqrt(Math.Max(0f, plants[i] / cap)) : 0f;
                float h = height[i];
                Rgb c;
                switch (t)
                {
                    case TileType.DeepWater:
                        c = Rgb.Lerp(Rgb.Lerp(DeepLow, DeepHigh, h / TileInfo.DeepWaterLevel), Algae, lush * 0.2f);
                        break;
                    case TileType.ShallowWater:
                        c = Rgb.Lerp(ShallowLow, ShallowHigh,
                            (h - TileInfo.DeepWaterLevel) / (TileInfo.WaterLevel - TileInfo.DeepWaterLevel));
                        c = Rgb.Lerp(c, Algae, lush * 0.3f);
                        break;
                    case TileType.Beach: c = Rgb.Lerp(Beach, BeachGreen, lush); break;
                    case TileType.Grassland: c = Rgb.Lerp(GrassDry, GrassLush, lush); break;
                    case TileType.Forest: c = Rgb.Lerp(ForestDry, ForestLush, lush); break;
                    case TileType.Desert: c = Rgb.Lerp(Desert, DesertGreen, lush); break;
                    case TileType.Hills: c = Rgb.Lerp(HillsDry, HillsLush, lush); break;
                    case TileType.Mountain: c = Mountain; break;
                    default: c = Snow; break;
                }

                if (t > TileType.ShallowWater)
                {
                    float s = shade[i];
                    c = new Rgb(c.r * s, c.g * s, c.b * s);
                }

                float m = meat[i];
                if (m > 0.05f) c = Rgb.Lerp(c, Meat, Math.Min(1f, m / 2.5f) * 0.75f);

                float f = fire[i];
                if (f > 0f)
                {
                    // Cheap per-tile flicker.
                    uint hash = (uint)i * 2654435761u + (uint)frame * 40503u;
                    float flicker = ((hash >> 16) & 255) / 255f;
                    c = Rgb.Lerp(c, new Rgb(255f, 90f + flicker * 140f, 20f), Math.Min(1f, 0.45f + f));
                }

                int p = i * 4;
                rgba[p] = ToByte(c.r);
                rgba[p + 1] = ToByte(c.g);
                rgba[p + 2] = ToByte(c.b);
                rgba[p + 3] = 255;
            }
        }

        static byte ToByte(float v)
        {
            return v <= 0f ? (byte)0 : (v >= 255f ? (byte)255 : (byte)v);
        }
    }
}
