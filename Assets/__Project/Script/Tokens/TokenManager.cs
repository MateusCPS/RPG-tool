using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.Netcode;
using Unity.VisualScripting.Antlr3.Runtime;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;


public class TokenManager : MonoBehaviour, INetworkPrefabInstanceHandler
{
    [SerializeField] TablePointer pointer;
    [SerializeField] TableGrid grid;
    [SerializeField] TokenView tokenPrefab;
    [SerializeField] MapBoard map;
    [SerializeField] NetworkManager network;

    [Header("Tokens de teste")]
    [SerializeField] Texture2D[] testImages;
    [SerializeField] float dragSendInterval = 0.05f;
    [SerializeField] ImageService images;

    readonly List<TokenView> tokens = new List<TokenView>();
    IMovementRule rule;
    TokenView dragged;
    TokenView selected;
    Vector3 grabOffset;
    Vector3 dragStart;
    Vector3 lastSentDrag;
    float nextDragSend;
    public bool IsDragging => dragged != null;
    public TokenView Dragged => dragged;
    public TokenView Selected => selected;
    public Vector3 DragStart => dragStart;
    public IMovementRule Rule => rule;

    bool IsClientOnly => network.IsListening && !network.IsServer;

    // Avisa quando a seleção muda (o botão Apagar usa).
    public event Action SelectionChanged;
    public event Action ModeChanged;
    
    public event Action<TokenView> TokenChanged;
    public bool CanManageTokens => !IsClientOnly;

    static readonly Plane TablePlane = new Plane(Vector3.up, Vector3.zero);

    public enum MovementMode { Grip, Free }
    public MovementMode Mode => mode; // o modo atual, para a interface mostrar

    [Header("Modo de movimento")]
    [SerializeField] MovementMode mode = MovementMode.Grip;

    void OnEnable()
    {
        if (map != null) map.MapChanged += OnMapChanged;
        network.OnServerStarted += OnServerStarted;
        network.OnClientStarted += OnClientStarted;
    }

    void OnDisable()
    {
        if (map != null) map.MapChanged -= OnMapChanged;
        network.OnServerStarted -= OnServerStarted;
        network.OnClientStarted -= OnClientStarted;
    }

    // Com um mapa novo, reaplica a regra para encaixar e prender os tokens na grade nova.
    void OnMapChanged()
    {
        if (rule != null) SetMode(mode);
    }

    IEnumerator Start()
    {
        network.PrefabHandler.AddHandler(tokenPrefab.gameObject, this);
        SetMode(mode);

        yield return null;

        for (int i = 0; i < testImages.Length; i++)
        {
            var data = new TokenData { displayName = testImages[i].name };
            data.Position = grid.CellToWorldCenter(new Vector2Int(2 + i * 2, 2));
            Spawn(data, testImages[i]);
        }
    }

    void OnDestroy()
    {
        if (network != null) network.PrefabHandler.RemoveHandler(tokenPrefab.gameObject);
    }

    // ownsTexture = true quando a imagem foi carregada só para este token
    // (assim o token libera a memória ao ser apagado). As imagens de teste são da Unity: false.
    public TokenView Spawn(TokenData data, Texture2D image, bool ownsTexture = false, byte[] imageBytes = null)
    {
        if (IsClientOnly) return null;

        if(imageBytes!= null) data.imageHash = images.Register(imageBytes);
        data.Position = rule.Snap(data.Position, data.size);
        TokenView view = Instantiate(tokenPrefab, transform);
        view.Init(data, image, grid.CellSize, ownsTexture);
        tokens.Add(view);
        if (network.IsServer) SpawnOnNetwork(view);
        return view;
    }

