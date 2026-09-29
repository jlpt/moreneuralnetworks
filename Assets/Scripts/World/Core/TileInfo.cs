namespace NeuronWorld
{
    public enum TileType : byte
    {
        DeepWater,
        ShallowWater,
        Beach,
        Grassland,
        Forest,
        Desert,
        Hills,
        Mountain,
        Snow
    }

    /// <summary>Per-terrain constants: how much plant food a tile holds, how fast it regrows, how easy it is to cross.</summary>
    public static class TileInfo
    {
        public const int Count = 9;

        public static readonly string[] Names =
        {
            "Deep water", "Shallow water", "Beach", "Grassland", "Forest", "Desert", "Hills", "Mountain", "Snow"
        };

        // Maximum plant food a tile of this type can hold.
        public static readonly float[] PlantCapacity = { 0.35f, 0.6f, 0.15f, 1.0f, 1.6f, 0.1f, 0.5f, 0.05f, 0.02f };

        // Fraction of the missing plant food that regrows each tick. Plant regrowth is what limits
        // the population, so these are small: a grazed-bare grassland tile takes ~1700 ticks to half recover.
        public static readonly float[] PlantGrowth = { 0.00015f, 0.00025f, 0.0001f, 0.0004f, 0.0005f, 0.00006f, 0.0002f, 0.00004f, 0.00002f };

        // Movement speed multiplier (water tiles are handled by the aquatic trait instead).
        public static readonly float[] SpeedFactor = { 1f, 1f, 0.95f, 1f, 0.8f, 0.9f, 0.7f, 0.45f, 0.55f };

        public static readonly bool[] Flammable = { false, false, false, true, true, false, true, false, false };

        // Height thresholds (height is 0..1).
        public const float DeepWaterLevel = 0.36f;
        public const float WaterLevel = 0.44f;
        public const float BeachLevel = 0.465f;
        public const float HillsLevel = 0.70f;
        public const float MountainLevel = 0.80f;
        public const float SnowLevel = 0.90f;

        public static bool IsWater(TileType t)
        {
            return t <= TileType.ShallowWater;
        }

        public static TileType Classify(float height, float moisture)
        {
            if (height < DeepWaterLevel) return TileType.DeepWater;
            if (height < WaterLevel) return TileType.ShallowWater;
            if (height < BeachLevel) return moisture < 0.3f ? TileType.Desert : TileType.Beach;
            if (height < HillsLevel)
            {
                if (moisture < 0.33f) return TileType.Desert;
                if (moisture > 0.58f) return TileType.Forest;
                return TileType.Grassland;
            }
            if (height < MountainLevel) return TileType.Hills;
            if (height < SnowLevel) return TileType.Mountain;
            return TileType.Snow;
        }
    }
}
