using TMPro;
using UnityEngine;

// Mostra a distância do arraste acima do token: "4,5 m / 9 m".
// Fica vermelho quando passa do deslocamento do token, mas não bloqueia (GDD: o mestre decide).
[DefaultExecutionOrder(100)] // roda depois dos outros scripts: o token já se moveu neste quadro
public class DragDistanceLabel : MonoBehaviour
{
    [SerializeField] TokenManager tokens;
    [SerializeField] TableGrid grid;
    [SerializeField] TextMeshPro label;
    [SerializeField] Color okColor = Color.white;
    [SerializeField] Color overColor = new Color(1f, 0.35f, 0.35f);
    [SerializeField] float height = 0.3f; // acima do token arrastado
    [SerializeField] float gap = 0.3f;    // espaço entre a borda do token e o texto

    void Awake()
    {
        label.transform.rotation = Quaternion.Euler(90f, 0f, 0f); // deitado, de frente para a câmera
        label.gameObject.SetActive(false);
    }

    void LateUpdate()
    {
        if (tokens == null || !tokens.IsDragging)
        {
            label.gameObject.SetActive(false);
            return;
        }

        TokenView token = tokens.Dragged;
        IMovementRule rule = tokens.Rule;

        // Mede até onde o token VAI CAIR (encaixado), não até a posição livre do mouse.
        Vector3 snapped = rule.Snap(token.transform.position, token.Data.size);
        float distance = rule.MeasureDistance(tokens.DragStart, snapped);
        float speed = token.Data.speed;

        // "0.#" mostra no máximo uma casa decimal: 9 → "9", 4.5 → "4,5" (vírgula no Windows em português).
        label.text = $"{distance:0.#} m / {speed:0.#} m";
        label.color = distance > speed + 0.001f ? overColor : okColor;

        // Posiciona acima do token na tela (+Z no mundo), logo depois da borda dele.
        float half = token.Data.size * grid.CellSize / 2f;
        Vector3 p = token.transform.position;
        label.transform.position = new Vector3(p.x, height, p.z + half + gap);
        label.gameObject.SetActive(true);
    }
}