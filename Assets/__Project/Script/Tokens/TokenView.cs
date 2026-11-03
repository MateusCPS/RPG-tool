using UnityEngine;

// A aparência de um token na cena. Lê a posição do TokenData,
// exceto durante o arraste, quando segue o ponteiro livremente.
public class TokenView : MonoBehaviour
{
    [SerializeField] MeshRenderer image;                 // o Quad filho que mostra a imagem
    [SerializeField] float restHeight = 0.05f;           // altura parado (acima do destaque)
    [SerializeField] float dragHeight = 0.15f;           // altura sendo arrastado (fica por cima dos outros)
    [SerializeField, Range(0.5f, 1f)] float fill = 0.95f; // quanto do quadrado a imagem ocupa
    [SerializeField] float dragScale = 1.1f;             // cresce um pouco ao ser pego

    public TokenData Data { get; private set; }

    float cellSize = 1.5f;
    bool dragging;

    public void Init(TokenData data, Texture2D texture, float newCellSize)
    {
        Data = data;
        cellSize = newCellSize;
        if (texture != null) image.material.mainTexture = texture;
        image.transform.localRotation = Quaternion.Euler(90f, 0f, 0f); // deita o Quad
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

    // Durante o arraste: move só a aparência; os dados mudam ao soltar.
    public void MoveVisual(Vector3 worldPoint)
    {
        transform.position = new Vector3(worldPoint.x, dragHeight, worldPoint.z);
    }

    // Atualiza tamanho e, se não estiver sendo arrastado, a posição a partir dos dados.
    public void Refresh()
    {
        float size = Data.size * cellSize * fill * (dragging ? dragScale : 1f);
        image.transform.localScale = new Vector3(size, size, 1f);

        if (!dragging)
            transform.position = new Vector3(Data.x, restHeight, Data.z);
    }
}
