using UnityEngine;
using UnityEngine.InputSystem;

public class TablePointer : MonoBehaviour
{
    [SerializeField] Camera cam;
    [SerializeField] TableGrid grid;

    static readonly Plane TablePlane = new Plane(Vector3.up, Vector3.zero);

    public bool HasPoint { get; private set; }
    public Vector3 WorldPoint { get; private set; }
    public bool IsOverGrid {get; private set; }
    public Vector2Int Cell { get; private set; }

    void Awake()
    {
        if (cam == null) cam = Camera.main;
    }

    
    void Update()
    {
        HasPoint = false;
        IsOverGrid = false;

        Pointer pointer = Pointer.current;
        if (pointer == null) return;

        if (pointer is Touchscreen && !pointer.press.isPressed) return;

        Vector2 screen = pointer.position.ReadValue();
        if (!cam.pixelRect.Contains(screen)) return;

        Ray ray = cam.ScreenPointToRay(screen);
        if(!TablePlane.Raycast(ray, out float distance)) return;

        WorldPoint = ray.GetPoint(distance);
        HasPoint = true;

        if (grid == null) return;
        Cell = grid.WorldToCell(WorldPoint);
        IsOverGrid = grid.IsInside(Cell);
    }
}
