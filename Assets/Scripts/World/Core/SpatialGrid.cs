using System;
using System.Collections.Generic;

namespace NeuronWorld
{
    /// <summary>Buckets creatures by position so neighbour lookups don't scan the whole population.</summary>
    public sealed class SpatialGrid
    {
        readonly float cellSize;
        readonly int cols, rows;
        readonly List<Creature>[] cells;

        public SpatialGrid(int width, int height, float cellSize)
        {
            this.cellSize = cellSize;
            cols = Math.Max(1, (int)Math.Ceiling(width / cellSize));
            rows = Math.Max(1, (int)Math.Ceiling(height / cellSize));
            cells = new List<Creature>[cols * rows];
            for (int i = 0; i < cells.Length; i++) cells[i] = new List<Creature>();
        }

        public void Clear()
        {
            for (int i = 0; i < cells.Length; i++) cells[i].Clear();
        }

        public void Insert(Creature c)
        {
            int cx = Math.Min(cols - 1, Math.Max(0, (int)(c.x / cellSize)));
            int cy = Math.Min(rows - 1, Math.Max(0, (int)(c.y / cellSize)));
            cells[cy * cols + cx].Add(c);
        }

        /// <summary>Fills results with creatures in cells overlapping the circle (callers still check exact distance).</summary>
        public void Query(float x, float y, float radius, List<Creature> results)
        {
            results.Clear();
            int x0 = Math.Max(0, (int)((x - radius) / cellSize));
            int x1 = Math.Min(cols - 1, (int)((x + radius) / cellSize));
            int y0 = Math.Max(0, (int)((y - radius) / cellSize));
            int y1 = Math.Min(rows - 1, (int)((y + radius) / cellSize));
            for (int cy = y0; cy <= y1; cy++)
                for (int cx = x0; cx <= x1; cx++)
                    results.AddRange(cells[cy * cols + cx]);
        }
    }
}
