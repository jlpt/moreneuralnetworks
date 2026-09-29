using System.Collections.Generic;
using UnityEngine;

namespace NeuronWorld
{
    /// <summary>
    /// The whole interface, drawn with IMGUI so it needs no Canvas, prefabs or fonts:
    /// top bar (time, population, speed), god-power toolbar, species list, event feed,
    /// world statistics with a population graph, and the creature inspector with a live brain view.
    /// </summary>
    public class WorldUI : MonoBehaviour
    {
        public WorldManager world;
        public bool showHelp = true;

        const float TopBarHeight = 32f;
        const float Pad = 8f;
        const float ToolbarWidth = 150f;
        const float SpeciesWidth = 250f;
        const float RightWidth = 332f;
        static readonly string[] ViewNames = { "Genes", "Species", "Diet", "Energy" };

        readonly List<Rect> panels = new List<Rect>();
        readonly List<Species> topSpecies = new List<Species>();
        float scale = 1f;
        float toolbarBottom;

        GUIStyle panelStyle, helpStyle, titleStyle, headerStyle, labelStyle, smallStyle, dimStyle, rightSmall,
            buttonStyle, activeButtonStyle, linkStyle, tooltipStyle, toggleStyle;

        BrainView brainView;
        GraphView graphView;
        float brainTimer;
        float speciesTimer;
        Vector2 inspectorScroll;
        string hoverText;

        void Awake()
        {
            brainView = new BrainView();
            graphView = new GraphView();
        }

        public void OnNewWorld()
        {
            topSpecies.Clear();
            speciesTimer = 0f;
            if (graphView != null && world != null && world.Sim != null) graphView.Render(world.Sim.history.samples, true);
        }

        /// <summary>True when the mouse is over any UI panel (so clicks there don't reach the world).</summary>
        public bool IsPointerOverUI()
        {
            Vector3 m = Input.mousePosition;
            var p = new Vector2(m.x / scale, (Screen.height - m.y) / scale);
            for (int i = 0; i < panels.Count; i++)
                if (panels[i].Contains(p)) return true;
            return false;
        }

        void Update()
        {
            if (world == null || world.Sim == null) return;

            speciesTimer -= Time.unscaledDeltaTime;
            if (speciesTimer <= 0f)
            {
                speciesTimer = 0.5f;
                topSpecies.Clear();
                foreach (Species s in world.Sim.species.all)
                    if (!s.Extinct && s.population > 0) topSpecies.Add(s);
                topSpecies.Sort((a, b) => b.population.CompareTo(a.population));
            }

            graphView.Render(world.Sim.history.samples, false);

            brainTimer -= Time.unscaledDeltaTime;
            if (world.Selected != null && brainTimer <= 0f)
            {
                brainTimer = 0.1f;
                brainView.Render(world.Selected.Brain);
            }
        }

        void OnGUI()
        {
            if (world == null || world.Sim == null) return;
            EnsureStyles();

            scale = Mathf.Clamp(Screen.height / 820f, 0.75f, 2.5f);
            GUI.matrix = Matrix4x4.TRS(Vector3.zero, Quaternion.identity, new Vector3(scale, scale, 1f));
            float W = Screen.width / scale;
            float H = Screen.height / scale;

            panels.Clear();
            hoverText = null;

            DrawTopBar(W);
            if (showHelp)
            {
                // Only the top bar stays interactive while help is open, so clicks can't reach panels underneath.
                DrawHelp(W, H);
            }
            else
            {
                DrawToolbar();
                DrawSpeciesPanel(H);
                if (world.Selected != null) DrawInspector(W, H);
                else DrawWorldPanel(W);
                DrawEventFeed(W, H);
                DrawHoverCreature();
            }

            string tip = !string.IsNullOrEmpty(GUI.tooltip) ? GUI.tooltip : hoverText;
            if (!string.IsNullOrEmpty(tip)) DrawTooltip(tip, W, H);
        }

        // ------------------------------------------------------------------
        // Panels
        // ------------------------------------------------------------------

