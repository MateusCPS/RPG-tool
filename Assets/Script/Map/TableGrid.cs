using UnityEngine;

[RequireComponent(typeof(MeshRenderer))]
public class TableGrid : MonoBehaviour
{
    [Header("Tamanho")]
    [SerializeField] int widthInCells = 30;
    [SerializeField] int heightInCells = 30;
    [SerializeField] float cellSize = 1.5f;
    [SerializeField] Vector2 offset = Vector2.zero;

    [Header("Aparência")]
    [SerializeField] Color lineColor = new Color(0f, 0f, 0f, 0.5f);
    [SerializeField, Range(2, 8)] int lineThickness = 2;
    [SerializeField] bool visible = true;

    const int TextureSize = 64;

    MeshRenderer meshRenderer;
    Material material;
    Texture2D texture;

    public float CellSize => cellSize;

    // Canto inferior esquerdo da grade no mundo (x = X, y = Z).
    Vector2 Origin => offset - new Vector2(widthInCells * cellSize, heightInCells * cellSize) / 2f; // ← faltava

    public Vector2 Min => Origin;
    public Vector2 Max => Origin + new Vector2(widthInCells, heightInCells) * cellSize;

    void Awake()
    {
        meshRenderer = GetComponent<MeshRenderer>();
        material = meshRenderer.material;

        texture = new Texture2D(TextureSize, TextureSize, TextureFormat.RGBA32, true);
        texture.wrapMode = TextureWrapMode.Repeat;
        texture.filterMode = FilterMode.Bilinear;
        texture.anisoLevel = 4;
        material.mainTexture = texture;

        Apply();
    }

    void OnValidate()
    {
        if (material != null && texture != null) Apply();
    }

    void Apply()
    {
        DrawTexture();

        transform.rotation = Quaternion.Euler(90f, 0f, 0f);
        transform.localScale = new Vector3(widthInCells * cellSize, heightInCells * cellSize, 1f);
        transform.position = new Vector3(offset.x, 0.01f, offset.y); // ← faltava

        material.mainTextureScale = new Vector2(widthInCells, heightInCells);
        meshRenderer.enabled = visible;
    }

    void DrawTexture()
    {
        int half = lineThickness / 2;
        Color32 line = lineColor;
        Color32 empty = new Color32(0, 0, 0, 0);
        var pixels = new Color32[TextureSize * TextureSize];

        for (int y = 0; y < TextureSize; y++)
        {
            for (int x = 0; x < TextureSize; x++)
            {
                bool onEdge = x < half || x >= TextureSize - half || y < half || y >= TextureSize - half;
                pixels[y * TextureSize + x] = onEdge ? line : empty;
            }
        }

        texture.SetPixels32(pixels);
        texture.Apply(true);
    }

    public Vector2Int WorldToCell(Vector3 world)
    {
        Vector2 o = Origin;
        return new Vector2Int(
            Mathf.FloorToInt((world.x - o.x) / cellSize),
            Mathf.FloorToInt((world.z - o.y) / cellSize));
    }

    public Vector3 CellToWorldCenter(Vector2Int cell)
    {
        Vector2 o = Origin;
        return new Vector3(
            o.x + (cell.x + 0.5f) * cellSize, 0f,
            o.y + (cell.y + 0.5f) * cellSize);
    }

    public bool IsInside(Vector2Int cell) =>
        cell.x >= 0 && cell.y >= 0 && cell.x < widthInCells && cell.y < heightInCells;
}