using System;
using Unity.Netcode;

public struct PlayerNetworkData : INetworkSerializable
{
    public float zDelta;
    public bool isRunning;
    public bool isCrouching;

    public void NetworkSerialize<T>(BufferSerializer<T> serializer) 
        where T : IReaderWriter
    {
        serializer.SerializeValue(ref zDelta);
        serializer.SerializeValue(ref isRunning);
        serializer.SerializeValue(ref isCrouching);
    }

    public override readonly bool Equals(object obj)
    {
        if (obj is PlayerNetworkData otherData)
        {
            return otherData.zDelta == zDelta
                && otherData.isRunning == isRunning
                && otherData.isCrouching == isCrouching;
        }

        return false;
    }

    public override readonly int GetHashCode()
    {
        return HashCode.Combine(zDelta, isRunning, isCrouching);
    }
}