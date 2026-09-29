using UnityEngine;

namespace NeuronWorld
{
    /// <summary>
    /// Draws a creature's brain into a texture: inputs on the left, hidden neurons in the middle,
    /// outputs on the right. Node colours follow Neuron-Box's network UI (inputs black to white,
    /// hidden red to green, outputs cyan to yellow); connections are green (excitatory) or red
    /// (inhibitory), brighter when a signal is flowing through them.
    /// </summary>
    public sealed class BrainView
    {
        public const int Width = 296;
        public const int Height = 260;
        const int Margin = 10;

        static readonly Color32 Background = new Color32(14, 17, 24, 255);

        public readonly Texture2D texture;
        readonly Color32[] pixels = new Color32[Width * Height];
        Vector2[] positions = new Vector2[0];

        public BrainView()
        {
            texture = new Texture2D(Width, Height, TextureFormat.RGBA32, false)
            {
                name = "Brain view",
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp
            };
        }

        /// <summary>Node position in GUI space (origin top-left of the texture).</summary>
        public Vector2 NodePosition(int neuron)
        {
            return neuron >= 0 && neuron < positions.Length ? positions[neuron] : Vector2.zero;
        }

        public int NodeCount { get { return positions.Length; } }

        void Layout(Brain brain)
        {
            int n = brain.NeuronCount;
            if (positions.Length != n) positions = new Vector2[n];

            int inputs = BrainIO.InputCount;
            float inputStep = (Height - 2f * Margin) / (inputs - 1);
            for (int i = 0; i < inputs; i++) positions[i] = new Vector2(Margin, Margin + i * inputStep);

            int outputs = BrainIO.OutputCount;
            float outputStep = (Height - 2f * Margin - 40f) / (outputs - 1);
            for (int o = 0; o < outputs; o++)
                positions[Brain.FirstOutput + o] = new Vector2(Width - Margin - 4, Margin + 20f + o * outputStep);

            int hidden = brain.HiddenCount;
            var center = new Vector2(Width * 0.5f, Height * 0.5f);
            if (hidden == 1)
            {
                positions[Brain.FirstHidden] = center;
            }
            else
            {
                // Hidden neurons on a ring (two rings when there are many).
                for (int h = 0; h < hidden; h++)
                {
                    bool inner = hidden > 10 && h % 2 == 1;
                    float radius = inner ? 45f : 85f;
                    float angle = (h / (float)hidden) * Mathf.PI * 2f + 0.3f;
                    positions[Brain.FirstHidden + h] = center + new Vector2(Mathf.Cos(angle) * radius * 0.8f, Mathf.Sin(angle) * radius);
                }
            }
        }

        public void Render(Brain brain)
        {
            Layout(brain);
            for (int i = 0; i < pixels.Length; i++) pixels[i] = Background;

            float[] state = brain.State;
            var synapses = brain.synapses;
            for (int k = 0; k < synapses.Count; k++)
            {
                Synapse s = synapses[k];
                float strength = Mathf.Clamp01(Mathf.Abs(s.weight) / 3f);
                float activity = Mathf.Clamp01(Mathf.Abs(state[s.from]));
                float alpha = 0.12f + 0.28f * strength + 0.6f * activity * strength;
                Color32 c = s.weight >= 0f ? new Color32(80, 220, 110, 255) : new Color32(235, 80, 70, 255);
                DrawLine(positions[s.from], positions[s.to], c, alpha);
            }

            for (int i = 0; i < brain.NeuronCount; i++)
            {
                float a = state[i];
                Color c;
                float radius;
                if (i < Brain.FirstOutput)
                {
                    c = a >= 0f ? Color.Lerp(new Color(0.12f, 0.12f, 0.14f), Color.white, a)
                                : Color.Lerp(new Color(0.12f, 0.12f, 0.14f), new Color(0.75f, 0.45f, 1f), -a);
                    radius = 2.6f;
                }
                else if (i < Brain.FirstHidden)
                {
                    c = Color.Lerp(Color.cyan, Color.yellow, (a + 1f) * 0.5f);
                    radius = 6f;
                }
                else
                {
                    c = Color.Lerp(new Color(0.9f, 0.2f, 0.2f), new Color(0.2f, 0.95f, 0.3f), (a + 1f) * 0.5f);
                    radius = 5f;
                }
                DrawDisc(positions[i], radius + 1.2f, new Color32(0, 0, 0, 255));
                DrawDisc(positions[i], radius, c);
            }

            texture.SetPixels32(pixels);
            texture.Apply(false);
        }

        /// <summary>Index of the neuron under a point in texture GUI space, or -1.</summary>
        public int NeuronAt(Vector2 p)
        {
            int best = -1;
            float bestD = 7f * 7f;
            for (int i = 0; i < positions.Length; i++)
            {
                float d = (positions[i] - p).sqrMagnitude;
                if (d < bestD)
                {
                    bestD = d;
                    best = i;
                }
            }
            return best;
        }

        void Blend(int x, int y, Color32 c, float alpha)
        {
            if (x < 0 || y < 0 || x >= Width || y >= Height) return;
            int i = (Height - 1 - y) * Width + x; // GUI space is y-down, textures are y-up
            Color32 dst = pixels[i];
            pixels[i] = new Color32(
                (byte)(dst.r + (c.r - dst.r) * alpha),
                (byte)(dst.g + (c.g - dst.g) * alpha),
                (byte)(dst.b + (c.b - dst.b) * alpha),
                255);
        }

        void DrawLine(Vector2 a, Vector2 b, Color32 c, float alpha)
        {
            float dx = b.x - a.x, dy = b.y - a.y;
            int steps = Mathf.Max(1, Mathf.CeilToInt(Mathf.Max(Mathf.Abs(dx), Mathf.Abs(dy))));
            if (steps < 2)
            {
                // Self-connection: draw a small loop marker above the node.
                DrawDisc(a + new Vector2(0f, -7f), 2f, c);
                return;
            }
            for (int s = 0; s <= steps; s++)
            {
                float t = s / (float)steps;
                Blend(Mathf.RoundToInt(a.x + dx * t), Mathf.RoundToInt(a.y + dy * t), c, alpha);
            }
        }

        void DrawDisc(Vector2 center, float radius, Color c)
        {
            Color32 c32 = c;
            int x0 = Mathf.FloorToInt(center.x - radius), x1 = Mathf.CeilToInt(center.x + radius);
            int y0 = Mathf.FloorToInt(center.y - radius), y1 = Mathf.CeilToInt(center.y + radius);
            for (int y = y0; y <= y1; y++)
            {
                for (int x = x0; x <= x1; x++)
                {
                    float d = Mathf.Sqrt((x - center.x) * (x - center.x) + (y - center.y) * (y - center.y));
                    float cover = Mathf.Clamp01(radius + 0.5f - d);
                    if (cover > 0f) Blend(x, y, c32, cover);
                }
            }
        }
    }
}
