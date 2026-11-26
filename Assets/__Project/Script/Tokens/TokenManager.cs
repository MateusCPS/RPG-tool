using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.Netcode;
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
    [SerializeField] PlayerRoster roster;
    [SerializeField] SessionService session;

    readonly List<TokenView> tokens = new List<TokenView>();
    IMovementRule rule;
    TokenView dragged;
    TokenView selected;
    Vector3 grabOffset;
    Vector3 dragStart;
    Vector3 lastSentDrag;
    float nextDragSend;
    bool combatActive;
    int round;

    struct MoveRecord
    {
        public TokenView Token;
        public Vector3 From;
        public float Distance;
        public string MoverId;
    }

    readonly List<MoveRecord> moveHistory = new List<MoveRecord>();
    TokenView lastMoved;
    public bool CombatActive => combatActive;
    public int Round => round;
    public bool IsDragging => dragged != null;
    public TokenView Dragged => dragged;
    public TokenView Selected => selected;
    public Vector3 DragStart => dragStart;
    public IMovementRule Rule => rule;

    bool IsClientOnly => network.IsListening && !network.IsServer;

    // Avisa quando a seleção muda (o botão Apagar usa).
    public event Action SelectionChanged;
    public event Action ModeChanged;
    public event Action PermissionsChanged;
    public event Action CombatChanged;
    public event Action<TokenView> TokenChanged;
    public event Action<TokenView, MoveRejection> MoveRejected;
    public bool CanManageTokens => !IsClientOnly;

    static readonly Plane TablePlane = new Plane(Vector3.up, Vector3.zero);

    public enum MovementMode { Grip = 0, Free = 1 }
    public MovementMode Mode => mode; 

    [Header("Modo de movimento")]
    [SerializeField] MovementMode mode = MovementMode.Grip;

    void OnEnable()
    {
        if (map != null) map.MapChanged += OnMapChanged;
        network.OnServerStarted += OnServerStarted;
        network.OnClientStarted += OnClientStarted;
        network.OnClientStopped += OnClientStopped;
    }

    void OnDisable()
    {
        if (map != null) map.MapChanged -= OnMapChanged;
        network.OnServerStarted -= OnServerStarted;
        network.OnClientStarted -= OnClientStarted;
        network.OnClientStopped -= OnClientStopped;

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

    public void SetCombat(bool active)
    {
        if (IsClientOnly) return;
        combatActive = active;
        round = active ? 1 : 0;
        ResetMovement();
        CombatChanged?.Invoke();
    }

    public void NewRound()
    {
        if (IsClientOnly || !combatActive) return;
        round++;
        ResetMovement();
        CombatChanged?.Invoke();
    }

    public void ApplyCombat(bool active, int newRound)
    {
        if (combatActive == active && round == newRound) return;
        combatActive = active;
        round = newRound;
        CombatChanged?.Invoke();
    }

    void ResetMovement()
    {
        moveHistory.Clear();
        foreach (TokenView token in tokens)
        {
            token.Data.movedThisRound = 0f;
            if (network.IsServer && token.Network.IsSpawned)
                token.Network.Publish(TokenState.From(token.Data));
        }
    }

    void AddMovement(TokenView token, Vector3 from, float distance, string moverId)
    {
        if (!combatActive || distance <= 0f) return;
        token.Data.movedThisRound += distance;
        moveHistory.Add(new MoveRecord { Token = token, From = from, Distance = distance, MoverId = moverId });
    }

    bool CanTravel(ulong clientId, TokenData data, float distance) =>
        CombatRules.CanMove(combatActive, RoleOf(clientId), data.movedThisRound, distance, data.speed);

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

    public void SetOwner(TokenView token, string ownerId)
    {
        if (token == null || !CanManageTokens) return;

        token.Data.ownerId = ownerId ?? "";
        if (token.Network.IsSpawned) token.Network.Publish(TokenState.From(token.Data));
        TokenChanged?.Invoke(token);
    }

    public void SetHidden(TokenView token, bool hidden)
    {
        if (token == null || !CanManageTokens || token.Data.hidden == hidden) return;

        token.Data.hidden = hidden;
        token.Refresh();
        if (token.Network.IsSpawned) token.Network.ApplyVisibility();
        TokenChanged?.Invoke(token);
    }

    void ApplyHpChange(TokenView token, int delta)
    {
        TokenData data = token.Data;
        data.hp = Mathf.Clamp(data.hp + delta, 0, data.maxHp);
        token.Refresh();
        if (token.Network.IsSpawned) token.Network.Publish(TokenState.From(data));
        TokenChanged?.Invoke(token);
    }

    void OnHpChangeRequested(NetworkToken net, int delta, ulong sender)
    {
        if(!CanChangeHp(sender, net.View.Data)) return;
        ApplyHpChange(net.View, delta);
    }

    void OnDragPreviewRequested(NetworkToken net, Vector3 point, ulong sender)
    {
        if (!CanMove(sender, net.View.Data)) return;
        net.RelayDragPreview(point);
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
        HandleUndoKey();

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
        if (found != null && CanDragLocally(found.Data)) BeginDrag(found, point);
    }

    // A tecla Delete apaga o token selecionado.
    void HandleDeleteKey()
    {
        Keyboard keyboard = Keyboard.current;
        if (keyboard == null || !keyboard.deleteKey.wasPressedThisFrame || IsTyping()) return;
        DeleteSelected();
    }

    void HandleUndoKey()
    {
        Keyboard keyboard = Keyboard.current;
        if (keyboard == null || !keyboard.zKey.wasPressedThisFrame || !keyboard.ctrlKey.isPressed || IsTyping()) return;
        UndoLastMove();
    }

    static bool IsTyping()
    {
        GameObject focused = EventSystem.current != null ? EventSystem.current.currentSelectedGameObject : null;
        return focused != null && focused.GetComponent<TMP_InputField>() != null;
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
        Vector3 snapped = rule.Snap(dropped, dragged.Data.size);

        if (IsClientOnly)
        {
            dragged.Data.Position = snapped;
            dragged.Network.RequestMove(snapped);
        }
        else
        {
            AddMovement(dragged, dragged.Data.Position, rule.MeasureDistance(dragged.Data.Position, snapped), session.PlayerId);
            dragged.Data.Position = snapped;
            if (dragged.Network.IsSpawned) dragged.Network.Publish(TokenState.From(dragged.Data));
        }

        lastMoved = dragged;
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
        token.Network.DragPreviewRequested -= OnDragPreviewRequested;
        token.Network.StateChanged -= OnTokenState;
        token.Network.UndoMoveRequested -= OnUndoMoveRequested;
        token.Network.MoveRejected -= OnMoveRejected;
        if (selected == token) Select(null);
        if (dragged == token) dragged = null;
        if (lastMoved == token) lastMoved = null;
        for (int i = moveHistory.Count - 1; i >= 0; i--)
            if (moveHistory[i].Token == token) moveHistory.RemoveAt(i);
    }

    void OnServerStarted()
    {
        foreach (TokenView view in tokens) SpawnOnNetwork(view);
    }

    void OnClientStarted()
    {
        PermissionsChanged?.Invoke();
        if (network.IsServer) return;

        for (int i = tokens.Count - 1; i >= 0; i--)
        {
            TokenView view = tokens[i];
            Forget(view);
            Destroy(view.gameObject);
        }
    }

    void OnClientStopped(bool wasHost) => PermissionsChanged?.Invoke();

    void SpawnOnNetwork(TokenView view)
    {
        view.Network.MoveRequested += OnMoveRequested;
        view.Network.HpChangeRequested += OnHpChangeRequested;
        view.Network.DragPreviewRequested += OnDragPreviewRequested;
        view.Network.UndoMoveRequested += OnUndoMoveRequested;
        view.Network.Publish(TokenState.From(view.Data));
        view.Network.NetworkObject.CheckObjectVisibility = view.Network.IsVisbleTo;
        if (!view.Network.IsSpawned) view.Network.NetworkObject.Spawn();
    }

    static TableRole RoleOf(ulong clientId) =>
        clientId == NetworkManager.ServerClientId ? TableRole.Master : TableRole.Player;
    
    string PlayerIdOf(ulong clientId) =>
        roster.TryGetPlayerId(clientId, out string playerId) ? playerId : "";
    
    bool CanMove(ulong clientId, TokenData data) =>
        PermissionRules.CanSeeToken(RoleOf(clientId), data.hidden) &&
        PermissionRules.CanMoveToken(RoleOf(clientId), PlayerIdOf(clientId), data.ownerId);

    bool CanChangeHp(ulong clientId, TokenData data) =>
        PermissionRules.CanSeeToken(RoleOf(clientId), data.hidden) &&
        PermissionRules.CanChangeHp(RoleOf(clientId), PlayerIdOf(clientId), data.ownerId);

    bool CanDragLocally(TokenData data) =>
        !IsClientOnly || PermissionRules.CanMoveToken(TableRole.Player, session.PlayerId, data.ownerId);

    public bool CanEditHpLocally(TokenData data) =>
        !IsClientOnly || PermissionRules.CanChangeHp(TableRole.Player, session.PlayerId, data.ownerId);
    void OnMoveRequested(NetworkToken net, Vector3 target, ulong sender)
    {
        TokenView view = net.View;
        Vector3 snapped = rule.Snap(target, view.Data.size);
        float distance = rule.MeasureDistance(view.Data.Position, snapped);

        if (!CanMove(sender, view.Data))
        {
            net.RejectMove(sender, MoveRejection.NotAllowed);
            return;
        }

        if (!CanTravel(sender, view.Data, distance))
        {
            net.RejectMove(sender, MoveRejection.NoMovementLeft);
            return;
        }

        AddMovement(view, view.Data.Position, distance, PlayerIdOf(sender));
        view.Data.Position = snapped;
        view.EndRemoteDrag();
        net.Publish(TokenState.From(view.Data));
        TokenChanged?.Invoke(view);
    }

    void OnMoveRejected(NetworkToken net, MoveRejection reason) => MoveRejected?.Invoke(net.View, reason);

    public void UndoLastMove()
    {
        if (lastMoved == null || dragged != null || !combatActive) return;

        if (IsClientOnly)
        {
            lastMoved.Network.RequestUndoMove();
            return;
        }

        TryUndoMove(lastMoved, NetworkManager.ServerClientId);
    }

    void OnUndoMoveRequested(NetworkToken net, ulong sender) => TryUndoMove(net.View, sender);

    void TryUndoMove(TokenView token, ulong clientId)
    {
        int index = LastMoveOf(token);
        if (index < 0 || !CanMove(clientId, token.Data)) return;

        MoveRecord record = moveHistory[index];
        if (!CombatRules.CanUndoMove(RoleOf(clientId), record.MoverId == PlayerIdOf(clientId))) return;

        moveHistory.RemoveAt(index);
        token.Data.Position = record.From;
        token.Data.movedThisRound = Mathf.Max(0f, token.Data.movedThisRound - record.Distance);
        token.Refresh();
        if (token.Network.IsSpawned) token.Network.Publish(TokenState.From(token.Data));
        TokenChanged?.Invoke(token);
    }

    int LastMoveOf(TokenView token)
    {
        for (int i = moveHistory.Count - 1; i >= 0; i--)
            if (moveHistory[i].Token == token) return i;
        return -1;
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
        view.Network.MoveRejected += OnMoveRejected;
        return view.Network.NetworkObject;
    }

    void INetworkPrefabInstanceHandler.Destroy(NetworkObject networkObject)
    {
        TokenView view = networkObject.GetComponent<TokenView>();

        if (network.IsServer && network.ShutdownInProgress)
        {
            KeepAfterShutdown(view);
            return;
        }

        Forget(view);
        Destroy(networkObject.gameObject);
    }

    void KeepAfterShutdown(TokenView view)
    {
        view.Network.MoveRequested -= OnMoveRequested;
        view.Network.HpChangeRequested -= OnHpChangeRequested;
        view.Network.DragPreviewRequested -= OnDragPreviewRequested;
        view.Network.UndoMoveRequested -= OnUndoMoveRequested;
        view.EndRemoteDrag();
    }
}
