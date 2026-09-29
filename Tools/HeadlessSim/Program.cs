using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using NeuronWorld;

// Headless runner for the Neuron World simulation core.
//   dotnet run -c Release -- --ticks 30000 --seed 42 --every 1000 [--map]
static class Program
{
    static int Main(string[] args)
    {
        int ticks = 30000, seed = 42, every = 1000;
        bool showMap = false;
        string pngPath = null;
        var settings = new SimSettings();
        for (int i = 0; i < args.Length; i++)
        {
            switch (args[i])
            {
                case "--ticks": ticks = int.Parse(args[++i]); break;
                case "--seed": seed = int.Parse(args[++i]); break;
                case "--every": every = int.Parse(args[++i]); break;
                case "--map": showMap = true; break;
                case "--png": pngPath = args[++i]; break; // write a picture of the world at the end
                case "--set": Set(settings, args[++i]); break; // e.g. --set plantEnergy=30
                default: Console.Error.WriteLine("Unknown argument " + args[i]); return 1;
            }
        }
        settings.seed = seed;
        var sw = Stopwatch.StartNew();
        var sim = new Simulation(settings);
        var dietDeaths = new Dictionary<string, Dictionary<string, int>>();
        var dietLives = new Dictionary<string, (int count, long ages, long kills, long kids, double eaten)>();
        sim.Died += c =>
        {
            string diet = c.genome.DietLabel;
            if (!dietDeaths.TryGetValue(diet, out var causes)) dietDeaths[diet] = causes = new Dictionary<string, int>();
            string cause = c.causeOfDeath.StartsWith("Killed by") ? "Predation" : c.causeOfDeath;
            causes[cause] = causes.GetValueOrDefault(cause) + 1;
            var l = dietLives.GetValueOrDefault(diet);
            dietLives[diet] = (l.count + 1, l.ages + c.age, l.kills + c.kills, l.kids + c.children, l.eaten + c.energyEaten);
        };
        Console.WriteLine($"World {sim.map.Width}x{sim.map.Height}, seed {sim.seed}, generated in {sw.ElapsedMilliseconds} ms");
        PrintTerrainSummary(sim.map);
        if (showMap) PrintMap(sim);

        Console.WriteLine();
        Console.WriteLine("   tick  year season |  pop  herb omni carn  aqua | species maxGen | size speed hidden syn | ms/tick");
        sw.Restart();
        long lastMs = 0;
        int lastTick = 0;
        for (int t = 1; t <= ticks; t++)
        {
            sim.Step();
            if (t % every == 0 || t == ticks)
            {
                long ms = sw.ElapsedMilliseconds;
                double per = (ms - lastMs) / (double)Math.Max(1, t - lastTick);
                lastMs = ms;
                lastTick = t;
                var s = sim.ComputeStats();
                Console.WriteLine(
                    $"{t,7} {sim.Year,5} {sim.SeasonName,-6} | {s.population,4} {s.herbivores,5} {s.omnivores,4} {s.carnivores,4} {s.aquatic,5} | {s.species,7} {s.maxGeneration,6} | {s.avgSize,4:0.00} {s.avgSpeed,5:0.00} {s.avgHidden,6:0.0} {s.avgSynapses,4:0} | {per,6:0.00}");
            }
        }
        Console.WriteLine($"Ran {ticks} ticks in {sw.Elapsed.TotalSeconds:0.0}s ({ticks / sw.Elapsed.TotalSeconds:0} ticks/s)");
        Console.WriteLine($"Births {sim.TotalBirths}, deaths {sim.TotalDeaths}, species ever {sim.species.all.Count}, alive {sim.species.AliveCount}");

        Console.WriteLine();
        Console.WriteLine("Deaths by cause:");
        foreach (var kv in sim.deathCauses.OrderByDescending(k => k.Value))
            Console.WriteLine($"  {kv.Key,-22} {kv.Value,7} ({100.0 * kv.Value / Math.Max(1, sim.TotalDeaths):0.0}%)");

        foreach (var kv in dietLives)
        {
            var l = kv.Value;
            Console.WriteLine($"  {kv.Key,-10} lives {l.count,6}: avg age {l.ages / (double)l.count,6:0}, kills {l.kills / (double)l.count:0.00}, children {l.kids / (double)l.count:0.00}, eaten {l.eaten / l.count:0}  | " +
                string.Join(", ", dietDeaths[kv.Key].OrderByDescending(c => c.Value).Select(c => $"{c.Key} {100.0 * c.Value / l.count:0}%")));
        }

        Console.WriteLine();
        Console.WriteLine("Largest living species:");
        foreach (var sp in sim.species.all.Where(s => !s.Extinct).OrderByDescending(s => s.population).Take(10))
        {
            var members = sim.creatures.Where(c => c.alive && c.species == sp).ToList();
            float diet = members.Count > 0 ? members.Average(c => c.genome.diet) : 0f;
            float aqua = members.Count > 0 ? members.Average(c => c.genome.aquatic) : 0f;
            float size = members.Count > 0 ? members.Average(c => c.genome.size) : 0f;
            float gen = members.Count > 0 ? (float)members.Average(c => c.generation) : 0f;
            Console.WriteLine($"  {sp.name,-16} pop {sp.population,4} peak {sp.peakPopulation,4} diet {diet:0.00} aquatic {aqua:0.00} size {size:0.00} avgGen {gen,6:0.0} from {sp.parentName ?? "-"}");
        }

        Console.WriteLine();
        Console.WriteLine("Last events:");
        foreach (var e in sim.events.Skip(Math.Max(0, sim.events.Count - 12)))
            Console.WriteLine($"  [{e.tick,6}] {e.text}");

        if (showMap) PrintMap(sim);
        if (pngPath != null)
        {
            WritePng(sim, pngPath, 4);
            Console.WriteLine("Wrote " + pngPath);
        }
        return 0;
    }