        void DrawTopBar(float W)
        {
            var bar = new Rect(0f, 0f, W, TopBarHeight);
            GUI.Box(bar, GUIContent.none, panelStyle);
            panels.Add(bar);

            Simulation sim = world.Sim;
            StatsSample st = sim.Latest;
            GUI.Label(new Rect(10f, 7f, 150f, 20f), "NEURON WORLD", titleStyle);

            const float controlsWidth = 470f;
            string info = W > 1250f
                ? string.Format("Year {0} · {1}      Population {2}  (herbivores {3} · omnivores {4} · carnivores {5} · in water {6})      Species {7}      Max generation {8}",
                    sim.Year, sim.SeasonName, st.population, st.herbivores, st.omnivores, st.carnivores, st.aquatic, st.species, sim.MaxGeneration)
                : string.Format("Y{0} {1} · Pop {2} ({3}/{4}/{5}) · Species {6} · Gen {7}",
                    sim.Year, sim.SeasonName, st.population, st.herbivores, st.omnivores, st.carnivores, st.species, sim.MaxGeneration);
            GUI.Label(new Rect(165f, 8f, Mathf.Max(50f, W - 165f - controlsWidth), 20f), info, labelStyle);

            float x = W - controlsWidth + 4f;
            const float y = 5f, h = 22f;
            if (GUI.Button(new Rect(x, y, 56f, h), world.paused ? "Play" : "Pause", world.paused ? activeButtonStyle : buttonStyle))
                world.paused = !world.paused;
            x += 60f;
            foreach (float s in WorldManager.SpeedSteps)
            {
                bool active = !world.paused && !world.maxSpeed && Mathf.Approximately(world.speed, s);
                if (GUI.Button(new Rect(x, y, 36f, h), s + "x", active ? activeButtonStyle : buttonStyle)) world.SetSpeed(s);
                x += 38f;
            }
            if (GUI.Button(new Rect(x, y, 42f, h), new GUIContent("Max", "Run as fast as the computer allows"),
                    !world.paused && world.maxSpeed ? activeButtonStyle : buttonStyle))
                world.SetMaxSpeed();
            x += 46f;
            GUI.Label(new Rect(x, y + 3f, 70f, h), string.Format("{0:0} ticks/s", world.MeasuredTicksPerSecond), dimStyle);
            x += 72f;
            if (GUI.Button(new Rect(x, y, 78f, h), new GUIContent("New world", "Generate a new map and new life"), buttonStyle))
                world.NewWorld();
            x += 82f;
            if (GUI.Button(new Rect(x, y, 24f, h), new GUIContent("?", "Help (H)"), buttonStyle)) showHelp = !showHelp;
        }

        void DrawToolbar()
        {
            GodPowers powers = world.Powers;
            int toolCount = GodPowers.Names.Length;
            float x = Pad, y = TopBarHeight + Pad;
            float h = 30f + toolCount * 25f + 200f;
            var r = new Rect(x, y, ToolbarWidth, h);
            GUI.Box(r, GUIContent.none, panelStyle);
            panels.Add(r);
            toolbarBottom = r.yMax;

            GUILayout.BeginArea(new Rect(x + 7f, y + 6f, ToolbarWidth - 14f, h - 12f));
            GUILayout.Label("God powers", headerStyle);
            for (int i = 0; i < toolCount; i++)
            {
                bool active = powers.tool == (GodTool)i;
                var content = new GUIContent(GodPowers.Names[i], GodPowers.Descriptions[i]);
                if (GUILayout.Button(content, active ? activeButtonStyle : buttonStyle, GUILayout.Height(22f)))
                    powers.tool = (GodTool)i;
            }

            GUILayout.Space(8f);
            GUILayout.Label(string.Format("Brush size  {0:0}", powers.brushRadius), smallStyle);
            powers.brushRadius = Mathf.Round(GUILayout.HorizontalSlider(powers.brushRadius, 1f, 30f));
            GUILayout.Space(2f);
            GUILayout.Label(string.Format("Creatures per spawn  {0}", powers.spawnCount), smallStyle);
            powers.spawnCount = Mathf.RoundToInt(GUILayout.HorizontalSlider(powers.spawnCount, 1f, 40f));

            GUILayout.Space(8f);
            GUILayout.Label(new GUIContent("Colour creatures by", "Tab cycles through these"), smallStyle);
            world.viewMode = (ViewMode)GUILayout.SelectionGrid((int)world.viewMode, ViewNames, 2, buttonStyle);
            GUILayout.Space(6f);
            world.settings.autoSeed = GUILayout.Toggle(world.settings.autoSeed,
                new GUIContent(" Auto-seed life", "Add new species when life is nearly extinct"), toggleStyle);
            GUILayout.EndArea();
        }

