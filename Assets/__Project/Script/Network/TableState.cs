using System;
using Unity.Netcode;

public class TableState : INetworkSerializable, IEquatable<TableState>
{
    public int Mode;
    public bool GridVisible;
    public float PixelsPerCell;
    public float OffsetX;
    public float OffsetY;

    public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
    {
        
        serializer.SerializeValue(ref Mode);
        serializer.SerializeValue(ref GridVisible);
        serializer.SerializeValue(ref PixelsPerCell);
        serializer.SerializeValue(ref OffsetX);
        serializer.SerializeValue(ref OffsetY);

    }

    public bool Equals(TableState other) =>
        Mode == other.Mode && GridVisible == other.GridVisible && 
        PixelsPerCell == other.PixelsPerCell && OffsetX == other.OffsetX && OffsetY == other.OffsetY;
}
