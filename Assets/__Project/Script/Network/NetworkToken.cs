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
    public event Action<NetworkToken, Vector3> MoveRequested;
    public event Action<NetworkToken, int> HpChangeRequested;

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

    public void RequestMove(Vector3 target)
    {
        RequestMoveRpc(new Vector2(target.x, target.z));
    }

    void OnStateChanged(TokenState previous, TokenState current)
    {
        StateChanged?.Invoke(this);
    }

    [Rpc(SendTo.Server)]
    void RequestMoveRpc(Vector2 target)
    {
        MoveRequested?.Invoke(this, new Vector3(target.x, 0f, target.y));
    }

    public void RequestHpChange(int delta)
    {
        RequestHpChangeRpc(delta);
    }

    [Rpc(SendTo.Server)]
    void RequestHpChangeRpc(int delta)
    {
        HpChangeRequested?.Invoke(this, delta);
    }

    public void SendDragPreview(Vector3 point)
    {
        var flat = new Vector2(point.x, point.z);
        if (IsServer) ShowDragRpc(flat);
        else DragPreviewRpc(flat);
    }

    [Rpc(SendTo.Server, Delivery = RpcDelivery.Unreliable)]
    void DragPreviewRpc(Vector2 point)
    {
        view.ShowRemoteDrag(new Vector3(point.x, 0f, point.y));
        ShowDragRpc(point);
    }

    [Rpc(SendTo.NotServer, Delivery = RpcDelivery.Unreliable)]
    void ShowDragRpc(Vector2 point)
    {
        view.ShowRemoteDrag(new Vector3(point.x, 0f, point.y));
    }
}
