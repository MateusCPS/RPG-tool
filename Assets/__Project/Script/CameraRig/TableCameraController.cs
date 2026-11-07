using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;

[RequireComponent(typeof(Camera))]
public class TableCameraController : MonoBehaviour
{
    [Header("Zoom")]
    [SerializeField] float minSize = 3f;
    [SerializeField] float maxSize = 30f;
    [SerializeField] float scrollZoomFactor = 1.1f;
    [SerializeField] float zoomSmoothness = 12f;

    [Header("Limites da Mesa")]
    [SerializeField] bool clampToBounds = true;
    [SerializeField] TableGrid grid;
    [SerializeField] float margin = 1f;

    [Header("Mapa")]
    [SerializeField] MapBoard map;
    [SerializeField] Color backgroundColor = new Color(0.08f, 0.08f, 0.09f);

    [Header("Tokens")]
    [SerializeField] TokenManager tokens;

    Camera cam;
    bool touchStartedOnToken;
    float targetSize;
    Vector2 zoomAnchor;
    bool smoothZooming;

    static readonly Plane TablePlane = new Plane(Vector3.up, Vector3.zero);

    void Awake()
    {
        cam = GetComponent<Camera>();
        cam.orthographic = true;
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = backgroundColor;
        targetSize = cam.orthographicSize;
    }

    void OnEnable()
    {
        if (map != null) map.MapChanged += FitToGrid;
    }

    void OnDisable()
    {
        if (map != null) map.MapChanged -= FitToGrid;
    }

    void Update()
    {
        bool touched = Touchscreen.current != null && HandleTouch();
        if (!touched) HandleMouse();
        UpdateSmoothZoom();
    }

    void LateUpdate()
    {
        float limit = MaxZoom();
        if (cam.orthographicSize > limit) cam.orthographicSize = limit;
        if (targetSize > limit) targetSize = limit;
        ClampPosition();
    }

    void HandleMouse()
    {
        Mouse mouse = Mouse.current;
        if (mouse == null) return;

        if (mouse.middleButton.isPressed)
            Pan(mouse.delta.ReadValue());

        float scroll = mouse.scroll.ReadValue().y;
        if (scroll != 0f)
        {
            float factor = scroll > 0f ? 1f / scrollZoomFactor : scrollZoomFactor;
            targetSize = Mathf.Clamp(targetSize * factor, minSize, MaxZoom());
            zoomAnchor = mouse.position.ReadValue();
            smoothZooming = true;
        }
    }

    void UpdateSmoothZoom()
    {
        if (!smoothZooming) return;

        float t = 1f - Mathf.Exp(-zoomSmoothness * Time.deltaTime);
        float size = Mathf.Lerp(cam.orthographicSize, targetSize, t);

        if (Mathf.Abs(size - targetSize) < 0.001f)
        {
            size = targetSize;
            smoothZooming = false;
        }

        ZoomAt(zoomAnchor, size);
    }

    bool HandleTouch()
    {
        TouchControl first = null, second = null;
        int count = 0;

        foreach (TouchControl touch in Touchscreen.current.touches)
        {
            if (!touch.press.isPressed) continue;
            if (count == 0) first = touch;
            else if (count == 1) second = touch;
            count++;
        }

        if (count == 0)
        {
            touchStartedOnToken = false;
            return false;
        }

        if (tokens != null && tokens.IsDragging) return true;

        if (count == 1)
        {
            if (first.press.wasPressedThisFrame)
                touchStartedOnToken = tokens != null
                    && tokens.IsTokenAt(ScreenToTable(first.position.ReadValue()));

            if (!touchStartedOnToken) Pan(first.delta.ReadValue());
            return true;
        }

        Vector2 a = first.position.ReadValue();
        Vector2 b = second.position.ReadValue();
        Vector2 aPrev = a - first.delta.ReadValue();
        Vector2 bPrev = b - second.delta.ReadValue();

        float prevDistance = Vector2.Distance(aPrev, bPrev);
        float distance = Vector2.Distance(a, b);

        if (prevDistance > 0.01f && distance > 0.01f)
        {
            ZoomAt((a + b) / 2f, cam.orthographicSize * prevDistance / distance);
            targetSize = cam.orthographicSize;
            smoothZooming = false;
        }

        Pan(((a + b) - (aPrev + bPrev)) / 2f);
        return true;
    }

    Vector3 ScreenToTable(Vector2 screen)
    {
        Ray ray = cam.ScreenPointToRay(screen);
        return TablePlane.Raycast(ray, out float d) ? ray.GetPoint(d) : Vector3.positiveInfinity;
    }

    void Pan(Vector2 screenDelta)
    {
        float unitsPerPixel = 2f * cam.orthographicSize / cam.pixelHeight;
        Vector3 move = (transform.right * screenDelta.x + transform.up * screenDelta.y) * unitsPerPixel;
        transform.position -= move;
        ClampPosition();
    }

    void ZoomAt(Vector2 screenPoint, float newSize)
    {
        Vector3 before = cam.ScreenToWorldPoint(screenPoint);
        cam.orthographicSize = Mathf.Clamp(newSize, minSize, MaxZoom());
        Vector3 after = cam.ScreenToWorldPoint(screenPoint);
        transform.position += before - after;
        ClampPosition();
    }

    public void FitToGrid()
    {
        GetBounds(out Vector2 min, out Vector2 max);
        Vector2 center = (min + max) / 2f;
        transform.position = new Vector3(center.x, transform.position.y, center.y);
        cam.orthographicSize = MaxZoom();
        targetSize = cam.orthographicSize;
        smoothZooming = false;
        ClampPosition();
    }

    float MaxZoom()
    {
        if (map == null && grid == null) return maxSize;

        GetBounds(out Vector2 min, out Vector2 max);
        Vector2 size = max - min + Vector2.one * margin * 2f;

        float fitHeight = size.y / 2f;
        float fitWidth = size.x / 2f / cam.aspect;
        return Mathf.Max(minSize, Mathf.Max(fitHeight, fitWidth));
    }

    void ClampPosition()
    {
        if (!clampToBounds) return;

        GetBounds(out Vector2 min, out Vector2 max);

        float halfHeight = cam.orthographicSize;
        float halfWidth = halfHeight * cam.aspect;

        Vector3 p = transform.position;
        p.x = ClampAxis(p.x, min.x + halfWidth, max.x - halfWidth);
        p.z = ClampAxis(p.z, min.y + halfHeight, max.y - halfHeight);
        transform.position = p;
    }

    static float ClampAxis(float value, float low, float high) =>
        low <= high ? Mathf.Clamp(value, low, high) : (low + high) / 2f;

    void GetBounds(out Vector2 min, out Vector2 max)
    {
        if (map != null && map.SizeInMeters != Vector2.zero)
        {
            max = map.SizeInMeters / 2f;
            min = -max;
        }
        else if (grid != null)
        {
            min = grid.Min;
            max = grid.Max;
        }
        else
        {
            min = Vector2.one * -1000f;
            max = Vector2.one * 1000f;
        }
    }
}
