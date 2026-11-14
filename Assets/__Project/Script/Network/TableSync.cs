using UnityEngine;
using Unity.Netcode;

public class TableSync : NetworkBehaviour
{
    [SerializeField] TokenManager tokens;
    [SerializeField] TableGrid grid;
    [SerializeField] MapBoard map;

    readonly NetworkVariable<TableState> state = new NetworkVariable<TableState>();

    public override void OnNetworkSpawn()
    {
        if (IsServer)
        {
            tokens.ModeChanged += Publish;
            grid.VisibilityChanged += Publish;
            map.MapChanged += Publish;
            map.CalibrationChanged += Publish;
            Publish();
            return;
        }

        state.OnValueChanged += OnStateChanged;
        Apply(state.Value);
    }


    public override void OnNetworkDespawn()
    {
        if (IsServer)
        {
            tokens.ModeChanged -= Publish;
            grid.VisibilityChanged -= Publish;
            map.MapChanged -= Publish;
            map.CalibrationChanged -= Publish;
            return;
        }

        state.OnValueChanged -= OnStateChanged;
    }

    void Publish()
    {
        state.Value = new TableState
        {
            Mode = (int)tokens.Mode,
            GridVisible = grid.Visible,
            PixelsPerCell = map.PixelsPerCell,
            OffsetX = map.OffsetPixels.x,
            OffsetY = map.OffsetPixels.y
        };
    }

    void OnStateChanged(TableState previous, TableState current)
    {
        Apply(current);
    }

    void Apply(TableState table)
    {
        map.SetCalibration(table.PixelsPerCell, new Vector2(table.OffsetX, table.OffsetY));
        grid.SetVisible(table.GridVisible);
        tokens.SetMode((TokenManager.MovementMode)table.Mode);
    }
}
