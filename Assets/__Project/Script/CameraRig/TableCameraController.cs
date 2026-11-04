using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;

// Câmera de cima para a mesa virtual.
// Computador: arrasta com o botão do meio do mouse e dá zoom com a roda.
// Celular: arrasta com um dedo, faz pinça com dois dedos para zoom.
// Ao carregar um mapa, enquadra o mapa inteiro na tela; o que sobra fica com fundo escuro.
[RequireComponent(typeof(Camera))]
public class TableCameraController : MonoBehaviour
{
    [Header("Zoom")]
    [SerializeField] float minSize = 3f;            // zoom máximo (mais perto)
    [SerializeField] float maxSize = 30f;           // limite usado só quando não há mapa nem grade
    [SerializeField] float scrollZoomFactor = 1.1f; // cada clique da roda aproxima ou afasta 10%

    [Header("Limites da Mesa")]
    [SerializeField] bool clampToBounds = true;
    [SerializeField] TableGrid grid;
    [SerializeField] float margin = 1f;             // folga em volta do mapa no enquadramento, em metros

    [Header("Mapa")]
    [SerializeField] MapBoard map;                  // arraste o objeto Map aqui
    [SerializeField] Color backgroundColor = new Color(0.08f, 0.08f, 0.09f); // fundo escuro em volta do mapa

    [Header("Tokens")]
    [SerializeField] TokenManager tokens;           // arraste o objeto Tokens aqui

    Camera cam;
    bool touchStartedOnToken; // o dedo atual começou em cima de um token?

    // Plano do chão (Y = 0), o mesmo do TablePointer.
    static readonly Plane TablePlane = new Plane(Vector3.up, Vector3.zero);

    void Awake()
    {
        cam = GetComponent<Camera>();
        cam.orthographic = true;
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = backgroundColor;
    }

    void OnEnable()
    {
        if (map != null) map.MapChanged += FitToGrid;
    }

    void OnDisable()
    {
        if (map != null) map.MapChanged -= FitToGrid;
    }

    void Update()
    {
        // Se houver toque na tela, ele tem prioridade; senão, usa o mouse.
        if (Touchscreen.current != null && HandleTouch()) return;
        HandleMouse();
    }

    // Roda todo quadro, depois dos movimentos: se a janela mudar de tamanho,
    // o zoom e a posição continuam dentro dos limites.
    void LateUpdate()
    {
        float limit = MaxZoom();
        if (cam.orthographicSize > limit) cam.orthographicSize = limit;
        ClampPosition();
    }

    // ---------- Mouse ----------
    void HandleMouse()
    {
        Mouse mouse = Mouse.current;
        if (mouse == null) return;

        if (mouse.middleButton.isPressed)
            Pan(mouse.delta.ReadValue());

        float scroll = mouse.scroll.ReadValue().y;
        if (scroll != 0f)
        {
            float factor = scroll > 0f ? 1f / scrollZoomFactor : scrollZoomFactor;
            ZoomAt(mouse.position.ReadValue(), cam.orthographicSize * factor);
        }
    }

    // ---------- Toque ----------
    bool HandleTouch()
    {
        TouchControl first = null, second = null;
        int count = 0;

        foreach (TouchControl touch in Touchscreen.current.touches)
        {
            if (!touch.press.isPressed) continue;
            if (count == 0) first = touch;
            else if (count == 1) second = touch;
            count++;
        }

        if (count == 0)
        {
            touchStartedOnToken = false; // nenhum dedo: zera para o próximo toque
            return false;
        }

        // Um token está sendo arrastado: a câmera fica parada, nem pinça.
        if (tokens != null && tokens.IsDragging) return true;

        if (count == 1)
        {
            // A decisão acontece no PRIMEIRO quadro do toque, aqui mesmo na câmera.
            // Se esperasse o TokenManager (que só age no LateUpdate), a mesa andaria um quadro antes.
            if (first.press.wasPressedThisFrame)
                touchStartedOnToken = tokens != null
                    && tokens.IsTokenAt(ScreenToTable(first.position.ReadValue()));

            if (!touchStartedOnToken) Pan(first.delta.ReadValue());
            return true;
        }

        // Dois dedos: pinça e arraste.
        Vector2 a = first.position.ReadValue();
        Vector2 b = second.position.ReadValue();
        Vector2 aPrev = a - first.delta.ReadValue();
        Vector2 bPrev = b - second.delta.ReadValue();

        float prevDistance = Vector2.Distance(aPrev, bPrev);
        float distance = Vector2.Distance(a, b);

        if (prevDistance > 0.01f && distance > 0.01f)
            ZoomAt((a + b) / 2f, cam.orthographicSize * prevDistance / distance);

        Pan(((a + b) - (aPrev + bPrev)) / 2f);
        return true;
    }

