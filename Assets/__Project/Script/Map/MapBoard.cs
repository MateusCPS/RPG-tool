using System.IO;
using System.Text.RegularExpressions;
using UnityEngine;

// O mapa da mesa: mostra a imagem no chão, no tamanho certo em metros,
// e ajusta a grade para bater com os quadrados desenhados na imagem.
// Cadeia: imagem → MapBoard → TableGrid → câmera (que lê os limites da grade).
[RequireComponent(typeof(MeshRenderer))]
public class MapBoard : MonoBehaviour
{
    [SerializeField] TableGrid grid;

    [Header("Imagem de teste (enquanto não há janela de abrir arquivo)")]
    [SerializeField] Texture2D testImage;
    [SerializeField] bool guessOnStart = true; // tenta adivinhar os pixels por quadrado

    [Header("Calibração da grade")]
    [SerializeField] float pixelsPerCell = 70f;            // quantos pixels da imagem formam um quadrado
    [SerializeField] Vector2 offsetPixels = Vector2.zero;  // desloca a grade para alinhar com o desenho
    [SerializeField] float cellSize = 1.5f;                // metros por quadrado (regra da mesa)

    MeshRenderer meshRenderer;
    Material material;
    Texture2D image;
    bool started;

    public Vector2 SizeInMeters { get; private set; }
    public event System.Action MapChanged; 

    void Awake()
    {
        meshRenderer = GetComponent<MeshRenderer>();
        material = meshRenderer.material;
    }

    // Start roda depois de todos os Awake: a grade já está pronta para receber Configure().
    void Start()
    {
        started = true;
        if (testImage != null) SetImage(testImage, testImage.name, guessOnStart);
    }

    // Mexer na calibração no Inspector, durante o Play, atualiza na hora.
    void OnValidate()
    {
        if (started && image != null) Apply();
    }

    // ---------- Entrada da imagem ----------

    public void SetImage(Texture2D texture, string fileName, bool guess = true)
    {
        image = texture;
        image.wrapMode = TextureWrapMode.Clamp; // a imagem não se repete nas bordas
        material.mainTexture = image;

        if (guess) pixelsPerCell = GuessPixelsPerCell(image.width, image.height, fileName);
        Apply();
        MapChanged?.Invoke();
    }

    // Carrega uma imagem do disco (PNG ou JPG). Será usada pela janela de abrir arquivo.
    public bool LoadFromFile(string path)
    {
        if (!File.Exists(path)) return false;

        var texture = new Texture2D(2, 2, TextureFormat.RGBA32, true);
        if (!texture.LoadImage(File.ReadAllBytes(path))) // redimensiona a textura sozinho
        {
            Destroy(texture);
            return false;
        }

        texture.anisoLevel = 4;
        SetImage(texture, Path.GetFileNameWithoutExtension(path));
        return true;
    }

    // Usado pela tela de calibração do mestre, no futuro.
    public void SetCalibration(float newPixelsPerCell, Vector2 newOffsetPixels)
    {
        pixelsPerCell = newPixelsPerCell;
        offsetPixels = newOffsetPixels;
        if (image != null) Apply();
    }

    // ---------- Cálculo ----------

    void Apply()
    {
        pixelsPerCell = Mathf.Max(1f, pixelsPerCell);
        float metersPerPixel = cellSize / pixelsPerCell;

        // Tamanho do mapa no mundo: cada pixel vale metersPerPixel metros.
        SizeInMeters = new Vector2(image.width, image.height) * metersPerPixel;

        // O Quad deitado no chão, centralizado na origem do mundo.
        transform.rotation = Quaternion.Euler(90f, 0f, 0f);
        transform.localScale = new Vector3(SizeInMeters.x, SizeInMeters.y, 1f);
        transform.position = Vector3.zero;

        if (grid == null) return;

        Vector2 mapMin = -SizeInMeters / 2f;
        Vector2 mapMax = SizeInMeters / 2f;

        // Só importa o deslocamento dentro de um quadrado (de 0 a pixelsPerCell).
        Vector2 shift = new Vector2(
            Mathf.Repeat(offsetPixels.x, pixelsPerCell),
            Mathf.Repeat(offsetPixels.y, pixelsPerCell)) * metersPerPixel;

        // Com deslocamento, a grade começa um quadrado antes, para cobrir a faixa da borda.
        Vector2 gridOrigin = mapMin + shift;
        if (shift.x > 0.0001f) gridOrigin.x -= cellSize;
        if (shift.y > 0.0001f) gridOrigin.y -= cellSize;

        // Quantos quadrados cabem até a outra borda (arredondando para cima).
        int cellsX = Mathf.CeilToInt((mapMax.x - gridOrigin.x) / cellSize - 0.01f);
        int cellsY = Mathf.CeilToInt((mapMax.y - gridOrigin.y) / cellSize - 0.01f);

        grid.Configure(gridOrigin, cellsX, cellsY, cellSize);
    }

    // Chute inicial de pixels por quadrado. O mestre sempre pode corrigir depois.
    public static float GuessPixelsPerCell(int width, int height, string fileName)
    {
        // 1) Nome como "taverna_30x20": 30 quadrados de largura.
        if (!string.IsNullOrEmpty(fileName))
        {
            Match match = Regex.Match(fileName, @"(\d+)\s*[xX×]\s*(\d+)");
            // Ignora números grandes, que são resolução (ex.: 1920x1080), não quadrados.
            if (match.Success && int.TryParse(match.Groups[1].Value, out int cellsX) && cellsX >= 2 && cellsX <= 200)
                return (float)width / cellsX;
        }

        // 2) Tamanhos de quadrado comuns em mapas prontos.
        int[] common = { 70, 100, 140, 200, 50, 128, 256 };
        foreach (int size in common)
            if (width % size == 0 && height % size == 0) return size;

        // 3) Sem pista: assume cerca de 30 quadrados de largura.
        return width / 30f;
    }
}
