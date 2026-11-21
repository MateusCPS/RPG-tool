using System;
using Unity.Collections;
using Unity.Netcode;

public struct PlayerEntry : INetworkSerializable, IEquatable<PlayerEntry>
{
    public ulong ClientId;
    public FixedString64Bytes Name;
    public FixedString64Bytes PlayerId;


    public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
    {
        serializer.SerializeValue(ref ClientId);
        serializer.SerializeValue(ref Name);
        serializer.SerializeValue(ref PlayerId);
    }

    public bool Equals(PlayerEntry other) => ClientId == other.ClientId && Name == other.Name && PlayerId == other.PlayerId;
}
