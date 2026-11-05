using UnityEngine;

// A aparência de um token na cena. Lê a posição do TokenData,
// exceto durante o arraste, quando segue o ponteiro livremente.
public class TokenView : MonoBehaviour
{
    [SerializeField] MeshRenderer image;                 // o Quad filho que mostra a imagem
    [SerializeField] MeshRenderer selection;             // Quad filho que aparece quando selecionado
    [SerializeField] float restHeight = 0.05f;           // altura parado (acima do destaque)
    [SerializeField] float dragHeight = 0.15f;           // altura sendo arrastado (fica por cima dos outros)
    [SerializeField, Range(0.5f, 1f)] float fill = 0.95f; // quanto do quadrado a imagem ocupa
    [SerializeField] float dragScale = 1.1f;             // cresce um pouco ao ser pego

    public TokenData Data { get; private set; }

    float cellSize = 1.5f;
    bool dragging;
    bool selected;
    Material imageMaterial; // a cópia do material deste token
    Texture2D texture;
    bool ownsTexture;       // a imagem foi carregada só para este token?

    public void Init(TokenData data, Texture2D texture, float newCellSize, bool ownsTexture = false)
    {
        Data = data;
        cellSize = newCellSize;
        this.texture = texture;
        this.ownsTexture = ownsTexture;

        imageMaterial = image.material; // cria a cópia do material só para este token
        if (texture != null) imageMaterial.mainTexture = texture;
        image.transform.localRotation = Quaternion.Euler(90f, 0f, 0f); // deita o Quad

        // A marcação de seleção fica deitada, um pouco abaixo da imagem, e começa escondida.
        if (selection != null)
        {
            selection.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            selection.transform.localPosition = new Vector3(0f, -0.01f, 0f);
            selection.enabled = false;
        }

        gameObject.name = $"Token - {data.displayName}";
        Refresh();
    }

    // O ponto do chão está em cima deste token?
    public bool Contains(Vector3 worldPoint)
    {
        float half = Data.size * cellSize / 2f;
        Vector3 center = transform.position;
        return Mathf.Abs(worldPoint.x - center.x) <= half
            && Mathf.Abs(worldPoint.z - center.z) <= half;
    }

    public void SetDragging(bool value)
    {
        dragging = value;
        Refresh();
    }

    public void SetSelected(bool value)
    {
        selected = value;
        if (selection != null) selection.enabled = value;
    }

    // Durante o arraste: move só a aparência; os dados mudam ao soltar.
    public void MoveVisual(Vector3 worldPoint)
    {
        transform.position = new Vector3(worldPoint.x, dragHeight, worldPoint.z);
    }

    // Atualiza tamanho e, se não estiver sendo arrastado, a posição a partir dos dados.
    public void Refresh()
    {
        float cell = Data.size * cellSize;
        float scale = dragging ? dragScale : 1f;

        float imageSize = cell * fill * scale;
        image.transform.localScale = new Vector3(imageSize, imageSize, 1f);

        // A seleção ocupa o quadrado inteiro; como a imagem é um pouco menor, sobra uma borda.
        if (selection != null)
        {
            float selectionSize = cell * scale;
            selection.transform.localScale = new Vector3(selectionSize, selectionSize, 1f);
        }

        if (!dragging)
            transform.position = new Vector3(Data.x, restHeight, Data.z);
    }

    // Ao apagar o token, libera a memória do material e da imagem (se ela era só dele).
    void OnDestroy()
    {
        if (imageMaterial != null) Destroy(imageMaterial);
        if (ownsTexture && texture != null) Destroy(texture);
    }
}
