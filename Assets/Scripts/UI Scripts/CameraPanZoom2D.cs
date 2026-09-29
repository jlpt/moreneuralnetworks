using UnityEngine;

public class CameraPanZoom2D_Consistent : MonoBehaviour
{
    [Header("Panning")]
    public float panSpeed = 1.0f;
    public int panMouseButton = 1;   // 0=Left, 1=Right, 2=Middle

    [Header("Zoom (consistent scaling)")]
    public float zoomFactor = 0.15f;     // Percent zoom per wheel step (0.1–0.2 is typical)
    public float minZoom = 2f;
    public float maxZoom = 40f;

    [Header("Bounds (camera center)")]
    public float minX = -50f;
    public float maxX = 50f;
    public float minY = -50f;
    public float maxY = 50f;

    private Camera cam;
    private Vector3 dragOrigin;

    void Awake()
    {
        cam = Camera.main;
    }

    void Update()
    {
        HandlePanning();
        HandleZoomTowardsMouseConsistent();
        ClampPosition();
    }

    private void HandlePanning()
    {
        if (Input.GetMouseButtonDown(panMouseButton))
        {
            dragOrigin = cam.ScreenToWorldPoint(Input.mousePosition);
        }

        if (Input.GetMouseButton(panMouseButton))
        {
            Vector3 cur = cam.ScreenToWorldPoint(Input.mousePosition);
            Vector3 diff = dragOrigin - cur;
            transform.position += diff * panSpeed;
        }
    }

    private void HandleZoomTowardsMouseConsistent()
    {
        float scroll = Input.GetAxis("Mouse ScrollWheel");
        if (Mathf.Abs(scroll) < 0.0001f) return;

        // World point BEFORE zoom
        Vector3 before = cam.ScreenToWorldPoint(Input.mousePosition);

        // Consistent exponential zooming
        float scale = 1f - scroll * zoomFactor;
        float newSize = cam.orthographicSize * scale;

        cam.orthographicSize = Mathf.Clamp(newSize, minZoom, maxZoom);

        // World point AFTER zoom
        Vector3 after = cam.ScreenToWorldPoint(Input.mousePosition);

        // Shift the camera so it zooms toward the mouse
        transform.position += before - after;
    }

    private void ClampPosition()
    {
        float clampedX = Mathf.Clamp(transform.position.x, minX, maxX);
        float clampedY = Mathf.Clamp(transform.position.y, minY, maxY);

        transform.position = new Vector3(clampedX, clampedY, transform.position.z);
    }
}
