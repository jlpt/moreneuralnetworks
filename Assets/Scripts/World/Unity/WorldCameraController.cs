using UnityEngine;

namespace NeuronWorld
{
    /// <summary>
    /// 2D camera: right/middle-drag or WASD/arrows to pan, scroll to zoom towards the cursor
    /// (same approach as Neuron-Box's CameraPanZoom2D), and smooth follow of the selected creature.
    /// </summary>
    [RequireComponent(typeof(Camera))]
    public class WorldCameraController : MonoBehaviour
    {
        public WorldManager world;
        [Tooltip("Percent zoom per scroll step.")]
        public float zoomFactor = 0.15f;
        public float minZoom = 3f;
        [Tooltip("Screens per second when panning with the keyboard.")]
        public float keyPanSpeed = 1.2f;

        Camera cam;
        Vector3 dragOrigin;
        bool dragging;
        float maxZoom = 100f;
        float worldWidth, worldHeight;
        float shake;

        void Awake()
        {
            cam = GetComponent<Camera>();
        }

        public void FrameWorld(float width, float height)
        {
            worldWidth = width;
            worldHeight = height;
            float aspect = Mathf.Max(0.5f, cam.aspect);
            float fit = Mathf.Max(height * 0.5f, width * 0.5f / aspect);
            maxZoom = fit * 1.3f;
            cam.orthographicSize = fit * 1.05f;
            transform.position = new Vector3(width * 0.5f, height * 0.5f, -10f);
        }

        public void Shake(float amount)
        {
            shake = Mathf.Max(shake, amount);
        }

        void LateUpdate()
        {
            if (world == null || world.Sim == null) return;
            float dt = Time.unscaledDeltaTime;
            bool overUI = world.UI != null && world.UI.IsPointerOverUI();

            // Drag to pan.
            if ((Input.GetMouseButtonDown(1) || Input.GetMouseButtonDown(2)) && !overUI)
            {
                dragging = true;
                dragOrigin = cam.ScreenToWorldPoint(Input.mousePosition);
                world.FollowSelected = false;
            }
            if (dragging && (Input.GetMouseButton(1) || Input.GetMouseButton(2)))
            {
                Vector3 current = cam.ScreenToWorldPoint(Input.mousePosition);
                Vector3 diff = dragOrigin - current;
                diff.z = 0f;
                transform.position += diff;
            }
            else
            {
                dragging = false;
            }

            // Keyboard pan.
            Vector2 move = Vector2.zero;
            if (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow)) move.x -= 1f;
            if (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow)) move.x += 1f;
            if (Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow)) move.y -= 1f;
            if (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow)) move.y += 1f;
            if (move != Vector2.zero)
            {
                transform.position += (Vector3)(move.normalized * cam.orthographicSize * 2f * keyPanSpeed * dt);
                world.FollowSelected = false;
            }

            // Zoom towards the cursor.
            float scroll = Input.mouseScrollDelta.y;
            if (Input.GetKey(KeyCode.Equals) || Input.GetKey(KeyCode.KeypadPlus)) scroll += 6f * dt;
            if (Input.GetKey(KeyCode.Minus) || Input.GetKey(KeyCode.KeypadMinus)) scroll -= 6f * dt;
            if (Mathf.Abs(scroll) > 0.0001f && !overUI)
            {
                Vector3 before = cam.ScreenToWorldPoint(Input.mousePosition);
                float scale = 1f - Mathf.Clamp(scroll, -3f, 3f) * zoomFactor;
                cam.orthographicSize = Mathf.Clamp(cam.orthographicSize * scale, minZoom, maxZoom);
                Vector3 after = cam.ScreenToWorldPoint(Input.mousePosition);
                Vector3 shift = before - after;
                shift.z = 0f;
                if (!world.FollowSelected) transform.position += shift;
            }

            // Follow the selected creature.
            Creature selected = world.Selected;
            if (world.FollowSelected && selected != null && selected.alive)
            {
                Vector3 target = new Vector3(selected.x, selected.y, transform.position.z);
                transform.position = Vector3.Lerp(transform.position, target, 1f - Mathf.Exp(-8f * dt));
            }

            // Keep the camera over the world.
            Vector3 p = transform.position;
            p.x = Mathf.Clamp(p.x, 0f, worldWidth);
            p.y = Mathf.Clamp(p.y, 0f, worldHeight);
            p.z = -10f;
            transform.position = p;

            if (shake > 0f)
            {
                shake = Mathf.Max(0f, shake - dt * 1.5f);
                Vector2 jitter = Random.insideUnitCircle * shake * cam.orthographicSize * 0.03f;
                transform.position += new Vector3(jitter.x, jitter.y, 0f);
            }
        }
    }
}
