using System.Collections.Generic;
using UnityEngine;

namespace NeuronWorld
{
    public enum ViewMode
    {
        Genes,
        Species,
        Diet,
        Energy
    }

    /// <summary>
    /// Shows every living creature with a pooled SpriteRenderer. Body shape reflects the genome
    /// (round grazer, pointed hunter, fish), colour depends on the view mode.
    /// </summary>
    public sealed class CreatureRenderer
    {
        const int SortBites = 5;
        const int SortBodies = 10;
        const int SortSelection = 20;

        readonly Transform root;
        readonly Material material;
        readonly List<SpriteRenderer> bodies = new List<SpriteRenderer>();
        readonly List<SpriteRenderer> bites = new List<SpriteRenderer>();
        readonly SpriteRenderer selectionRing;
        int activeBodies;
        int activeBites;

        public CreatureRenderer(Transform parent, Material material)
        {
            this.material = material;
            root = new GameObject("Creatures").transform;
            root.SetParent(parent, false);
            selectionRing = CreateRenderer("Selection", SpriteFactory.Ring, SortSelection);
            selectionRing.gameObject.SetActive(false);
        }

        SpriteRenderer CreateRenderer(string name, Sprite sprite, int order)
        {
            var go = new GameObject(name);
            go.transform.SetParent(root, false);
            var sr = go.AddComponent<SpriteRenderer>();
            if (material != null) sr.sharedMaterial = material;
            sr.sprite = sprite;
            sr.sortingOrder = order;
            return sr;
        }

        public void Sync(List<Creature> creatures, ViewMode mode, Creature selected, int tick)
        {
            int n = 0, b = 0;
            for (int i = 0; i < creatures.Count; i++)
            {
                Creature c = creatures[i];
                if (!c.alive) continue;

                SpriteRenderer sr = Body(n++);
                Transform t = sr.transform;
                t.localPosition = new Vector3(c.x, c.y, 0f);
                t.localRotation = Quaternion.Euler(0f, 0f, c.heading * Mathf.Rad2Deg);
                float d = c.radius * 2f;
                t.localScale = new Vector3(d, d, 1f);
                Sprite shape = SpriteFactory.BodyFor(c.genome);
                if (sr.sprite != shape) sr.sprite = shape;
                sr.color = ColorFor(c, mode);

                if (tick - c.lastAttackTick <= 3)
                {
                    SpriteRenderer bite = Bite(b++);
                    bite.transform.localPosition = new Vector3(c.lastTargetX, c.lastTargetY, 0f);
                    float s = 0.5f + 0.3f * c.genome.size;
                    bite.transform.localScale = new Vector3(s, s, 1f);
                }
            }

            for (int i = n; i < activeBodies; i++) bodies[i].gameObject.SetActive(false);
            activeBodies = n;
            for (int i = b; i < activeBites; i++) bites[i].gameObject.SetActive(false);
            activeBites = b;

            if (selected != null && selected.alive)
            {
                if (!selectionRing.gameObject.activeSelf) selectionRing.gameObject.SetActive(true);
                selectionRing.transform.localPosition = new Vector3(selected.x, selected.y, 0f);
                float pulse = 1f + 0.12f * Mathf.Sin(Time.unscaledTime * 6f);
                float r = (selected.radius * 2f + 0.8f) * pulse;
                selectionRing.transform.localScale = new Vector3(r, r, 1f);
                selectionRing.color = new Color(1f, 0.95f, 0.4f, 0.95f);
            }
            else if (selectionRing.gameObject.activeSelf)
            {
                selectionRing.gameObject.SetActive(false);
            }
        }

        SpriteRenderer Body(int index)
        {
            if (index == bodies.Count) bodies.Add(CreateRenderer("Creature", SpriteFactory.RoundBody, SortBodies));
            SpriteRenderer sr = bodies[index];
            if (index >= activeBodies) sr.gameObject.SetActive(true);
            return sr;
        }

        SpriteRenderer Bite(int index)
        {
            if (index == bites.Count)
            {
                SpriteRenderer sr = CreateRenderer("Bite", SpriteFactory.Circle, SortBites);
                sr.color = new Color(1f, 0.15f, 0.1f, 0.65f);
                bites.Add(sr);
            }
            SpriteRenderer bite = bites[index];
            if (index >= activeBites) bite.gameObject.SetActive(true);
            return bite;
        }

        public static Color ColorFor(Creature c, ViewMode mode)
        {
            Genome g = c.genome;
            switch (mode)
            {
                case ViewMode.Species:
                    Genome f = c.species.founder;
                    return Color.HSVToRGB(f.hue, Mathf.Max(0.6f, f.saturation), 1f);
                case ViewMode.Diet:
                    return DietColor(g.diet);
                case ViewMode.Energy:
                    return Color.Lerp(new Color(0.3f, 0.1f, 0.12f), new Color(1f, 0.95f, 0.35f), c.EnergyFraction);
                default:
                    return Color.HSVToRGB(g.hue, g.saturation, g.value);
            }
        }

        public static Color DietColor(float diet)
        {
            var green = new Color(0.35f, 0.9f, 0.3f);
            var yellow = new Color(1f, 0.85f, 0.2f);
            var red = new Color(0.95f, 0.2f, 0.15f);
            return diet < 0.5f ? Color.Lerp(green, yellow, diet * 2f) : Color.Lerp(yellow, red, diet * 2f - 1f);
        }
    }
}