    // Ponto do chão embaixo de um ponto da tela.
    Vector3 ScreenToTable(Vector2 screen)
    {
        Ray ray = cam.ScreenPointToRay(screen);
        return TablePlane.Raycast(ray, out float d) ? ray.GetPoint(d) : Vector3.positiveInfinity;
    }

    // ---------- Movimento ----------

    // Move a câmera ao contrário do arraste, para a mesa "grudar" no dedo ou no mouse.
    void Pan(Vector2 screenDelta)
    {
        float unitsPerPixel = 2f * cam.orthographicSize / cam.pixelHeight;
        Vector3 move = (transform.right * screenDelta.x + transform.up * screenDelta.y) * unitsPerPixel;
        transform.position -= move;
        ClampPosition();
    }

    // Faz zoom mantendo fixo o ponto da mesa que está embaixo do cursor ou entre os dedos.
    void ZoomAt(Vector2 screenPoint, float newSize)
    {
        Vector3 before = cam.ScreenToWorldPoint(screenPoint);
        cam.orthographicSize = Mathf.Clamp(newSize, minSize, MaxZoom());
        Vector3 after = cam.ScreenToWorldPoint(screenPoint);
        transform.position += before - after;
        ClampPosition();
    }

    // ---------- Enquadramento e limites ----------

    // Centraliza no mapa e afasta o zoom até o mapa inteiro caber na tela.
    public void FitToGrid()
    {
        GetBounds(out Vector2 min, out Vector2 max);
        Vector2 center = (min + max) / 2f;
        transform.position = new Vector3(center.x, transform.position.y, center.y);
        cam.orthographicSize = MaxZoom();
        ClampPosition();
    }

    // O zoom que faz o mapa inteiro (com a margem) caber na tela.
    // Também é o limite: não adianta afastar mais do que isso.
    float MaxZoom()
    {
        if (map == null && grid == null) return maxSize;

        GetBounds(out Vector2 min, out Vector2 max);
        Vector2 size = max - min + Vector2.one * margin * 2f;

        // orthographicSize é metade da altura visível; a largura visível é altura × proporção da tela.
        float fitHeight = size.y / 2f;
        float fitWidth = size.x / 2f / cam.aspect;
        // "Caber": escolhe o lado que precisa de mais espaço, para nada ficar de fora.
        return Mathf.Max(minSize, Mathf.Max(fitHeight, fitWidth));
    }

    // Impede a borda da tela de passar da borda do mapa.
    // Num eixo em que o mapa é menor que a tela, a câmera fica centralizada.
    void ClampPosition()
    {
        if (!clampToBounds) return;

        GetBounds(out Vector2 min, out Vector2 max);

        // Metade do que a câmera enxerga, em metros.
        float halfHeight = cam.orthographicSize;
        float halfWidth = halfHeight * cam.aspect;

        Vector3 p = transform.position;
        p.x = ClampAxis(p.x, min.x + halfWidth, max.x - halfWidth);
        p.z = ClampAxis(p.z, min.y + halfHeight, max.y - halfHeight);
        transform.position = p;
    }

    // Se a tela for maior que o mapa nesse eixo, o intervalo "inverte": aí só centraliza.
    static float ClampAxis(float value, float low, float high) =>
        low <= high ? Mathf.Clamp(value, low, high) : (low + high) / 2f;

    // Limites do mapa no mundo (x = X, y = Z). O MapBoard deixa o mapa centralizado na origem.
    void GetBounds(out Vector2 min, out Vector2 max)
    {
        if (map != null && map.SizeInMeters != Vector2.zero)
        {
            max = map.SizeInMeters / 2f;
            min = -max;
        }
        else if (grid != null)
        {
            min = grid.Min;
            max = grid.Max;
        }
        else
        {
            min = Vector2.one * -1000f;
            max = Vector2.one * 1000f;
        }
    }
}
