using UnityEngine;
using UnityEngine.SceneManagement;

namespace NeuronWorld
{
    /// <summary>
    /// Entry point of the World scene: owns the simulation, runs it at the chosen speed and wires up
    /// rendering, camera, god powers and the on-screen UI. Everything is created from code, so the
    /// scene only needs a camera and this component.
    /// </summary>
    public class WorldManager : MonoBehaviour
    {
        [Tooltip("Rules of the world. Changes apply when you press New World.")]
        public SimSettings settings = new SimSettings();

        [Tooltip("Simulation ticks per second at 1x speed.")]
        public float ticksPerSecond = 30f;

        [Tooltip("Most milliseconds per frame spent simulating, so the game stays responsive at high speed.")]
        public float frameBudgetMs = 22f;

        [Tooltip("How often the map texture is redrawn, in seconds.")]
        public float mapRefreshInterval = 0.1f;

        public static readonly float[] SpeedSteps = { 1f, 2f, 5f, 10f };

        public Simulation Sim { get; private set; }
        public Camera Cam { get; private set; }
        public WorldUI UI { get; private set; }
        public GodPowers Powers { get; private set; }

        [HideInInspector] public float speed = 1f;
        [HideInInspector] public bool maxSpeed;
        [HideInInspector] public bool paused;
        [HideInInspector] public ViewMode viewMode = ViewMode.Genes;

        public Creature Selected { get; private set; }
        public bool FollowSelected { get; set; }
        public float MeasuredTicksPerSecond { get; private set; }

        MapRenderer mapRenderer;
        CreatureRenderer creatureRenderer;
        EffectsRenderer effects;
        WorldCameraController cameraController;
        float accumulator;
        int ticksCounted;
        float measureTimer;
        float mapTimer;
        int lastTerrainVersion = -1;

        const string SceneName = "World";

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void RegisterSceneHook()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            // Safety net: if the World scene ever loses its WorldManager reference, create one.
            if (scene.name == SceneName && FindAnyObjectByType<WorldManager>() == null)
                new GameObject("World").AddComponent<WorldManager>();
        }

        void Start()
        {
            Application.targetFrameRate = 60;

            Cam = Camera.main;
            if (Cam == null)
            {
                var camGo = new GameObject("Main Camera") { tag = "MainCamera" };
                Cam = camGo.AddComponent<Camera>();
            }
            Cam.orthographic = true;
            Cam.clearFlags = CameraClearFlags.SolidColor;
            Cam.backgroundColor = new Color(0.04f, 0.07f, 0.13f);

            cameraController = Cam.GetComponent<WorldCameraController>();
            if (cameraController == null) cameraController = Cam.gameObject.AddComponent<WorldCameraController>();
            cameraController.world = this;

            Material spriteMaterial = SpriteFactory.CreateSpriteMaterial();
            mapRenderer = new MapRenderer(transform, spriteMaterial);
            creatureRenderer = new CreatureRenderer(transform, spriteMaterial);
            effects = new EffectsRenderer(transform, spriteMaterial);
            Powers = new GodPowers(this, effects);

            UI = GetComponent<WorldUI>();
            if (UI == null) UI = gameObject.AddComponent<WorldUI>();
            UI.world = this;

            NewWorld();
        }

        /// <summary>Generate a fresh world with the current settings.</summary>
        public void NewWorld()
        {
            Selected = null;
            FollowSelected = false;
            Sim = new Simulation(settings);
            mapRenderer.Build(Sim.map);
            lastTerrainVersion = Sim.map.TerrainVersion;
            cameraController.FrameWorld(Sim.map.Width, Sim.map.Height);
            accumulator = 0f;
            if (UI != null) UI.OnNewWorld();
        }

        public void Select(Creature c)
        {
            Selected = c;
            if (c == null) FollowSelected = false;
        }

        public void SetSpeed(float multiplier)
        {
            paused = false;
            maxSpeed = false;
            speed = multiplier;
        }

        public void SetMaxSpeed()
        {
            paused = false;
            maxSpeed = true;
        }

        public void ShakeCamera(float amount)
        {
            cameraController.Shake(amount);
        }

        void Update()
        {
            if (Sim == null) return;
            HandleHotkeys();
            Powers.HandleInput(UI != null && UI.IsPointerOverUI());
            RunSimulation();
        }

        void LateUpdate()
        {
            if (Sim == null) return;

            mapTimer += Time.unscaledDeltaTime;
            bool terrainChanged = Sim.map.TerrainVersion != lastTerrainVersion;
            if (terrainChanged || mapTimer >= mapRefreshInterval)
            {
                mapRenderer.Refresh(Sim.map);
                lastTerrainVersion = Sim.map.TerrainVersion;
                mapTimer = 0f;
            }

            creatureRenderer.Sync(Sim.creatures, viewMode, Selected, Sim.Tick);
            effects.Update(Time.unscaledDeltaTime);
        }

        void RunSimulation()
        {
            float dt = Time.unscaledDeltaTime;
            int ran = 0;
            if (!paused)
            {
                var watch = System.Diagnostics.Stopwatch.StartNew();
                if (maxSpeed)
                {
                    while (watch.Elapsed.TotalMilliseconds < frameBudgetMs)
                    {
                        Sim.Step();
                        ran++;
                    }
                }
                else
                {
                    accumulator += dt * ticksPerSecond * speed;
                    while (accumulator >= 1f)
                    {
                        Sim.Step();
                        ran++;
                        accumulator -= 1f;
                        if (watch.Elapsed.TotalMilliseconds > frameBudgetMs)
                        {
                            accumulator = 0f; // can't keep up: run slower rather than freeze
                            break;
                        }
                    }
                }
            }

            ticksCounted += ran;
            measureTimer += dt;
            if (measureTimer >= 0.5f)
            {
                MeasuredTicksPerSecond = ticksCounted / measureTimer;
                ticksCounted = 0;
                measureTimer = 0f;
            }
        }

        void HandleHotkeys()
        {
            if (Input.GetKeyDown(KeyCode.Space)) paused = !paused;
            if (Input.GetKeyDown(KeyCode.Alpha1)) SetSpeed(SpeedSteps[0]);
            if (Input.GetKeyDown(KeyCode.Alpha2)) SetSpeed(SpeedSteps[1]);
            if (Input.GetKeyDown(KeyCode.Alpha3)) SetSpeed(SpeedSteps[2]);
            if (Input.GetKeyDown(KeyCode.Alpha4)) SetSpeed(SpeedSteps[3]);
            if (Input.GetKeyDown(KeyCode.Alpha5)) SetMaxSpeed();
            if (Input.GetKeyDown(KeyCode.Tab)) viewMode = (ViewMode)(((int)viewMode + 1) % 4);
            if (Input.GetKeyDown(KeyCode.F) && Selected != null) FollowSelected = !FollowSelected;
            if (Input.GetKeyDown(KeyCode.H) && UI != null) UI.showHelp = !UI.showHelp;
            if (Input.GetKeyDown(KeyCode.LeftBracket)) Powers.brushRadius = Mathf.Max(1f, Powers.brushRadius - 1f);
            if (Input.GetKeyDown(KeyCode.RightBracket)) Powers.brushRadius = Mathf.Min(30f, Powers.brushRadius + 1f);
            if (Input.GetKeyDown(KeyCode.Delete) && Selected != null && Selected.alive) Sim.Kill(Selected, "Smitten by god");
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                if (Powers.tool != GodTool.Inspect) Powers.tool = GodTool.Inspect;
                else Select(null);
            }
        }

        void OnDestroy()
        {
            if (mapRenderer != null) mapRenderer.Destroy();
        }
    }
}
