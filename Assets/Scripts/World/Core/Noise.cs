using System;

namespace NeuronWorld
{
    /// <summary>Seeded 2D Perlin noise used to generate terrain.</summary>
    public sealed class Noise
    {
        readonly int[] perm = new int[512];

        public Noise(int seed)
        {
            var rng = new Rng(seed);
            var p = new int[256];
            for (int i = 0; i < 256; i++) p[i] = i;
            for (int i = 255; i > 0; i--)
            {
                int j = rng.Range(0, i + 1);
                int t = p[i];
                p[i] = p[j];
                p[j] = t;
            }
            for (int i = 0; i < 512; i++) perm[i] = p[i & 255];
        }

        static float Fade(float t)
        {
            return t * t * t * (t * (t * 6f - 15f) + 10f);
        }

        static float Lerp(float a, float b, float t)
        {
            return a + (b - a) * t;
        }

        static float Grad(int hash, float x, float y)
        {
            switch (hash & 7)
            {
                case 0: return x + y;
                case 1: return -x + y;
                case 2: return x - y;
                case 3: return -x - y;
                case 4: return x;
                case 5: return -x;
                case 6: return y;
                default: return -y;
            }
        }

        /// <summary>Perlin noise, roughly in [-1, 1].</summary>
        public float Perlin(float x, float y)
        {
            int xi = (int)MathF.Floor(x);
            int yi = (int)MathF.Floor(y);
            float xf = x - xi;
            float yf = y - yi;
            int X = xi & 255;
            int Y = yi & 255;
            float u = Fade(xf);
            float v = Fade(yf);

            int aa = perm[perm[X] + Y];
            int ab = perm[perm[X] + Y + 1];
            int ba = perm[perm[X + 1] + Y];
            int bb = perm[perm[X + 1] + Y + 1];

            float x1 = Lerp(Grad(aa, xf, yf), Grad(ba, xf - 1f, yf), u);
            float x2 = Lerp(Grad(ab, xf, yf - 1f), Grad(bb, xf - 1f, yf - 1f), u);
            return Lerp(x1, x2, v);
        }

        /// <summary>Fractal (multi-octave) noise, roughly in [-1, 1].</summary>
        public float Fbm(float x, float y, int octaves, float lacunarity = 2f, float gain = 0.5f)
        {
            float sum = 0f, amp = 1f, freq = 1f, norm = 0f;
            for (int i = 0; i < octaves; i++)
            {
                sum += Perlin(x * freq, y * freq) * amp;
                norm += amp;
                amp *= gain;
                freq *= lacunarity;
            }
            return sum / norm;
        }
    }
}