    /// <summary>Renders the map with MapPalette (same colours as in Unity) plus every creature as a dot.</summary>
    static void WritePng(Simulation sim, string path, int scale)
    {
        var map = sim.map;
        var shade = new float[map.Size];
        var rgba = new byte[map.Size * 4];
        MapPalette.ComputeShade(map, shade);
        MapPalette.Fill(map, shade, rgba, 0);

        int w = map.Width * scale, h = map.Height * scale;
        var img = new byte[w * h * 3];
        for (int y = 0; y < h; y++)
        {
            for (int x = 0; x < w; x++)
            {
                int src = ((map.Height - 1 - y / scale) * map.Width + x / scale) * 4; // PNG rows go top-down
                int dst = (y * w + x) * 3;
                img[dst] = rgba[src];
                img[dst + 1] = rgba[src + 1];
                img[dst + 2] = rgba[src + 2];
            }
        }

        foreach (var c in sim.creatures)
        {
            if (!c.alive) continue;
            HsvToRgb(c.genome.hue, c.genome.saturation, c.genome.value, out byte r, out byte g, out byte b);
            float cx = c.x * scale, cy = (map.Height - c.y) * scale, rad = c.radius * scale;
            for (int y = (int)(cy - rad - 1); y <= (int)(cy + rad + 1); y++)
            {
                for (int x = (int)(cx - rad - 1); x <= (int)(cx + rad + 1); x++)
                {
                    if (x < 0 || y < 0 || x >= w || y >= h) continue;
                    float d = MathF.Sqrt((x + 0.5f - cx) * (x + 0.5f - cx) + (y + 0.5f - cy) * (y + 0.5f - cy));
                    if (d > rad + 0.5f) continue;
                    int dst = (y * w + x) * 3;
                    bool edge = d > rad - 1f;
                    img[dst] = edge ? (byte)30 : r;
                    img[dst + 1] = edge ? (byte)30 : g;
                    img[dst + 2] = edge ? (byte)35 : b;
                }
            }
        }

        using var file = System.IO.File.Create(path);
        EncodePng(file, img, w, h);
    }

