using System;
using Unity.Netcode;

public struct MusicState : INetworkSerializable, IEquatable<MusicState>
{
    public int Situation;
    public int Mode;
    public int Track;
    public int PlayId;
    public bool Playing;
    public double StartTime;

    public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
    {
        serializer.SerializeValue(ref Situation);
        serializer.SerializeValue(ref Mode);
        serializer.SerializeValue(ref Track);
        serializer.SerializeValue(ref PlayId);
        serializer.SerializeValue(ref Playing);
        serializer.SerializeValue(ref StartTime);
    }

    public bool Equals(MusicState other) =>
        Situation == other.Situation && Mode == other.Mode && Track == other.Track && PlayId == other.PlayId && Playing == other.Playing && StartTime == other.StartTime;
}
