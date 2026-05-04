using Unity.Netcode;
using System;

public enum GamePhase
{
    WaitingForPlayers,
    Playing,
    ColorSelection,
    TargetSelection, // Rule of 7
    ReactionEvent,   // Rule of 8
    GameOver
}

[Serializable]
public struct GameState : INetworkSerializable
{
    // Cần tối đa 4 người chơi theo đặc tả
    public const int MAX_PLAYERS = 4;

    // Dùng mảng ulong (ClientID) thay vì List<string> để NGO có thể serialize
    public ulong[] playerOrder; 
    public int playerCount; // Số lượng người chơi thực tế
    
    public int currentPlayerIndex;
    public bool isClockwise;
    
    // Lưu ý: CardInstance cũng phải implement INetworkSerializable
    public CardInstance topCard; 
    public CardColor activeColor; 
    public int pendingPenalty; 
    public GamePhase phase;
    
    // Vì không dùng Dictionary được, ta dùng mảng số lượng bài tương ứng với thứ tự trong playerOrder
    public int[] handCounts; 

    // Bắt buộc phải có để truyền qua mạng (Serialization)
    public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
    {
        serializer.SerializeValue(ref playerCount);
        
        // Khởi tạo mảng khi đang đọc (deserialize) trên Client
        if (serializer.IsReader && playerOrder == null)
        {
            playerOrder = new ulong[MAX_PLAYERS];
            handCounts = new int[MAX_PLAYERS];
        }

        for (int i = 0; i < MAX_PLAYERS; i++)
        {
            if (i < playerCount)
            {
                serializer.SerializeValue(ref playerOrder[i]);
                serializer.SerializeValue(ref handCounts[i]);
            }
        }

        serializer.SerializeValue(ref currentPlayerIndex);
        serializer.SerializeValue(ref isClockwise);
        
        // Serialize CardInstance
        topCard.NetworkSerialize(serializer);
        
        serializer.SerializeValue(ref activeColor);
        serializer.SerializeValue(ref pendingPenalty);
        serializer.SerializeValue(ref phase);
    }
}