    static void HsvToRgb(float h, float s, float v, out byte r, out byte g, out byte b)
    {
        float hh = (h % 1f) * 6f;
        int sector = (int)hh;
        float f = hh - sector, p = v * (1 - s), q = v * (1 - s * f), t = v * (1 - s * (1 - f));
        float rr, gg, bb;
        switch (sector)
        {
            case 0: rr = v; gg = t; bb = p; break;
            case 1: rr = q; gg = v; bb = p; break;
            case 2: rr = p; gg = v; bb = t; break;
            case 3: rr = p; gg = q; bb = v; break;
            case 4: rr = t; gg = p; bb = v; break;
            default: rr = v; gg = p; bb = q; break;
        }
        r = (byte)(rr * 255); g = (byte)(gg * 255); b = (byte)(bb * 255);
    }

    static void EncodePng(System.IO.Stream output, byte[] rgb, int w, int h)
    {
        var raw = new System.IO.MemoryStream();
        using (var z = new System.IO.Compression.ZLibStream(raw, System.IO.Compression.CompressionLevel.Optimal, true))
        {
            for (int y = 0; y < h; y++)
            {
                z.WriteByte(0); // no filter
                z.Write(rgb, y * w * 3, w * 3);
            }
        }
        output.Write(new byte[] { 137, 80, 78, 71, 13, 10, 26, 10 });
        var ihdr = new byte[13];
        WriteBE(ihdr, 0, w);
        WriteBE(ihdr, 4, h);
        ihdr[8] = 8;  // bit depth
        ihdr[9] = 2;  // colour type RGB
        WriteChunk(output, "IHDR", ihdr);
        WriteChunk(output, "IDAT", raw.ToArray());
        WriteChunk(output, "IEND", new byte[0]);
    }

    static void WriteChunk(System.IO.Stream s, string type, byte[] data)
    {
        var len = new byte[4];
        WriteBE(len, 0, data.Length);
        s.Write(len);
        var typeBytes = System.Text.Encoding.ASCII.GetBytes(type);
        s.Write(typeBytes);
        s.Write(data);
        uint crc = Crc32(typeBytes, 0xFFFFFFFFu);
        crc = Crc32(data, crc) ^ 0xFFFFFFFFu;
        var crcBytes = new byte[4];
        WriteBE(crcBytes, 0, (int)crc);
        s.Write(crcBytes);
    }

    static uint Crc32(byte[] data, uint crc)
    {
        foreach (byte b in data)
        {
            crc ^= b;
            for (int k = 0; k < 8; k++) crc = (crc & 1) != 0 ? (crc >> 1) ^ 0xEDB88320u : crc >> 1;
        }
        return crc;
    }

    static void WriteBE(byte[] buffer, int offset, int value)
    {
        buffer[offset] = (byte)(value >> 24);
        buffer[offset + 1] = (byte)(value >> 16);
        buffer[offset + 2] = (byte)(value >> 8);
        buffer[offset + 3] = (byte)value;
    }

    static void Set(SimSettings settings, string assignment)
    {
        string[] parts = assignment.Split('=');
        var field = typeof(SimSettings).GetField(parts[0]);
        if (field == null) throw new ArgumentException("No setting named " + parts[0]);
        field.SetValue(settings, Convert.ChangeType(parts[1], field.FieldType, System.Globalization.CultureInfo.InvariantCulture));
    }

    static void PrintTerrainSummary(WorldMap map)
    {
        var counts = new int[TileInfo.Count];
        foreach (var t in map.tiles) counts[(int)t]++;
        Console.WriteLine(string.Join(", ", Enumerable.Range(0, TileInfo.Count)
            .Select(i => $"{TileInfo.Names[i]} {100.0 * counts[i] / map.Size:0}%")));
    }

    static void PrintMap(Simulation sim)
    {
        var map = sim.map;
        const string glyphs = "~-.,\"_^A*";
        var occupied = new HashSet<int>();
        foreach (var c in sim.creatures)
            if (c.alive) occupied.Add(((int)(c.y / 4)) * 1000 + (int)(c.x / 2));
        for (int y = map.Height - 1; y >= 0; y -= 4)
        {
            var line = new char[map.Width / 2];
            for (int x = 0; x < map.Width; x += 2)
            {
                bool hasCreature = occupied.Contains((y / 4) * 1000 + x / 2);
                line[x / 2] = hasCreature ? '@' : glyphs[(int)map.tiles[y * map.Width + x]];
            }
            Console.WriteLine(new string(line));
        }
    }
}
