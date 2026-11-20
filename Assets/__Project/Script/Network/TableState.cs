using System;
using Unity.Collections;
using Unity.Netcode;

public struct TableState : INetworkSerializable, IEquatable<TableState>
{
    public int Mode;
    public bool GridVisible;
    public float PixelsPerCell;
    public float OffsetX;
    public float OffsetY;
    public FixedString64Bytes MapHash;

    public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
    {
        
        serializer.SerializeValue(ref Mode);
        serializer.SerializeValue(ref GridVisible);
        serializer.SerializeValue(ref PixelsPerCell);
        serializer.SerializeValue(ref OffsetX);
        serializer.SerializeValue(ref OffsetY);
        serializer.SerializeValue(ref MapHash);
    }

    public bool Equals(TableState other) =>
        Mode == other.Mode && GridVisible == other.GridVisible && 
        PixelsPerCell == other.PixelsPerCell && OffsetX == other.OffsetX && OffsetY == other.OffsetY &&
        MapHash == other.MapHash;
}
