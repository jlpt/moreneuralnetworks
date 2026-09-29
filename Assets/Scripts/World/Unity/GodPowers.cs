using UnityEngine;

namespace NeuronWorld
{
    public enum GodTool
    {
        Inspect,
        Herbivores,
        Omnivores,
        Carnivores,
        CloneSelected,
        Rain,
        RaiseLand,
        LowerLand,
        Fire,
        Lightning,
        Meteor,
        Bless
    }

    /// <summary>Turns mouse input into god powers: spawning life, shaping land, disasters and blessings.</summary>
    public sealed class GodPowers
    {
        public static readonly string[] Names =
        {
            "Inspect", "Herbivores", "Omnivores", "Carnivores", "Clone selected", "Rain",
            "Raise land", "Lower land", "Fire", "Lightning", "Meteor", "Bless"
        };

        public static readonly string[] Descriptions =
        {
            "Click a creature to see its body, brain and family. Right-drag or WASD to move, scroll to zoom.",
            "Click to create a new plant-eating species with its own random brain. Click in the sea for sea creatures.",
            "Click to create a new species that eats both plants and meat.",
            "Click to create a new hunting species. They attack other species and eat the meat.",
            "Click to spawn exact copies of the selected creature (select one first with Inspect).",
            "Hold to water the land: plants regrow fast and fires go out.",
            "Hold to raise the ground. Seas become shores, hills and mountains.",
            "Hold to lower the ground. Dig seas, lakes and rivers.",
            "Hold to start fires. They spread through grass and forest.",
            "Click to strike with lightning, killing creatures and setting plants alight.",
            "Click to drop a meteor: a crater, a firestorm and a mass extinction.",
            "Hold to fully feed and heal creatures under the brush."
        };

        static readonly Color[] BrushColors =
        {
            new Color(1f, 1f, 1f, 0.5f),
            new Color(0.4f, 1f, 0.4f, 0.8f),
            new Color(1f, 0.85f, 0.2f, 0.8f),
            new Color(1f, 0.3f, 0.25f, 0.8f),
            new Color(0.8f, 0.6f, 1f, 0.8f),
            new Color(0.4f, 0.7f, 1f, 0.8f),
            new Color(0.85f, 0.7f, 0.45f, 0.8f),
            new Color(0.3f, 0.5f, 0.9f, 0.8f),
            new Color(1f, 0.5f, 0.1f, 0.85f),
            new Color(1f, 1f, 0.6f, 0.85f),
            new Color(1f, 0.4f, 0.1f, 0.85f),
            new Color(1f, 0.9f, 0.5f, 0.8f)
        };

        public GodTool tool = GodTool.Inspect;
        public float brushRadius = 6f;
        public int spawnCount = 12;

        readonly WorldManager world;
        readonly EffectsRenderer effects;
        bool pressStartedOverUI;

        public Vector2 MouseWorld { get; private set; }
        public bool MouseInWorld { get; private set; }
        /// <summary>The creature under the mouse this frame (for the hover tooltip).</summary>
        public Creature Hovered { get; private set; }

        public GodPowers(WorldManager world, EffectsRenderer effects)
        {
            this.world = world;
            this.effects = effects;
        }

        public static bool IsHeld(GodTool t)
        {
            return t == GodTool.Rain || t == GodTool.RaiseLand || t == GodTool.LowerLand || t == GodTool.Fire || t == GodTool.Bless;
        }