        void DrawSpeciesPanel(float H)
        {
            float y = toolbarBottom + Pad;
            float h = H - y - Pad;
            if (h < 90f) return;
            var r = new Rect(Pad, y, SpeciesWidth, h);
            GUI.Box(r, GUIContent.none, panelStyle);
            panels.Add(r);

            Simulation sim = world.Sim;
            GUI.Label(new Rect(r.x + 8f, r.y + 6f, r.width - 16f, 20f),
                string.Format("Species  {0} alive · {1} ever", sim.species.AliveCount, sim.species.all.Count), headerStyle);

            const float rowH = 20f;
            float ry = r.y + 30f;
            int rows = Mathf.FloorToInt((r.yMax - ry - 18f) / rowH);
            for (int i = 0; i < topSpecies.Count && i < rows; i++)
            {
                Species s = topSpecies[i];
                var row = new Rect(r.x + 8f, ry, r.width - 16f, rowH);
                Genome f = s.founder;
                DrawSwatch(new Rect(row.x, row.y + 4f, 12f, 12f), Color.HSVToRGB(f.hue, Mathf.Max(0.6f, f.saturation), 1f));
                string label = s.name + (s.representative != null && s.representative.aquatic > 0.5f ? "  ~" : "");
                if (GUI.Button(new Rect(row.x + 18f, row.y, row.width - 110f, rowH),
                        new GUIContent(label, SpeciesTooltip(s)), linkStyle))
                {
                    Creature member = sim.FindMember(s);
                    if (member != null)
                    {
                        world.Select(member);
                        world.FollowSelected = true;
                    }
                }
                Genome rep = s.representative ?? f;
                GUI.color = CreatureRenderer.DietColor(rep.diet);
                GUI.Label(new Rect(row.xMax - 90f, row.y + 2f, 40f, rowH), Genome.DietName(rep.diet).Substring(0, 4), smallStyle);
                GUI.color = Color.white;
                GUI.Label(new Rect(row.xMax - 46f, row.y + 1f, 46f, rowH), s.population.ToString(), labelStyle);
                ry += rowH;
            }
            if (topSpecies.Count == 0)
                GUI.Label(new Rect(r.x + 8f, ry, r.width - 16f, 40f), "No life. Use the spawn powers.", dimStyle);
            GUI.Label(new Rect(r.x + 8f, r.yMax - 18f, r.width - 16f, 16f), "Click a species to follow one of its members.", dimStyle);
        }

        string SpeciesTooltip(Species s)
        {
            string origin = s.parentName != null ? "Evolved from " + s.parentName : "Created by god";
            return string.Format("{0}\n{1} in year {2}\nPopulation {3} (peak {4}), {5} born in total",
                s.name, origin, s.bornTick / world.settings.yearLength + 1, s.population, s.peakPopulation, s.totalBorn);
        }

