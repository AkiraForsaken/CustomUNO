using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

// GameManager là nơi chứa LUẬT và TRẠNG THÁI máy chủ. Không xử lý RPC trực tiếp.
public class GameManager : NetworkBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("Dependencies")]
    [SerializeField] private DeckManager deckManager;
    [SerializeField] private NetworkGameManager networkGameManager;

    // LƯU Ý BẢO MẬT: Biến này chỉ lưu trên Host, tuyệt đối không gửi toàn bộ qua mạng
    private Dictionary<ulong, List<CardInstance>> playerHands = new Dictionary<ulong, List<CardInstance>>();
    
    public GameState currentState;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    // // Bắt đầu game (gọi khi Lobby đã đầy và Host ấn Start)
    // public void StartGame(List<ulong> connectedClients)
    // {
    //     if (!IsServer) return; // Chỉ Host mới có quyền Start

    //     // Khởi tạo GameState
    //     currentState = new GameState
    //     {
    //         playerOrder = new ulong[GameState.MAX_PLAYERS],
    //         handCounts = new int[GameState.MAX_PLAYERS],
    //         playerCount = connectedClients.Count,
    //         currentPlayerIndex = 0,
    //         isClockwise = true,
    //         phase = GamePhase.Playing,
    //         pendingPenalty = 0
    //     };

    //     for (int i = 0; i < connectedClients.Count; i++)
    //     {
    //         currentState.playerOrder[i] = connectedClients[i];
    //         playerHands[connectedClients[i]] = new List<CardInstance>();
    //     }

    //     // 1. Trộn bài
    //     deckManager.ShuffleDeck(deckManager.drawPile);

    //     // 2. Chia bài (mỗi người 7 lá)
    //     foreach (var clientId in connectedClients)
    //     {
    //         for (int i = 0; i < 7; i++)
    //         {
    //             playerHands[clientId].Add(deckManager.DrawCard());
    //         }
    //     }

    //     // 3. Lật lá đầu tiên (Giả định lá đầu không phải Action Card cho đơn giản)
    //     CardInstance firstCard = deckManager.DrawCard();
    //     currentState.topCard = firstCard;
    //     currentState.activeColor = firstCard.color;
    //     deckManager.DiscardCard(firstCard);

    //     UpdateHandCounts();
    //     BroadcastState();
    // }

    // Đảm bảo có using Unity.Netcode; ở đầu file
    public void StartMatch()
    {
        // 1. Chỉ Host mới có quyền chia bài
        if (!NetworkManager.Singleton.IsServer) return;

        // 2. Lấy danh sách ID của những người đã kết nối
        var clients = NetworkManager.Singleton.ConnectedClientsIds;
        currentState.playerCount = clients.Count;
        currentState.playerOrder = new ulong[GameState.MAX_PLAYERS];
        currentState.handCounts = new int[GameState.MAX_PLAYERS];
        
        // Khởi tạo danh sách bài ẩn cho từng người (Dictionary trên Host)
        playerHands.Clear();

        for (int i = 0; i < clients.Count; i++)
        {
            ulong clientId = clients[i];
            currentState.playerOrder[i] = clientId;
            playerHands[clientId] = new List<CardInstance>();
        }

        // 3. Xào bài và chia mỗi người 7 lá
        deckManager.BuildStandardDeck();
        
        for (int i = 0; i < clients.Count; i++)
        {
            ulong clientId = clients[i];
            for (int c = 0; c < 7; c++)
            {
                CardInstance drawnCard = deckManager.DrawCard();
                playerHands[clientId].Add(drawnCard);
            }
            // Cập nhật số lượng bài lên state chung
            currentState.handCounts[i] = 7;
        }

        // 4. Lật lá bài đầu tiên ra giữa bàn (bỏ qua các lá Wild/Action cho dễ ở lượt đầu)
        CardInstance firstCard;
        do {
            firstCard = deckManager.DrawCard();
        } while (firstCard.type != CardType.Number);
        
        currentState.topCard = firstCard;
        currentState.activeColor = firstCard.color;

        // 5. Cài đặt các thông số lượt đi
        currentState.currentPlayerIndex = 0; // Host đi trước
        currentState.isClockwise = true;
        currentState.pendingPenalty = 0;
        currentState.phase = GamePhase.Playing; // CHUYỂN SANG TRẠNG THÁI CHƠI

        // 6. Gửi bài ẩn cho từng người và đồng bộ GameState
        SyncAllPlayerHands();
        networkGameManager.SyncGameStateClientRpc(currentState);
    }

    // Hàm hỗ trợ gửi bài riêng cho từng Client
    // Hàm hỗ trợ gửi bài riêng cho từng Client
    private void SyncAllPlayerHands()
    {
        foreach (var kvp in playerHands)
        {
            ulong clientId = kvp.Key;
            
            // TỐI ƯU: Nếu là Host, tự động nạp bài thẳng vào UI, không cần chờ mạng
            if (clientId == NetworkManager.Singleton.LocalClientId)
            {
                GameEvents.RaiseLocalHandUpdated(kvp.Value);
            }
            else
            {
                // Nếu là Client khác, gửi mảng dữ liệu qua mạng
                CardInstance[] handArray = kvp.Value.ToArray(); 
                networkGameManager.SyncPrivateHandClientRpc(handArray, new ClientRpcParams
                {
                    Send = new ClientRpcSendParams { TargetClientIds = new[] { clientId } }
                });
            }
        }
    }
    // Xử lý Client gửi yêu cầu đánh bài (Được gọi bởi NetworkGameManager)
    public void TryPlayCard(ulong clientId, CardInstance card)
    {
        if (!IsServer || currentState.phase != GamePhase.Playing) return;

        // 1. Kiểm tra Turn: Nếu không đúng lượt, từ chối
        if (currentState.playerOrder[currentState.currentPlayerIndex] != clientId) return;

        List<CardInstance> hand = playerHands[clientId];
        
        // 2. Kiểm tra xem lá bài này có nằm trên tay người chơi không (chống hack/lỗi UI)
        int cardIndex = hand.FindIndex(c => c.Equals(card));
        if (cardIndex == -1) return;

        // 3. Kiểm tra luật (UNO cơ bản, Stacking, No Action Card Win)
        if (!CardValidator.IsLegal(card, currentState, hand.Count))
        {
            Debug.Log($"Client {clientId} thực hiện nước đi không hợp lệ!");
            return; 
        }

        // --- Nếu Hợp Lệ ---
        hand.RemoveAt(cardIndex);
        currentState.topCard = card;
        currentState.activeColor = card.color; 
        deckManager.DiscardCard(card);

        // 4. Kiểm tra chiến thắng
        if (WinChecker.HasWon(hand.Count))
        {
            currentState.phase = GamePhase.GameOver;
            networkGameManager.NotifyGameOverClientRpc(clientId.ToString());
            BroadcastState();
            return;
        }

        // 5. Cộng dồn bài phạt nếu có thẻ +2, +4 (Luật Stacking)
        if (card.type == CardType.DrawTwo) currentState.pendingPenalty += 2;
        if (card.type == CardType.WildDrawFour) currentState.pendingPenalty += 4;

        // 6. Xử lý hiệu ứng (Skip, Reverse)
        ApplyCardEffects(card);

        // NẾU ĐÁNH BÀI WILD, TẠM DỪNG VÀ CHỜ CHỌN MÀU
        if (card.type == CardType.Wild || card.type == CardType.WildDrawFour)
        {
            currentState.phase = GamePhase.ColorSelection;
            // Dừng ở đây, KHÔNG gọi TurnManager.NextPlayer
        }
        else 
        {
            // Bài bình thường, chuyển lượt luôn
            TurnManager.NextPlayer(ref currentState);
        }
        UpdateHandCounts();
        BroadcastState();
    }

    // Xử lý khi người chơi đã chọn màu xong
    public void ReceiveColorChoice(ulong clientId, CardColor chosenColor)
    {
        if (!IsServer || currentState.phase != GamePhase.ColorSelection) return;

        // Chỉ người vừa đánh bài Wild mới được phép chọn màu
        if (currentState.playerOrder[currentState.currentPlayerIndex] != clientId) return;

        // 1. Áp dụng màu mới
        currentState.activeColor = chosenColor;
        
        // 2. Trả game về trạng thái bình thường
        currentState.phase = GamePhase.Playing;

        // 3. Bây giờ mới chuyển lượt đi cho người tiếp theo
        TurnManager.NextPlayer(ref currentState);
        
        UpdateHandCounts();
        BroadcastState();
    }

    // Xử lý Client yêu cầu rút bài
    public void TryDrawCard(ulong clientId)
    {
        if (!IsServer || currentState.phase != GamePhase.Playing) return;
        if (currentState.playerOrder[currentState.currentPlayerIndex] != clientId) return;

        // Nếu đang gánh Penalty do Stacking, phải rút TẤT CẢ
        if (currentState.pendingPenalty > 0)
        {
            for (int i = 0; i < currentState.pendingPenalty; i++)
            {
                playerHands[clientId].Add(deckManager.DrawCard());
            }
            currentState.pendingPenalty = 0;
            TurnManager.NextPlayer(ref currentState); // Bị phạt xong mất luôn lượt
        }
        else
        {
            // Rút 1 lá bình thường (Theo chuẩn, nếu rút xong đánh được thì cho phép đánh ngay, 
            // hiện tại ta thiết kế rút xong là mất lượt để xử lý luồng mạng ổn định trước)
            CardInstance drawn = deckManager.DrawCard();
            playerHands[clientId].Add(drawn);
            TurnManager.NextPlayer(ref currentState);
        }

        UpdateHandCounts();
        BroadcastState();
    }

    private void ApplyCardEffects(CardInstance card)
    {
        if (card.type == CardType.Reverse)
        {
            if (currentState.playerCount == 2)
                TurnManager.SkipNextPlayer(ref currentState); // Luật 2 người
            else
                TurnManager.ReverseDirection(ref currentState);
        }
        else if (card.type == CardType.Skip)
        {
            TurnManager.SkipNextPlayer(ref currentState);
        }
        // TODO: Mở rộng Luật 0, 7, 8 ở đây trong tương lai
    }

    private void UpdateHandCounts()
    {
        for (int i = 0; i < currentState.playerCount; i++)
        {
            ulong id = currentState.playerOrder[i];
            currentState.handCounts[i] = playerHands[id].Count;
        }
    }

    public void BroadcastState()
    {
        // Gửi trạng thái chung (ai tới lượt, lá trên cùng, hướng xoay)
        networkGameManager.SyncGameStateClientRpc(currentState);

        // Gửi bài ẩn (Client nào chỉ nhận được mảng bài của Client đó)
        foreach (var kvp in playerHands)
        {
            networkGameManager.SyncPrivateHand(kvp.Key, kvp.Value);
        }
    }
}