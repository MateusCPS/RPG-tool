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

    [Header("Limites da Mesa")]
    [SerializeField] bool clampToBounds = true;
    [SerializeField] TableGrid grid;
    [SerializeField] float margin = 2f;

    Camera cam;

    void Awake()
    {
        cam = GetComponent<Camera>();
        cam.orthographic = true;
    }

    // Update is called once per frame
    void Update()
    {
        if (Touchscreen.current != null && HandleTouch()) return;
        HandleMouse();
    }

    void HandleMouse()
    {
        Mouse mouse = Mouse.current;
        if(mouse == null) return;

        if(mouse.middleButton.isPressed)
            Pan(mouse.delta.ReadValue());

        float scroll = mouse.scroll.ReadValue().y;
        if(scroll != 0f)
        {
            float factor = scroll > 0f ? 1f / scrollZoomFactor : scrollZoomFactor;
            ZoomAt(mouse.position.ReadValue(), cam.orthographicSize * factor);
        }
    }

    bool HandleTouch()
    {
        TouchControl first = null, second = null;
        int count = 0;

        foreach (TouchControl touch in Touchscreen.current.touches)
        {
            if(!touch.press.isPressed) continue;
            if(count == 0) first = touch;
            else if(count ==1) second = touch;
            count++;
        }

        if(count == 1)
        {
            Pan(first.delta.ReadValue());
            return true;
        }

        if(count >= 2)
        {
            Vector2 a = first.position.ReadValue();
            Vector2 b = second.position.ReadValue();
            Vector2 aPrev = a - first.delta.ReadValue();
            Vector2 bPrev = b - second.delta.ReadValue();

            float prevDistance = Vector2.Distance(aPrev, bPrev);
            float distance = Vector2.Distance(a, b);

            if(prevDistance > 0.01f && distance > 0.01f)
                ZoomAt((a + b) / 2f, cam.orthographicSize * prevDistance / distance);
            
            Pan(((a + b) - (aPrev + bPrev)) / 2f);
            return true;
        }
        return false;
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
        cam.orthographicSize = Mathf.Clamp(newSize, minSize, maxSize);
        Vector3 after = cam.ScreenToWorldPoint(screenPoint);
        transform.position += before - after;
        ClampPosition();
    }

    void ClampPosition()
    {
        if (!clampToBounds || grid == null) return;

        Vector2 min = grid.Min - Vector2.one * margin;
        Vector2 max = grid.Max + Vector2.one * margin;

        Vector3 p = transform.position;
        p.x = Mathf.Clamp(p.x, min.x, max.x);
        p.z = Mathf.Clamp(p.z, min.y, max.y);
        transform.position = p;
    }
}
