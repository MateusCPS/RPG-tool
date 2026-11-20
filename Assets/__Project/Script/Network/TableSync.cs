using UnityEngine;
using Unity.Netcode;
using Unity.Collections;

public class TableSync : NetworkBehaviour
{
    [SerializeField] TokenManager tokens;
    [SerializeField] TableGrid grid;
    [SerializeField] MapBoard map;
    [SerializeField] ImageService images;

    readonly NetworkVariable<TableState> state = new NetworkVariable<TableState>();
    string mapHash = "";
    string appliedMapHash = "";

    public override void OnNetworkSpawn()
    {
        if (IsServer)
        {
            tokens.ModeChanged += Publish;
            grid.VisibilityChanged += Publish;
            map.MapChanged += OnMapChanged;
            map.CalibrationChanged += Publish;
            OnMapChanged();
            return;
        }

        state.OnValueChanged += OnStateChanged;
        appliedMapHash = null;
        Apply(state.Value);
    }


    public override void OnNetworkDespawn()
    {
        if (IsServer)
        {
            tokens.ModeChanged -= Publish;
            grid.VisibilityChanged -= Publish;
            map.MapChanged -= OnMapChanged;
            map.CalibrationChanged -= Publish;
            return;
        }

        state.OnValueChanged -= OnStateChanged;
        appliedMapHash = null;
        map.ResetToDefault();
    }

    void OnMapChanged()
    {
        mapHash = map.ImageBytes != null ? images.Register(map.ImageBytes) : "";
        Publish();
    }

    void Publish()
    {
        var table = new TableState
        {
            Mode = (int)tokens.Mode,
            GridVisible = grid.Visible,
            PixelsPerCell = map.PixelsPerCell,
            OffsetX = map.OffsetPixels.x,
            OffsetY = map.OffsetPixels.y
        };
        table.MapHash.CopyFromTruncated(mapHash);
        state.Value = table;
    }

    void OnStateChanged(TableState previous, TableState current)
    {
        Apply(current);
    }

    void Apply(TableState table)
    {
        string hash = table.MapHash.ToString();
        if (hash != appliedMapHash)
        {
            appliedMapHash = hash;
            if (hash.Length > 0)
            {
                images.Get(hash, texture => OnMapImageReady(hash,texture));
                return;
            }
            map.ResetToDefault();
        }
        ApplySettings(table);
    }

    void OnMapImageReady(string hash, Texture2D texture)
    {
        if (hash != appliedMapHash) return;
        if (texture != null) map.SetImage(texture, "", false);
        ApplySettings(state.Value);
    }

    void ApplySettings(TableState table)
    {
        map.SetCalibration(table.PixelsPerCell, new Vector2(table.OffsetX, table.OffsetY));
        grid.SetVisible(table.GridVisible);
        tokens.SetMode((TokenManager.MovementMode)table.Mode);
    }
}
