using Unity.Netcode;
using System;

[Serializable]
public struct CardInstance : INetworkSerializable, IEquatable<CardInstance>
{
    public CardColor color;
    public CardType type;
    public int number; 

    public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
    {
        serializer.SerializeValue(ref color);
        serializer.SerializeValue(ref type);
        serializer.SerializeValue(ref number);
    }

    // Helper method để so sánh 2 thẻ
    public bool Equals(CardInstance other)
    {
        return color == other.color && type == other.type && number == other.number;
    }
}