        void DrawWorldPanel(float W)
        {
            Simulation sim = world.Sim;
            var r = new Rect(W - RightWidth - Pad, TopBarHeight + Pad, RightWidth, 330f);
            GUI.Box(r, GUIContent.none, panelStyle);
            panels.Add(r);
            float x = r.x + 10f, y = r.y + 8f, w = r.width - 20f;

            GUI.Label(new Rect(x, y, w, 20f), "World", headerStyle);
            y += 24f;
            GUI.DrawTexture(new Rect(x, y, GraphView.Width, GraphView.Height), graphView.texture);
            GUI.Label(new Rect(x + 4f, y + 2f, 80f, 16f), graphView.MaxValue.ToString(), dimStyle);
            y += GraphView.Height + 4f;
            float lx = x;
            lx = Legend(lx, y, "total", GraphView.TotalColor);
            lx = Legend(lx, y, "herb", GraphView.HerbivoreColor);
            lx = Legend(lx, y, "omni", GraphView.OmnivoreColor);
            lx = Legend(lx, y, "carn", GraphView.CarnivoreColor);
            Legend(lx, y, "water", GraphView.AquaticColor);
            y += 22f;

            StatsSample st = sim.Latest;
            GUI.Label(new Rect(x, y, w, 18f), string.Format("Born {0:n0} · died {1:n0} · tick {2:n0}", sim.TotalBirths, sim.TotalDeaths, sim.Tick), labelStyle);
            y += 20f;
            GUI.Label(new Rect(x, y, w, 36f), "Deaths: " + TopDeathCauses(sim), labelStyle);
            y += 38f;
            GUI.Label(new Rect(x, y, w, 36f), string.Format("Average body size {0:0.00}, speed {1:0.00}\nAverage brain {2:0.0} hidden neurons, {3:0} connections",
                st.avgSize, st.avgSpeed, st.avgHidden, st.avgSynapses), labelStyle);
            y += 42f;
            GUI.Label(new Rect(x, y, w, 36f), "Select a creature with Inspect to see its brain at work. Seed " + sim.seed + ".", dimStyle);
        }

        float Legend(float x, float y, string text, Color color)
        {
            DrawSwatch(new Rect(x, y + 4f, 10f, 10f), color);
            GUI.Label(new Rect(x + 13f, y, 50f, 18f), text, smallStyle);
            return x + 17f + smallStyle.CalcSize(new GUIContent(text)).x + 8f;
        }

        static string TopDeathCauses(Simulation sim)
        {
            if (sim.TotalDeaths == 0) return "none yet";
            var list = new List<KeyValuePair<string, int>>(sim.deathCauses);
            list.Sort((a, b) => b.Value.CompareTo(a.Value));
            var parts = new List<string>();
            for (int i = 0; i < list.Count && i < 4; i++)
                parts.Add(string.Format("{0} {1:0}%", list[i].Key.ToLowerInvariant(), 100f * list[i].Value / sim.TotalDeaths));
            return string.Join(", ", parts.ToArray());
        }

