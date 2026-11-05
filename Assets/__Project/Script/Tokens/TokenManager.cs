using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;


public class TokenManager : MonoBehaviour
{
    [SerializeField] TablePointer pointer;
    [SerializeField] TableGrid grid;
    [SerializeField] TokenView tokenPrefab;
    [SerializeField] MapBoard map;

    [Header("Tokens de teste")]
    [SerializeField] Texture2D[] testImages;

    readonly List<TokenView> tokens = new List<TokenView>();
    IMovementRule rule;
    TokenView dragged;
    TokenView selected;
    Vector3 grabOffset;
    Vector3 dragStart;
    public bool IsDragging => dragged != null;
    public TokenView Dragged => dragged;
    public TokenView Selected => selected;
    public Vector3 DragStart => dragStart;
    public IMovementRule Rule => rule;

    // Avisa quando a seleção muda (o botão Apagar usa).
    public event Action SelectionChanged;

    static readonly Plane TablePlane = new Plane(Vector3.up, Vector3.zero);

    public enum MovementMode { Grip, Free }

    [Header("Modo de movimento")]
    [SerializeField] MovementMode mode = MovementMode.Grip;

    void OnEnable()
    {
        if (map != null) map.MapChanged += OnMapChanged;
    }

    void OnDisable()
    {
        if (map != null) map.MapChanged -= OnMapChanged;
    }

    // Com um mapa novo, reaplica a regra para encaixar e prender os tokens na grade nova.
    void OnMapChanged()
    {
        if (rule != null) SetMode(mode);
    }

    IEnumerator Start()
    {
        SetMode(mode);

        yield return null;

        for (int i = 0; i < testImages.Length; i++)
        {
            var data = new TokenData { displayName = testImages[i].name };
            data.Position = grid.CellToWorldCenter(new Vector2Int(2 + i * 2, 2));
            Spawn(data, testImages[i]);
        }
    }

    // ownsTexture = true quando a imagem foi carregada só para este token
    // (assim o token libera a memória ao ser apagado). As imagens de teste são da Unity: false.
    public TokenView Spawn(TokenData data, Texture2D image, bool ownsTexture = false)
    {
        data.Position = rule.Snap(data.Position, data.size);
        TokenView view = Instantiate(tokenPrefab, transform);
        view.Init(data, image, grid.CellSize, ownsTexture);
        tokens.Add(view);
        return view;
    }

    // Cria o token no ponto da mesa que está no centro da tela, e já seleciona.
    public TokenView SpawnAtScreenCenter(TokenData data, Texture2D image, bool ownsTexture)
    {
        Camera cam = Camera.main;
        Ray ray = cam.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
        if (TablePlane.Raycast(ray, out float distance))
            data.Position = ray.GetPoint(distance);

        TokenView view = Spawn(data, image, ownsTexture);
        Select(view);
        return view;
    }

    public void Select(TokenView token)
    {
        if (selected == token) return;

        if (selected != null) selected.SetSelected(false);
        selected = token;
        if (selected != null) selected.SetSelected(true);

        SelectionChanged?.Invoke();
    }

    public void DeleteSelected() => Delete(selected);

    public void Delete(TokenView token)
    {
        if (token == null || token == dragged) return; // não apaga o token que está sendo arrastado

        tokens.Remove(token);
        if (selected == token) Select(null);
        Destroy(token.gameObject);
    }

    public void SetMode(MovementMode newMode)
    {
        mode = newMode;
        rule = mode == MovementMode.Grip
        ? new GridMovementRule(grid)
        : new FreeMovementRule(grid);

        foreach (var token in tokens)
        {
            token.Data.Position = rule.Snap(token.Data.Position, token.Data.size);
            token.Refresh();
        }
    }

    void OnValidate()
    {
        if (Application.isPlaying && tokens != null)
            SetMode(mode);
    }


    public bool IsTokenAt(Vector3 worldPoint) => FindTokenAt(worldPoint) != null;

    TokenView FindTokenAt(Vector3 worldPoint)
    {
        for (int i = tokens.Count - 1; i >= 0; i--)
            if (tokens[i].Contains(worldPoint)) return tokens[i];
        return null;
    }

    void LateUpdate()
    {
        HandleDeleteKey();

        Pointer device = Pointer.current;
        if (device == null || rule == null) return;

        // O clique na mesa (fora da interface) seleciona o token embaixo, ou limpa a seleção.
        if (device.press.wasPressedThisFrame && pointer.HasPoint && !IsPointerOverUI())
            PressOnTable(pointer.WorldPoint);

        if (dragged == null) return;

        if (device.press.isPressed)
        {
            if (pointer.HasPoint) dragged.MoveVisual(pointer.WorldPoint + grabOffset);
        }
        else
        {
            EndDrag();
        }
    }

    // O clique é em cima de um botão ou painel?
    static bool IsPointerOverUI() =>
        EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();

    // Clicar num token seleciona e começa o arraste; clicar no vazio limpa a seleção.
    void PressOnTable(Vector3 point)
    {
        TokenView found = FindTokenAt(point);
        Select(found);
        if (found != null) BeginDrag(found, point);
    }

    // A tecla Delete apaga o token selecionado.
    void HandleDeleteKey()
    {
        Keyboard keyboard = Keyboard.current;
        if (keyboard == null || !keyboard.deleteKey.wasPressedThisFrame) return;

        // Se a pessoa estiver digitando num campo de texto, o Delete é para o texto, não para o token.
        GameObject focused = EventSystem.current != null ? EventSystem.current.currentSelectedGameObject : null;
        if (focused != null && focused.GetComponent<TMP_InputField>() != null) return;

        DeleteSelected();
    }

    void BeginDrag(TokenView token, Vector3 point)
    {
        dragged = token;
        dragStart = dragged.Data.Position;
        grabOffset = dragged.transform.position - point;
        grabOffset.y = 0f;
        dragged.SetDragging(true);
    }

    void EndDrag()
    {
        Vector3 dropped = dragged.transform.position;
        dragged.Data.Position = rule.Snap(dropped, dragged.Data.size);
        dragged.SetDragging(false);
        dragged = null;
    }
}
