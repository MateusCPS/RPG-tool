using UnityEngine;

// Grade visual da mesa. Gera a textura do quadriculado por código e ajusta o Quad.
// Quando existe um MapBoard, ele chama Configure() e define tamanho e posição da grade.
[RequireComponent(typeof(MeshRenderer))]
public class TableGrid : MonoBehaviour
{
    [Header("Tamanho (o MapBoard sobrescreve no Play)")]
    [SerializeField] int widthInCells = 30;
    [SerializeField] int heightInCells = 30;
    [SerializeField] float cellSize = 1.5f;
    [SerializeField] Vector2 origin = new Vector2(-22.5f, -22.5f); // canto inferior esquerdo (X, Z), em metros

    [Header("Aparência")]
    [SerializeField] Color lineColor = new Color(0f, 0f, 0f, 0.5f);
    [SerializeField, Range(2, 8)] int lineThickness = 2;
    [SerializeField] bool visible = true;

    const int TextureSize = 64;

    MeshRenderer meshRenderer;
    Material material;
    Texture2D texture;

    public float CellSize => cellSize;
    public Vector2 Min => origin;
    public Vector2 Max => origin + new Vector2(widthInCells, heightInCells) * cellSize;

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

    // Chamado pelo MapBoard sempre que a imagem ou a calibração mudam.
    public void Configure(Vector2 newOrigin, int width, int height, float newCellSize)
    {
        origin = newOrigin;
        widthInCells = Mathf.Max(1, width);
        heightInCells = Mathf.Max(1, height);
        cellSize = Mathf.Max(0.01f, newCellSize);
        if (material != null) Apply();
    }

    void Apply()
    {
        DrawTexture();

        float width = widthInCells * cellSize;
        float height = heightInCells * cellSize;

        transform.rotation = Quaternion.Euler(90f, 0f, 0f);
        transform.localScale = new Vector3(width, height, 1f);
        // O Quad é posicionado pelo centro; a origem é o canto, então soma metade do tamanho.
        transform.position = new Vector3(origin.x + width / 2f, 0.01f, origin.y + height / 2f);

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
        return new Vector2Int(
            Mathf.FloorToInt((world.x - origin.x) / cellSize),
            Mathf.FloorToInt((world.z - origin.y) / cellSize));
    }

    public Vector3 CellToWorldCenter(Vector2Int cell)
    {
        return new Vector3(
            origin.x + (cell.x + 0.5f) * cellSize, 0f,
            origin.y + (cell.y + 0.5f) * cellSize);
    }

    public bool IsInside(Vector2Int cell) =>
        cell.x >= 0 && cell.y >= 0 && cell.x < widthInCells && cell.y < heightInCells;
}
