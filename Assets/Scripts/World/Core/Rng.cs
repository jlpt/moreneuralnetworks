using System;

namespace NeuronWorld
{
    /// <summary>
    /// Small, fast, seedable random number generator (xorshift64*).
    /// The simulation core uses this instead of UnityEngine.Random so it can
    /// run outside Unity and so every world can be replayed from its seed.
    /// </summary>
    public sealed class Rng
    {
        ulong state;
        bool hasSpareGaussian;
        float spareGaussian;

        public Rng(int seed)
        {
            state = (ulong)(uint)seed * 0x9E3779B97F4A7C15UL + 0x632BE59BD9B4E019UL;
            if (state == 0) state = 1;
            for (int i = 0; i < 4; i++) NextU64();
        }

        public ulong NextU64()
        {
            state ^= state >> 12;
            state ^= state << 25;
            state ^= state >> 27;
            return state * 0x2545F4914F6CDD1DUL;
        }

        /// <summary>Uniform float in [0, 1).</summary>
        public float Value()
        {
            return (NextU64() >> 40) * (1f / 16777216f);
        }

        /// <summary>Uniform float in [min, max).</summary>
        public float Range(float min, float max)
        {
            return min + (max - min) * Value();
        }

        /// <summary>Uniform int in [min, maxExclusive).</summary>
        public int Range(int min, int maxExclusive)
        {
            if (maxExclusive <= min) return min;
            return min + (int)(NextU64() % (ulong)(maxExclusive - min));
        }

        public bool Chance(float probability)
        {
            return Value() < probability;
        }

        /// <summary>Standard normal sample (mean 0, deviation 1).</summary>
        public float Gaussian()
        {
            if (hasSpareGaussian)
            {
                hasSpareGaussian = false;
                return spareGaussian;
            }
            float u, v, s;
            do
            {
                u = Value() * 2f - 1f;
                v = Value() * 2f - 1f;
                s = u * u + v * v;
            } while (s >= 1f || s == 0f);
            float mul = MathF.Sqrt(-2f * MathF.Log(s) / s);
            spareGaussian = v * mul;
            hasSpareGaussian = true;
            return u * mul;
        }
    }
}