        void DrawInspector(float W, float H)
        {
            Creature c = world.Selected;
            Genome g = c.genome;
            var r = new Rect(W - RightWidth - Pad, TopBarHeight + Pad, RightWidth, H - TopBarHeight - 2f * Pad);
            GUI.Box(r, GUIContent.none, panelStyle);
            panels.Add(r);

            const float contentHeight = 800f;
            var view = new Rect(r.x + 4f, r.y + 4f, r.width - 8f, r.height - 8f);
            var content = new Rect(0f, 0f, view.width - (contentHeight > view.height ? 16f : 0f), contentHeight);
            inspectorScroll = GUI.BeginScrollView(view, inspectorScroll, content);
            float x = 6f, y = 4f, w = content.width - 12f;

            // Header
            Genome f = c.species.founder;
            DrawSwatch(new Rect(x, y + 4f, 14f, 14f), Color.HSVToRGB(f.hue, Mathf.Max(0.6f, f.saturation), 1f));
            GUI.Label(new Rect(x + 20f, y, w - 20f, 22f), c.species.name + "   generation " + c.generation, headerStyle);
            y += 22f;
            string status = c.alive
                ? string.Format("{0} · age {1}{2} · #{3}", g.DietLabel, AgeText(c.age), g.aquatic > 0.5f ? " · lives in water" : "", c.id)
                : "Died: " + c.causeOfDeath;
            GUI.Label(new Rect(x, y, w, 18f), status, c.alive ? labelStyle : dimStyle);
            y += 22f;

            float bx = x;
            bool follow = GUI.Toggle(new Rect(bx, y, 70f, 22f), world.FollowSelected, new GUIContent("Follow", "Keep the camera on this creature (F)"), buttonStyle);
            if (follow != world.FollowSelected) world.FollowSelected = follow && c.alive;
            bx += 74f;
            if (GUI.Button(new Rect(bx, y, 70f, 22f), new GUIContent("Clone", "Switch to the Clone tool to copy this creature"), buttonStyle))
                world.Powers.tool = GodTool.CloneSelected;
            bx += 74f;
            if (c.alive && GUI.Button(new Rect(bx, y, 70f, 22f), new GUIContent("Smite", "Kill this creature (Delete)"), buttonStyle))
                world.Sim.Kill(c, "Smitten by god");
            bx += 74f;
            if (GUI.Button(new Rect(bx, y, 60f, 22f), new GUIContent("Close", "Deselect (Esc)"), buttonStyle))
            {
                world.Select(null);
                GUI.EndScrollView();
                return;
            }
            y += 30f;

            // Vitals
            DrawBar(new Rect(x, y, w, 16f), c.EnergyFraction, new Color(1f, 0.8f, 0.2f), string.Format("Energy {0:0} / {1:0}", c.energy, c.maxEnergy));
            y += 19f;
            DrawBar(new Rect(x, y, w, 16f), c.HealthFraction, new Color(0.9f, 0.25f, 0.25f), string.Format("Health {0:0} / {1:0}", Mathf.Max(0f, c.health), c.maxHealth));
            y += 19f;
            DrawBar(new Rect(x, y, w, 16f), Mathf.Clamp01(c.age / (float)c.lifespan), new Color(0.55f, 0.55f, 0.65f), "Life " + AgeText(c.age) + " of " + AgeText(c.lifespan));
            y += 25f;

            // Genes
            GUI.Label(new Rect(x, y, w, 18f), "Genes", headerStyle);
            y += 20f;
            float half = w * 0.5f;
            Trait(x, y, "Diet", string.Format("{0:0.00} {1}", g.diet, g.DietLabel.ToLowerInvariant()), "0 eats only plants, 1 eats only meat");
            Trait(x + half, y, "Size", g.size.ToString("0.00"), "Bigger: more health, energy and bite, but costs more to run");
            y += 18f;
            Trait(x, y, "Speed", g.speed.ToString("0.00"), "Top speed. Moving fast costs energy");
            Trait(x + half, y, "Vision", g.vision.ToString("0.0"), "How far it can see, in tiles");
            y += 18f;
            Trait(x, y, "Aquatic", g.aquatic.ToString("0.00"), "0 land animal, 1 sea animal");
            Trait(x + half, y, "Mutation", g.mutationRate.ToString("0.000"), "How much its children differ from it");
            y += 18f;
            Trait(x, y, "Offspring", g.offspringShare.ToString("0.00"), "Share of its energy given to each child");
            Trait(x + half, y, "Children", c.children.ToString(), null);
            y += 18f;
            Trait(x, y, "Kills", c.kills.ToString(), null);
            Trait(x + half, y, "Food eaten", c.energyEaten.ToString("0"), "Total energy eaten in its life");
            y += 24f;

            // Brain
            Brain brain = c.Brain;
            GUI.Label(new Rect(x, y, w, 18f), string.Format("Brain  {0} hidden neurons · {1} connections", brain.HiddenCount, brain.synapses.Count), headerStyle);
            y += 20f;
            var tex = new Rect(x, y, BrainView.Width, BrainView.Height);
            GUI.DrawTexture(tex, brainView.texture);
            GUI.Label(new Rect(tex.x + 4f, tex.y + 1f, 60f, 16f), "senses", dimStyle);
            for (int o = 0; o < BrainIO.OutputCount; o++)
            {
                Vector2 p = brainView.NodePosition(Brain.FirstOutput + o);
                GUI.Label(new Rect(tex.x + p.x - 78f, tex.y + p.y - 9f, 68f, 18f), BrainIO.OutputNames[o], rightSmall);
            }
            Vector2 mouse = Event.current.mousePosition;
            if (tex.Contains(mouse))
            {
                int neuron = brainView.NeuronAt(mouse - tex.position);
                if (neuron >= 0 && neuron < brain.NeuronCount) hoverText = NeuronLabel(brain, neuron);
            }
            y += BrainView.Height + 6f;

            // Output bars
            for (int o = 0; o < BrainIO.OutputCount; o++)
            {
                float v = brain.Output(o);
                DrawSignedBar(new Rect(x, y, w, 15f), v, BrainIO.OutputNames[o] + string.Format("  {0:+0.00;-0.00}", v));
                y += 17f;
            }
            y += 6f;
            GUI.Label(new Rect(x, y, w, 40f), "Hover a neuron for details. Green links excite, red links inhibit; bright links are active.", dimStyle);

            GUI.EndScrollView();
        }

