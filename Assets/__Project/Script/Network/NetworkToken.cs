using System;
using Unity.Netcode;
using UnityEngine;

public class NetworkToken : NetworkBehaviour
{
    [SerializeField] TokenView view;

    readonly NetworkVariable<TokenState> state = new NetworkVariable<TokenState>();
    TokenState pending;

    public TokenView View => view;
    public TokenState State => state.Value;

    public bool IsVisibleTo(ulong clientId)
    {
        TableRole role = clientId == NetworkManager.ServerClientId ? TableRole.Master : TableRole.Player;
        return PermissionRules.CanSeeToken(role, view.Data.hidden);
    }

    public void ApplyVisibility()
    {
        if (!IsServer || !IsSpawned) return;

        foreach(ulong clientId in NetworkManager.ConnectedClientsIds)
        {
            if (clientId == NetworkManager.ServerClientId) continue;

            bool shouldSee = IsVisibleTo(clientId);
            bool sees = NetworkObject.IsNetworkVisibleTo(clientId);
            if (shouldSee && !sees) NetworkObject.NetworkShow(clientId);
            else if (!shouldSee && sees) NetworkObject.NetworkHide(clientId);
        }
    }

    public event Action<NetworkToken> StateChanged;
    public event Action<NetworkToken, Vector3, ulong> MoveRequested;
    public event Action<NetworkToken, int, ulong> HpChangeRequested;
    public event Action<NetworkToken, Vector3, ulong> DragPreviewRequested;
    public event Action<NetworkToken, ulong> UndoMoveRequested;
    public event Action<NetworkToken, MoveRejection> MoveRejected;

    public override void OnNetworkSpawn()
    {
        if (IsServer) state.Value = pending;
        state.OnValueChanged += OnStateChanged;
        if (!IsServer) StateChanged?.Invoke(this);
    }

    public override void OnNetworkDespawn()
    {
        state.OnValueChanged -= OnStateChanged;
    }

    public void Publish(TokenState newState)
    {
        if (IsSpawned) state.Value = newState;
        else pending = newState;
    }

    void OnStateChanged(TokenState previous, TokenState current)
    {
        StateChanged?.Invoke(this);
    }

    public void RequestMove(Vector3 target)
    {
        RequestMoveRpc(new Vector2(target.x, target.z));
    }

    [Rpc(SendTo.Server)]
    void RequestMoveRpc(Vector2 target, RpcParams rpcParams = default)
    {
        MoveRequested?.Invoke(this, new Vector3(target.x, 0f, target.y), rpcParams.Receive.SenderClientId);
    }

    public void RejectMove(ulong clientId, MoveRejection reason)
    {
        MoveRejectedRpc(reason, RpcTarget.Single(clientId, RpcTargetUse.Temp));
    }

    [Rpc(SendTo.SpecifiedInParams)]
    void MoveRejectedRpc(MoveRejection reason, RpcParams rpcParams)
    {
        StateChanged?.Invoke(this);
        MoveRejected?.Invoke(this, reason);
    }

    public void RequestUndoMove() => RequestUndoMoveRpc();

    [Rpc(SendTo.Server)]
    void RequestUndoMoveRpc(RpcParams rpcParams = default)
    {
        UndoMoveRequested?.Invoke(this, rpcParams.Receive.SenderClientId);
    }

    public void RequestHpChange(int delta)
    {
        RequestHpChangeRpc(delta);
    }

    [Rpc(SendTo.Server)]
    void RequestHpChangeRpc(int delta, RpcParams rpcParams = default)
    {
        HpChangeRequested?.Invoke(this, delta, rpcParams.Receive.SenderClientId);
    }

    public void SendDragPreview(Vector3 point)
    {
        var flat = new Vector2(point.x, point.z);
        if (IsServer)
        {
            if (!view.Data.hidden) ShowDragRpc(flat);
        }
        else DragPreviewRpc(flat);
    }

    [Rpc(SendTo.Server, Delivery = RpcDelivery.Unreliable)]
    void DragPreviewRpc(Vector2 point, RpcParams rpcParams = default)
    {
        DragPreviewRequested?.Invoke(this, new Vector3(point.x, 0f, point.y), rpcParams.Receive.SenderClientId);
    }

    public void RelayDragPreview(Vector3 point)
    {
        view.ShowRemoteDrag(point);
        ShowDragRpc(new Vector2(point.x, point.z));
    }

    [Rpc(SendTo.NotServer, Delivery = RpcDelivery.Unreliable)]
    void ShowDragRpc(Vector2 point)
    {
        view.ShowRemoteDrag(new Vector3(point.x, 0f, point.y));
    }
}