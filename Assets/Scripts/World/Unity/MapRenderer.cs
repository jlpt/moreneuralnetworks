using UnityEngine;

namespace NeuronWorld
{
    /// <summary>
    /// Draws the tile map as a single point-filtered texture (one pixel per tile), WorldBox style.
    /// Colours come from MapPalette (terrain, plant cover, meat, fire and relief shading).
    /// </summary>
    public sealed class MapRenderer
    {
        readonly GameObject gameObject;
        readonly SpriteRenderer spriteRenderer;
        Texture2D texture;
        Sprite sprite;
        byte[] pixels;
        float[] shade;
        int shadedVersion = -1;
        int frame;

        public MapRenderer(Transform parent, Material material)
        {
            gameObject = new GameObject("Map");
            gameObject.transform.SetParent(parent, false);
            spriteRenderer = gameObject.AddComponent<SpriteRenderer>();
            if (material != null) spriteRenderer.sharedMaterial = material;
            spriteRenderer.sortingOrder = -100;
        }

        public void Build(WorldMap map)
        {
            if (sprite != null) Object.Destroy(sprite);
            if (texture != null) Object.Destroy(texture);

            texture = new Texture2D(map.Width, map.Height, TextureFormat.RGBA32, false)
            {
                name = "World map",
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp
            };
            pixels = new byte[map.Size * 4];
            shade = new float[map.Size];
            shadedVersion = -1;
            // Pivot at the bottom-left corner and 1 pixel per unit, so tile (x, y) sits at world (x, y).
            sprite = Sprite.Create(texture, new Rect(0, 0, map.Width, map.Height), Vector2.zero, 1f, 0, SpriteMeshType.FullRect);
            spriteRenderer.sprite = sprite;
            Refresh(map);
        }

        public void Refresh(WorldMap map)
        {
            if (texture == null) return;
            if (shadedVersion != map.TerrainVersion)
            {
                MapPalette.ComputeShade(map, shade);
                shadedVersion = map.TerrainVersion;
            }
            MapPalette.Fill(map, shade, pixels, frame++);
            texture.SetPixelData(pixels, 0);
            texture.Apply(false);
        }

        public void Destroy()
        {
            if (sprite != null) Object.Destroy(sprite);
            if (texture != null) Object.Destroy(texture);
            if (gameObject != null) Object.Destroy(gameObject);
        }
    }
}
