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

    public event Action<NetworkToken> StateChanged;
    public event Action<NetworkToken, Vector3, ulong> MoveRequested;
    public event Action<NetworkToken, int, ulong> HpChangeRequested;
    public event Action<NetworkToken, Vector3, ulong> DragPreviewRequested;

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

    public void RejectMove(ulong clientId)
    {
        MoveRejectedRpc(RpcTarget.Single(clientId, RpcTargetUse.Temp));
    }

    [Rpc(SendTo.SpecifiedInParams)]
    void MoveRejectedRpc(RpcParams rpcParams)
    {
        StateChanged?.Invoke(this);
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
        if (IsServer) ShowDragRpc(flat);
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