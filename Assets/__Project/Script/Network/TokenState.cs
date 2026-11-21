using System;
using Unity.Collections;
using Unity.Netcode;

public struct TokenState : INetworkSerializable, IEquatable<TokenState>
{
    public FixedString64Bytes Id;
    public FixedString64Bytes Name;
    public FixedString64Bytes ImageHash;
    public FixedString64Bytes OwnerId;

    public float X;
    public float Z;
    public int Size;
    public float Speed;
    public float Moved;
    public int Hp;
    public int MaxHp;

    public static TokenState From(TokenData data)
    {
        var state = new TokenState
        {
            X = data.x,
            Z = data.z,
            Size = data.size,
            Speed = data.speed,
            Moved = data.movedThisRound,
            Hp = data.hp,
            MaxHp = data.maxHp
        };
        state.Id.CopyFromTruncated(data.id);
        state.Name.CopyFromTruncated(data.displayName);
        state.ImageHash.CopyFromTruncated(data.imageHash);
        state.OwnerId.CopyFromTruncated(data.ownerId);
        return state;
    }

    public void ApplyTo(TokenData data)
    {
        data.id = Id.ToString();
        data.displayName = Name.ToString();
        data.x = X;
        data.z = Z;
        data.size = Size;
        data.speed = Speed;
        data.hp = Hp;
        data.maxHp = MaxHp;
        data.imageHash = ImageHash.ToString();
        data.ownerId = OwnerId.ToString();
    }

    public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
    {
        serializer.SerializeValue(ref Id);
        serializer.SerializeValue(ref Name);
        serializer.SerializeValue(ref X);
        serializer.SerializeValue(ref Z);
        serializer.SerializeValue(ref Size);
        serializer.SerializeValue(ref Speed);
        serializer.SerializeValue(ref Moved);
        serializer.SerializeValue(ref Hp);
        serializer.SerializeValue(ref MaxHp);
        serializer.SerializeValue(ref ImageHash);
        serializer.SerializeValue(ref OwnerId);
    }

    public bool Equals(TokenState other) =>
        Id == other.Id && Name == other.Name && X == other.X && Z == other.Z &&
        Size == other.Size && Speed == other.Speed && Hp == other.Hp && MaxHp == other.MaxHp && ImageHash == other.ImageHash && OwnerId == other.OwnerId && Moved == other.Moved;
}
