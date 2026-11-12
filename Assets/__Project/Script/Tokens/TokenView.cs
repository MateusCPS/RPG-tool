using System;
using TMPro;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.PlayerLoop;


public class TokenView : MonoBehaviour
{
    [SerializeField] MeshRenderer image;                 
    [SerializeField] MeshRenderer selection;            
    [SerializeField] float restHeight = 0.05f;           
    [SerializeField] float dragHeight = 0.15f;           
    [SerializeField, Range(0.5f, 1f)] float fill = 0.95f;
    [SerializeField] float dragScale = 1.1f;        
    [SerializeField] TokenStatusDisplay status;

    [SerializeField] TextMeshPro initialLabel;
    [SerializeField, Range(0f, 1f)] float placeholderSaturation = 0.45f;
    [SerializeField, Range(0f, 1f)] float placeholderBrightness = 0.7f;


    public TokenData Data { get; private set; }

    float cellSize = 1.5f;
    bool dragging;
    bool selected;
    Material imageMaterial; 
    Texture2D texture;
    bool ownsTexture;      

    public void Init(TokenData data, Texture2D texture, float newCellSize, bool ownsTexture = false)
    {
        Data = data;
        cellSize = newCellSize;
        this.texture = texture;
        this.ownsTexture = ownsTexture;

        imageMaterial = image.material; // cria a cópia do material só para este token
        bool hasImage = texture != null;
        if (hasImage) imageMaterial.mainTexture = texture;
        else imageMaterial.color = PlaceholderColor(data.displayName);

        initialLabel.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
        initialLabel.gameObject.SetActive(!hasImage);
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

        if(initialLabel.gameObject.activeSelf)
        {
            initialLabel.text = Initial(Data.displayName);
            initialLabel.transform.localScale = Vector3.one * Data.size * scale;
        }
        if (status != null) status.Refresh(Data, cellSize);
    }

    static string Initial(string name)
    {
        string trimmed = name.Trim();
        return trimmed.Length > 0 ? char.ToUpperInvariant(trimmed[0]).ToString() : "?";
    }

    Color PlaceholderColor(string name)
    {
        uint hash = 2166136261;
        foreach (char c in name) hash = (hash ^ c) * 16777619;
        float hue = (hash % 360) / 360f;
        return Color.HSVToRGB(hue, placeholderSaturation, placeholderBrightness);
    }

    // Ao apagar o token, libera a memória do material e da imagem (se ela era só dele).
    void OnDestroy()
    {
        if (imageMaterial != null) Destroy(imageMaterial);
        if (ownsTexture && texture != null) Destroy(texture);
    }
}