        static string NeuronLabel(Brain brain, int neuron)
        {
            float v = brain.State[neuron];
            if (neuron < Brain.FirstOutput) return string.Format("Sense: {0}\nvalue {1:0.00}", BrainIO.InputNames[neuron], v);
            if (neuron < Brain.FirstHidden)
                return string.Format("Action: {0}\nactivation {1:0.00}", BrainIO.OutputNames[neuron - Brain.FirstOutput], v);
            return string.Format("Hidden neuron {0}\nactivation {1:0.00}, memory {2:0.00}", neuron - Brain.FirstHidden + 1, v, brain.memory[neuron]);
        }

        void DrawEventFeed(float W, float H)
        {
            Simulation sim = world.Sim;
            float left = Pad + SpeciesWidth + Pad;
            float right = W - RightWidth - Pad - Pad;
            float w = Mathf.Min(560f, right - left);
            if (w < 200f) return;
            float x = left + (right - left - w) * 0.5f;

            const int lines = 5;
            const float lineH = 18f;
            float h = 26f + lines * lineH + 8f;
            var r = new Rect(x, H - h - Pad, w, h);
            GUI.Box(r, GUIContent.none, panelStyle);
            panels.Add(r);

            GUI.Label(new Rect(r.x + 8f, r.y + 5f, w - 16f, 18f),
                GodPowers.Names[(int)world.Powers.tool] + ":  " + GodPowers.Descriptions[(int)world.Powers.tool], smallStyle);

            float y = r.y + 26f;
            int start = Mathf.Max(0, sim.events.Count - lines);
            if (sim.events.Count == 0) GUI.Label(new Rect(r.x + 8f, y, w - 16f, lineH), "History will be written here.", dimStyle);
            for (int i = start; i < sim.events.Count; i++)
            {
                WorldEvent e = sim.events[i];
                int year = e.tick / world.settings.yearLength + 1;
                float age = (sim.Tick - e.tick) / (float)world.settings.yearLength;
                GUI.color = new Color(1f, 1f, 1f, Mathf.Lerp(1f, 0.45f, Mathf.Clamp01(age)));
                string text = "Year " + year + ":  " + e.text;
                Species s = e.speciesId >= 0 && e.speciesId < sim.species.all.Count ? sim.species.all[e.speciesId] : null;
                if (s != null && !s.Extinct)
                {
                    if (GUI.Button(new Rect(r.x + 8f, y, w - 16f, lineH), new GUIContent(text, "Click to follow a " + s.name), linkStyle))
                    {
                        Creature member = sim.FindMember(s);
                        if (member != null)
                        {
                            world.Select(member);
                            world.FollowSelected = true;
                        }
                    }
                }
                else
                {
                    GUI.Label(new Rect(r.x + 8f, y, w - 16f, lineH), text, labelStyle);
                }
                GUI.color = Color.white;
                y += lineH;
            }
        }

        void DrawHoverCreature()
        {
            Creature c = world.Powers.Hovered;
            if (c == null || !string.IsNullOrEmpty(GUI.tooltip)) return;
            hoverText = string.Format("{0} (gen {1})\n{2}{3} · energy {4:0}%",
                c.species.name, c.generation, c.genome.DietLabel, c.genome.aquatic > 0.5f ? ", aquatic" : "", c.EnergyFraction * 100f);
        }