        public void HandleInput(bool pointerOverUI)
        {
            Simulation sim = world.Sim;
            Camera cam = world.Cam;
            Vector3 mp = Input.mousePosition;
            Vector3 w = cam.ScreenToWorldPoint(new Vector3(mp.x, mp.y, -cam.transform.position.z));
            MouseWorld = new Vector2(w.x, w.y);
            bool onScreen = mp.x >= 0f && mp.y >= 0f && mp.x <= Screen.width && mp.y <= Screen.height;
            MouseInWorld = onScreen && !pointerOverUI && sim.map.InBounds(w.x, w.y);
            Hovered = MouseInWorld ? sim.CreatureAt(w.x, w.y, 0.5f) : null;

            if (Input.GetMouseButtonDown(0)) pressStartedOverUI = !MouseInWorld;

            if (MouseInWorld && tool != GodTool.Inspect)
                effects.ShowBrush(MouseWorld, BrushSize(), BrushColors[(int)tool]);
            else
                effects.HideBrush();

            if (!MouseInWorld || pressStartedOverUI) return;

            float x = w.x, y = w.y, r = brushRadius;
            float dt = Time.unscaledDeltaTime;

            if (Input.GetMouseButtonDown(0))
            {
                switch (tool)
                {
                    case GodTool.Inspect:
                        world.Select(sim.CreatureAt(x, y, 0.8f));
                        break;
                    case GodTool.Herbivores:
                        Spawn(Random.Range(0f, 0.15f));
                        break;
                    case GodTool.Omnivores:
                        Spawn(Random.Range(0.4f, 0.5f));
                        break;
                    case GodTool.Carnivores:
                        Spawn(Random.Range(0.85f, 1f));
                        break;
                    case GodTool.CloneSelected:
                        if (world.Selected != null)
                        {
                            sim.SpawnClones(world.Selected, x, y, spawnCount, Mathf.Max(1f, r * 0.4f));
                            effects.Pulse(MouseWorld, r, BrushColors[(int)tool], 0.6f);
                        }
                        break;
                    case GodTool.Lightning:
                        float boltRadius = Mathf.Max(1.5f, r * 0.4f);
                        sim.Lightning(x, y, boltRadius);
                        effects.Bolt(MouseWorld);
                        break;
                    case GodTool.Meteor:
                        sim.Meteor(x, y, r);
                        effects.Flash(MouseWorld, r * 1.3f, new Color(1f, 0.55f, 0.15f, 0.95f), 0.9f, 2.2f);
                        effects.Flash(MouseWorld, r * 0.6f, new Color(1f, 1f, 0.8f, 1f), 0.4f, 1.5f);
                        world.ShakeCamera(0.6f);
                        break;
                }
            }

            if (Input.GetMouseButton(0))
            {
                switch (tool)
                {
                    case GodTool.Rain:
                        sim.Rain(x, y, r, dt * 12f);
                        break;
                    case GodTool.RaiseLand:
                        sim.ShapeLand(x, y, r, 0.35f * dt);
                        break;
                    case GodTool.LowerLand:
                        sim.ShapeLand(x, y, r, -0.35f * dt);
                        break;
                    case GodTool.Fire:
                        sim.IgniteArea(x, y, r * 0.5f, 6f * dt);
                        break;
                    case GodTool.Bless:
                        sim.Bless(x, y, r);
                        if (Random.value < dt * 6f) effects.Pulse(MouseWorld, r, new Color(1f, 0.9f, 0.5f, 0.7f), 0.5f);
                        break;
                }
            }
        }

        float BrushSize()
        {
            if (tool == GodTool.Lightning) return Mathf.Max(1.5f, brushRadius * 0.4f);
            if (tool == GodTool.Meteor) return brushRadius * 1.3f;
            if (tool == GodTool.Fire) return brushRadius * 0.5f;
            if (tool == GodTool.Herbivores || tool == GodTool.Omnivores || tool == GodTool.Carnivores || tool == GodTool.CloneSelected)
                return Mathf.Max(1f, brushRadius * 0.4f) * 2f;
            return brushRadius;
        }

        void Spawn(float diet)
        {
            Simulation sim = world.Sim;
            Species sp = sim.SpawnNewSpecies(MouseWorld.x, MouseWorld.y, spawnCount, diet, Mathf.Max(1f, brushRadius * 0.4f));
            effects.Pulse(MouseWorld, brushRadius, BrushColors[(int)tool], 0.6f);
            Creature first = sim.FindMember(sp);
            if (first != null) world.Select(first);
        }
    }
}