    // Cria o token no ponto da mesa que está no centro da tela, e já seleciona.
    public TokenView SpawnAtScreenCenter(TokenData data, Texture2D image, bool ownsTexture, byte[] imageBytes = null)
    {
        if (IsClientOnly) return null;
        
        Camera cam = Camera.main;
        Ray ray = cam.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
        if (TablePlane.Raycast(ray, out float distance))
            data.Position = ray.GetPoint(distance);

        TokenView view = Spawn(data, image, ownsTexture, imageBytes);
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
        if (token == null || token == dragged || IsClientOnly) return; // não apaga o token que está sendo arrastado

        if (token.Network.IsSpawned)
        {
            token.Network.NetworkObject.Despawn();
            return;
        }

        Forget(token);
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
            if (network.IsServer && token.Network.IsSpawned)
                token.Network.Publish(TokenState.From(token.Data));
        }
        ModeChanged?.Invoke();
    }

    public void ChangeHp(TokenView token, int delta)
    {
        if (token == null || delta == 0) return;

        if (IsClientOnly)
        {
            token.Network.RequestHpChange(delta);
            return;
        }

        ApplyHpChange(token, delta);
    }

    void ApplyHpChange(TokenView token, int delta)
    {
        TokenData data = token.Data;
        data.hp = Mathf.Clamp(data.hp + delta, 0, data.maxHp);
        token.Refresh();
        if (token.Network.IsSpawned) token.Network.Publish(TokenState.From(data));
        TokenChanged?.Invoke(token);
    }

    void OnHpChangeRequested(NetworkToken net, int delta)
    {
        ApplyHpChange(net.View, delta);
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
            if (pointer.HasPoint)
            {
                dragged.MoveVisual(pointer.WorldPoint + grabOffset);
                SendDragPreview();
            }
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

        if (IsClientOnly)
            dragged.Network.RequestMove(dragged.Data.Position);
        else if (dragged.Network.IsSpawned)
            dragged.Network.Publish(TokenState.From(dragged.Data));

        dragged.SetDragging(false);
        dragged = null;
    }

    void SendDragPreview()
    {
        if (!dragged.Network.IsSpawned || Time.time < nextDragSend) return;
        Vector3 position = dragged.transform.position;
        if ((position - lastSentDrag).sqrMagnitude < 0.0001f) return;

        nextDragSend = Time.time + dragSendInterval;
        lastSentDrag = position;
        dragged.Network.SendDragPreview(position);
    }

    void Forget(TokenView token)
    {
        tokens.Remove(token);
        token.Network.MoveRequested -= OnMoveRequested;
        token.Network.HpChangeRequested -= OnHpChangeRequested;
        token.Network.StateChanged -= OnTokenState;
        if (selected == token) Select(null);
        if (dragged == token) dragged = null;
    }

    void OnServerStarted()
    {
        foreach (TokenView view in tokens) SpawnOnNetwork(view);
    }

    void OnClientStarted()
    {
        if (network.IsServer) return;

        for (int i = tokens.Count - 1; i >= 0; i--)
        {
            TokenView view = tokens[i];
            Forget(view);
            Destroy(view.gameObject);
        }
    }

    void SpawnOnNetwork(TokenView view)
    {
        view.Network.MoveRequested += OnMoveRequested;
        view.Network.HpChangeRequested += OnHpChangeRequested;
        view.Network.Publish(TokenState.From(view.Data));
        if (!view.Network.IsSpawned) view.Network.NetworkObject.Spawn();
    }

    void OnMoveRequested(NetworkToken net, Vector3 target)
    {
        TokenView view = net.View;
        view.Data.Position = rule.Snap(target, view.Data.size);
        view.EndRemoteDrag();
        net.Publish(TokenState.From(view.Data));
        TokenChanged?.Invoke(view);
    }

    void OnTokenState(NetworkToken net)
    {
        TokenView view = net.View;

        if (view.Data == null)
        {
            var data = new TokenData();
            net.State.ApplyTo(data);
            view.Init(data, null, grid.CellSize);
            tokens.Add(view);
            RequestImage(view);
            return;
        }

        string previousHash = view.Data.imageHash;
        net.State.ApplyTo(view.Data);
        if (view.Data.imageHash != previousHash) RequestImage(view);
        view.EndRemoteDrag();
        TokenChanged?.Invoke(view);
    }

    void RequestImage(TokenView view)
    {
        string hash = view.Data.imageHash;
        if (string.IsNullOrEmpty(hash)) return;

        images.Get(hash, texture =>
        {
            if (view == null || texture == null || view.Data.imageHash != hash) return;
            view.SetImage(texture);
        });
    }

    NetworkObject INetworkPrefabInstanceHandler.Instantiate(ulong ownerClientId, Vector3 position, Quaternion rotation)
    {
        TokenView view = Instantiate(tokenPrefab, transform);
        view.Network.StateChanged += OnTokenState;
        return view.Network.NetworkObject;
    }

    void INetworkPrefabInstanceHandler.Destroy(NetworkObject networkObject)
    {
        TokenView view = networkObject.GetComponent<TokenView>();
        Forget(view);
        Destroy(networkObject.gameObject);
    }
}