        void DrawHelp(float W, float H)
        {
            float w = Mathf.Min(520f, W - 40f), h = Mathf.Min(430f, H - 80f);
            var r = new Rect((W - w) * 0.5f, (H - h) * 0.5f, w, h);
            GUI.Box(r, GUIContent.none, helpStyle);
            panels.Add(r);
            GUILayout.BeginArea(new Rect(r.x + 16f, r.y + 12f, w - 32f, h - 24f));
            GUILayout.Label("Welcome to Neuron World", titleStyle);
            GUILayout.Space(6f);
            GUILayout.Label(
                "Every creature is an AI agent with its own neural network brain. It sees food, water and other " +
                "creatures, feels hunger and pain, and decides for itself how to move, eat, fight and breed. " +
                "Children inherit their parent's brain and body with small mutations, so over generations the " +
                "creatures evolve. Lineages that drift apart become new species.", labelStyle);
            GUILayout.Space(8f);
            GUILayout.Label("Controls", headerStyle);
            GUILayout.Label(
                "Left click: use the selected god power (Inspect selects creatures)\n" +
                "Right/middle drag or WASD: move camera · Scroll or +/-: zoom\n" +
                "Space: pause · 1-4: speed 1x/2x/5x/10x · 5: max speed\n" +
                "Tab: change colours · F: follow selected · Del: smite selected\n" +
                "[ and ]: brush size · Esc: back to Inspect / deselect · H: this help", labelStyle);
            GUILayout.Space(8f);
            GUILayout.Label("Try: speed up to 5x and watch the species list, drop carnivores next to a herd, " +
                            "or raise a mountain range to split a species in two.", dimStyle);
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("Start", activeButtonStyle, GUILayout.Height(28f))) showHelp = false;
            GUILayout.EndArea();
        }

        // ------------------------------------------------------------------
        // Widgets
        // ------------------------------------------------------------------

        void Trait(float x, float y, string name, string value, string tooltip)
        {
            GUI.Label(new Rect(x, y, 70f, 18f), new GUIContent(name, tooltip), dimStyle);
            GUI.Label(new Rect(x + 70f, y, 90f, 18f), value, labelStyle);
        }

        void DrawBar(Rect r, float fraction, Color color, string text)
        {
            DrawSwatch(r, new Color(0.12f, 0.13f, 0.17f));
            DrawSwatch(new Rect(r.x, r.y, r.width * Mathf.Clamp01(fraction), r.height), color * new Color(1f, 1f, 1f, 0.85f));
            GUI.Label(new Rect(r.x + 5f, r.y - 1f, r.width - 10f, r.height + 2f), text, smallStyle);
        }

        void DrawSignedBar(Rect r, float value, string text)
        {
            DrawSwatch(r, new Color(0.12f, 0.13f, 0.17f));
            float mid = r.x + r.width * 0.5f;
            float len = r.width * 0.5f * Mathf.Clamp(value, -1f, 1f);
            Color c = value >= 0f ? new Color(0.3f, 0.75f, 0.4f) : new Color(0.8f, 0.3f, 0.3f);
            DrawSwatch(value >= 0f ? new Rect(mid, r.y, len, r.height) : new Rect(mid + len, r.y, -len, r.height), c);
            DrawSwatch(new Rect(mid, r.y, 1f, r.height), new Color(1f, 1f, 1f, 0.4f));
            GUI.Label(new Rect(r.x + 5f, r.y - 1f, r.width - 10f, r.height + 2f), text, smallStyle);
        }

        static void DrawSwatch(Rect r, Color c)
        {
            Color old = GUI.color;
            GUI.color = c;
            GUI.DrawTexture(r, Texture2D.whiteTexture);
            GUI.color = old;
        }

        void DrawTooltip(string text, float W, float H)
        {
            // Event.current.mousePosition is relative to scroll views, so use the raw mouse position.
            Vector3 raw = Input.mousePosition;
            var mouse = new Vector2(raw.x / scale, (Screen.height - raw.y) / scale);
            Vector2 size = tooltipStyle.CalcSize(new GUIContent(text));
            size.x = Mathf.Min(size.x, 320f);
            size.y = tooltipStyle.CalcHeight(new GUIContent(text), size.x);
            float x = Mathf.Min(mouse.x + 16f, W - size.x - 4f);
            float y = Mathf.Min(mouse.y + 18f, H - size.y - 4f);
            GUI.Label(new Rect(x, y, size.x, size.y), text, tooltipStyle);
        }

