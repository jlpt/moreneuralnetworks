using System.Collections.Generic;
using UnityEngine;

namespace NeuronWorld
{
    /// <summary>Population-over-time chart: total, herbivores, omnivores, carnivores and sea creatures.</summary>
    public sealed class GraphView
    {
        public const int Width = 312;
        public const int Height = 110;

        public static readonly Color TotalColor = new Color(0.92f, 0.92f, 0.95f);
        public static readonly Color HerbivoreColor = new Color(0.35f, 0.9f, 0.3f);
        public static readonly Color OmnivoreColor = new Color(1f, 0.85f, 0.2f);
        public static readonly Color CarnivoreColor = new Color(0.95f, 0.25f, 0.2f);
        public static readonly Color AquaticColor = new Color(0.35f, 0.65f, 1f);

        static readonly Color32 Background = new Color32(14, 17, 24, 255);
        static readonly Color32 GridColor = new Color32(40, 46, 58, 255);

        public readonly Texture2D texture;
        readonly Color32[] pixels = new Color32[Width * Height];
        int renderedCount = -1;
        int renderedTick = -1;

        public int MaxValue { get; private set; }

        public GraphView()
        {
            texture = new Texture2D(Width, Height, TextureFormat.RGBA32, false)
            {
                name = "Population graph",
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp
            };
        }

        public void Render(List<StatsSample> samples, bool force)
        {
            int lastTick = samples.Count > 0 ? samples[samples.Count - 1].tick : 0;
            if (!force && samples.Count == renderedCount && lastTick == renderedTick) return;
            renderedCount = samples.Count;
            renderedTick = lastTick;

            for (int i = 0; i < pixels.Length; i++) pixels[i] = Background;
            for (int g = 1; g < 4; g++)
            {
                int y = g * Height / 4;
                for (int x = 0; x < Width; x++) pixels[y * Width + x] = GridColor;
            }

            int max = 10;
            foreach (StatsSample s in samples) if (s.population > max) max = s.population;
            MaxValue = max;

            if (samples.Count >= 2)
            {
                Plot(samples, max, s => s.aquatic, AquaticColor);
                Plot(samples, max, s => s.carnivores, CarnivoreColor);
                Plot(samples, max, s => s.omnivores, OmnivoreColor);
                Plot(samples, max, s => s.herbivores, HerbivoreColor);
                Plot(samples, max, s => s.population, TotalColor);
            }

            texture.SetPixels32(pixels);
            texture.Apply(false);
        }

        void Plot(List<StatsSample> samples, int max, System.Func<StatsSample, int> value, Color color)
        {
            Color32 c = color;
            int prevX = 0, prevY = 0;
            for (int i = 0; i < samples.Count; i++)
            {
                int x = Mathf.RoundToInt(i / (float)(samples.Count - 1) * (Width - 1));
                int y = Mathf.RoundToInt(value(samples[i]) / (float)max * (Height - 3)) + 1;
                if (i > 0) Line(prevX, prevY, x, y, c);
                prevX = x;
                prevY = y;
            }
        }

        void Line(int x0, int y0, int x1, int y1, Color32 c)
        {
            int steps = Mathf.Max(Mathf.Abs(x1 - x0), Mathf.Abs(y1 - y0));
            if (steps == 0)
            {
                Set(x0, y0, c);
                return;
            }
            for (int s = 0; s <= steps; s++)
            {
                float t = s / (float)steps;
                int x = Mathf.RoundToInt(x0 + (x1 - x0) * t);
                int y = Mathf.RoundToInt(y0 + (y1 - y0) * t);
                Set(x, y, c);
                Set(x, y + 1, c);
            }
        }

        void Set(int x, int y, Color32 c)
        {
            if (x < 0 || y < 0 || x >= Width || y >= Height) return;
            pixels[y * Width + x] = c;
        }
    }
}
