using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;

public class SpikeToken : NetworkBehaviour
{
    [SerializeField] float tableHalfSize = 10f;
    [SerializeField] float grabRadius = 1f;
    [SerializeField] float sendInterval = 0.033f;
    [SerializeField] float smoothing = 15f;

    readonly NetworkVariable<Vector3> position = new NetworkVariable<Vector3>();
    static readonly Plane Table = new Plane(Vector3.up, Vector3.zero);

    bool dragging;
    float nextSend;
    Vector3 target;

    public override void OnNetworkSpawn()
    {
        position.OnValueChanged += OnPositionChanged;
        target = position.Value;
        transform.position = target;
    }

    public override void OnNetworkDespawn()
    {
        position.OnValueChanged -= OnPositionChanged;
    }

    void OnPositionChanged(Vector3 previous, Vector3 current)
    {
        target = current;
    }

    void Update()
    {
        if (!IsSpawned) return;

        if (!dragging)
            transform.position = Vector3.Lerp(transform.position, target, 1f - Mathf.Exp(-smoothing * Time.deltaTime));

        Mouse mouse = Mouse.current;
        Camera cam = Camera.main;
        if (mouse == null || cam == null) return;

        Ray ray = cam.ScreenPointToRay(mouse.position.ReadValue());
        if (!Table.Raycast(ray, out float distance)) return;
        Vector3 point = ray.GetPoint(distance);

        if (mouse.leftButton.wasPressedThisFrame && Vector3.Distance(point, transform.position) <= grabRadius)
            dragging = true;

        if (!dragging) return;

        if (mouse.leftButton.isPressed)
        {
            transform.position = point;
            if (Time.time >= nextSend)
            {
                nextSend = Time.time + sendInterval;
                RequestMoveRpc(point);
            }
        }
        else
        {
            dragging = false;
            target = point;
            RequestMoveRpc(point);
        }
    }

    [Rpc(SendTo.Server)]
    void RequestMoveRpc(Vector3 target)
    {
        target.x = Mathf.Clamp(target.x, -tableHalfSize, tableHalfSize);
        target.z = Mathf.Clamp(target.z, -tableHalfSize, tableHalfSize);
        target.y = 0f;
        position.Value = target;
    }
}