        static string AgeText(int ticks)
        {
            return ticks + " ticks";
        }

        void EnsureStyles()
        {
            if (panelStyle != null) return;

            panelStyle = new GUIStyle(GUI.skin.box) { border = new RectOffset(0, 0, 0, 0) };
            panelStyle.normal.background = MakeTexture(new Color(0.07f, 0.09f, 0.13f, 0.9f));
            helpStyle = new GUIStyle(panelStyle);
            helpStyle.normal.background = MakeTexture(new Color(0.05f, 0.07f, 0.1f, 0.97f));

            labelStyle = new GUIStyle(GUI.skin.label) { fontSize = 12, wordWrap = true, richText = false };
            labelStyle.normal.textColor = new Color(0.9f, 0.92f, 0.95f);
            labelStyle.padding = new RectOffset(0, 0, 1, 1);

            smallStyle = new GUIStyle(labelStyle) { fontSize = 11, wordWrap = false };
            dimStyle = new GUIStyle(labelStyle) { fontSize = 11 };
            dimStyle.normal.textColor = new Color(0.6f, 0.65f, 0.72f);
            rightSmall = new GUIStyle(smallStyle) { alignment = TextAnchor.MiddleRight };
            rightSmall.normal.textColor = new Color(0.85f, 0.9f, 1f);

            headerStyle = new GUIStyle(labelStyle) { fontSize = 13, fontStyle = FontStyle.Bold, wordWrap = false };
            titleStyle = new GUIStyle(labelStyle) { fontSize = 15, fontStyle = FontStyle.Bold, wordWrap = false };
            titleStyle.normal.textColor = new Color(0.55f, 0.95f, 0.75f);

            buttonStyle = new GUIStyle(GUI.skin.button) { fontSize = 12 };
            activeButtonStyle = new GUIStyle(buttonStyle) { fontStyle = FontStyle.Bold };
            activeButtonStyle.normal.background = MakeTexture(new Color(0.2f, 0.55f, 0.4f, 1f));
            activeButtonStyle.hover.background = MakeTexture(new Color(0.25f, 0.62f, 0.46f, 1f));
            activeButtonStyle.active.background = activeButtonStyle.normal.background;
            activeButtonStyle.onNormal.background = activeButtonStyle.normal.background;
            activeButtonStyle.onHover.background = activeButtonStyle.hover.background;
            activeButtonStyle.normal.textColor = Color.white;
            activeButtonStyle.hover.textColor = Color.white;
            // Toggle-buttons (e.g. Follow) show the green style while on.
            buttonStyle.onNormal.background = activeButtonStyle.normal.background;
            buttonStyle.onHover.background = activeButtonStyle.hover.background;
            buttonStyle.onNormal.textColor = Color.white;

            linkStyle = new GUIStyle(labelStyle) { wordWrap = false, clipping = TextClipping.Clip };
            linkStyle.hover.textColor = new Color(0.55f, 0.95f, 0.75f);

            toggleStyle = new GUIStyle(GUI.skin.toggle) { fontSize = 12 };
            toggleStyle.normal.textColor = labelStyle.normal.textColor;
            toggleStyle.onNormal.textColor = labelStyle.normal.textColor;

            tooltipStyle = new GUIStyle(labelStyle) { fontSize = 11, wordWrap = true };
            tooltipStyle.normal.background = MakeTexture(new Color(0.02f, 0.03f, 0.05f, 0.95f));
            tooltipStyle.padding = new RectOffset(6, 6, 4, 4);
        }

        static Texture2D MakeTexture(Color c)
        {
            var t = new Texture2D(1, 1, TextureFormat.RGBA32, false) { hideFlags = HideFlags.HideAndDontSave };
            t.SetPixel(0, 0, c);
            t.Apply();
            return t;
        }
    }
}
