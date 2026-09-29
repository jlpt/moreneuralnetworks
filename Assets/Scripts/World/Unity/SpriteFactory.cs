using System;
using UnityEngine;

namespace NeuronWorld
{
    /// <summary>
    /// Builds all sprites the world needs at runtime, so the scene has no art dependencies.
    /// Creature sprites are white with a dark outline and face +x; SpriteRenderer.color tints them.
    /// </summary>
    public static class SpriteFactory
    {
        const int BodySize = 64;

        static Sprite roundBody, hunterBody, fishBody, circle, ring, square;

        /// <summary>An unlit sprite material, so colours show exactly regardless of 2D lights.</summary>
        public static Material CreateSpriteMaterial()
        {
            Shader shader = Shader.Find("Universal Render Pipeline/2D/Sprite-Unlit-Default");
            if (shader == null) shader = Shader.Find("Sprites/Default");
            return shader != null ? new Material(shader) : null;
        }

        public static Sprite BodyFor(Genome g)
        {
            if (g.aquatic > 0.6f) return FishBody;
            if (g.diet > 0.6f) return HunterBody;
            return RoundBody;
        }

        public static Sprite RoundBody
        {
            get
            {
                if (roundBody == null)
                {
                    roundBody = MakeBody("Round body",
                        (x, y) => Sq(x - 32f) + Sq(y - 32f) <= Sq(27f),
                        new Vector2(44f, 40f), new Vector2(44f, 24f));
                }
                return roundBody;
            }
        }

        public static Sprite HunterBody
        {
            get
            {
                if (hunterBody == null)
                {
                    hunterBody = MakeBody("Hunter body",
                        (x, y) => Sq(x - 26f) + Sq(y - 32f) <= Sq(21f)
                                  || (x >= 26f && x <= 61f && Math.Abs(y - 32f) <= 19f * (61f - x) / 35f),
                        new Vector2(36f, 41f), new Vector2(36f, 23f));
                }
                return hunterBody;
            }
        }

        public static Sprite FishBody
        {
            get
            {
                if (fishBody == null)
                {
                    fishBody = MakeBody("Fish body",
                        (x, y) => Sq((x - 36f) / 25f) + Sq((y - 32f) / 14f) <= 1f
                                  || (x >= 3f && x <= 15f && Math.Abs(y - 32f) <= 2f + (15f - x) * 1.1f),
                        new Vector2(50f, 36f));
                }
                return fishBody;
            }
        }

        /// <summary>Soft filled circle (effects).</summary>
        public static Sprite Circle
        {
            get
            {
                if (circle == null)
                {
                    circle = MakeShape("Circle", 64, (x, y) =>
                    {
                        float d = Mathf.Sqrt(Sq(x - 32f) + Sq(y - 32f));
                        return Mathf.Clamp01(31.5f - d);
                    });
                }
                return circle;
            }
        }

        /// <summary>Thin ring (selection and brush outlines).</summary>
        public static Sprite Ring
        {
            get
            {
                if (ring == null)
                {
                    ring = MakeShape("Ring", 128, (x, y) =>
                    {
                        float d = Mathf.Sqrt(Sq(x - 64f) + Sq(y - 64f));
                        return Mathf.Clamp01(2.5f - Math.Abs(d - 61f));
                    });
                }
                return ring;
            }
        }

        public static Sprite Square
        {
            get
            {
                if (square == null) square = MakeShape("Square", 8, (x, y) => 1f);
                return square;
            }
        }

        static float Sq(float v)
        {
            return v * v;
        }

        static Sprite MakeShape(string name, int size, Func<float, float, float> alpha)
        {
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false)
            {
                name = name,
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp
            };
            var px = new Color32[size * size];
            for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                    px[y * size + x] = new Color32(255, 255, 255, (byte)(alpha(x + 0.5f, y + 0.5f) * 255f));
            tex.SetPixels32(px);
            tex.Apply(false, true);
            return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size);
        }

        /// <summary>Anti-aliased silhouette with a dark outline and black eyes, 1 world unit across.</summary>
        static Sprite MakeBody(string name, Func<float, float, bool> inside, params Vector2[] eyes)
        {
            const int N = BodySize;
            const int SS = 4;
            var coverage = new float[N * N];
            for (int y = 0; y < N; y++)
            {
                for (int x = 0; x < N; x++)
                {
                    int hits = 0;
                    for (int sy = 0; sy < SS; sy++)
                        for (int sx = 0; sx < SS; sx++)
                            if (inside(x + (sx + 0.5f) / SS, y + (sy + 0.5f) / SS)) hits++;
                    coverage[y * N + x] = hits / (float)(SS * SS);
                }
            }

            var outline = new Color32(35, 35, 40, 255);
            var fill = new Color32(255, 255, 255, 255);
            var eye = new Color32(10, 10, 12, 255);
            var px = new Color32[N * N];
            for (int y = 0; y < N; y++)
            {
                for (int x = 0; x < N; x++)
                {
                    float cov = coverage[y * N + x];
                    if (cov <= 0f)
                    {
                        px[y * N + x] = new Color32(0, 0, 0, 0);
                        continue;
                    }
                    bool edge = false;
                    for (int dy = -3; dy <= 3 && !edge; dy++)
                    {
                        for (int dx = -3; dx <= 3; dx++)
                        {
                            if (dx * dx + dy * dy > 7) continue;
                            int nx = x + dx, ny = y + dy;
                            if (nx < 0 || ny < 0 || nx >= N || ny >= N || coverage[ny * N + nx] < 0.5f)
                            {
                                edge = true;
                                break;
                            }
                        }
                    }
                    Color32 c = edge ? outline : fill;
                    foreach (Vector2 e in eyes)
                        if (Sq(x + 0.5f - e.x) + Sq(y + 0.5f - e.y) <= Sq(3.8f)) c = eye;
                    c.a = (byte)(cov * 255f);
                    px[y * N + x] = c;
                }
            }

            var tex = new Texture2D(N, N, TextureFormat.RGBA32, true)
            {
                name = name,
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp
            };
            tex.SetPixels32(px);
            tex.Apply(true, true);
            return Sprite.Create(tex, new Rect(0, 0, N, N), new Vector2(0.5f, 0.5f), N);
        }
    }
}
