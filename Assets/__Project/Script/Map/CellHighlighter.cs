using UnityEngine;

[RequireComponent(typeof(MeshRenderer))]
public class CellHighlighter : MonoBehaviour
{
    [SerializeField] TablePointer pointer;
    [SerializeField] TableGrid grid;
    [SerializeField, Range(0.5f, 1f)] float fill = 0.9f;
    [SerializeField] float height = 0.02f;

    MeshRenderer MeshRenderer;

    void Awake()
    {
        MeshRenderer = GetComponent<MeshRenderer>();
        transform.rotation = Quaternion.Euler(90f, 0f, 0f);
        MeshRenderer.enabled = false;
    }

    void LateUpdate()
    {
        bool show = pointer != null && grid != null && pointer.IsOverGrid;
        MeshRenderer.enabled = show;
        if(!show) return;

        Vector3 center = grid.CellToWorldCenter(pointer.Cell);
        transform.position = new Vector3(center.x, height, center.z);

        float size = grid.CellSize * fill;
        transform.localScale = new Vector3(size, size, 1f);
    }
}
