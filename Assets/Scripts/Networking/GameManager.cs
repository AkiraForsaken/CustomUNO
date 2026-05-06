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
    // Dùng cho Luật 8
    private List<ulong> reactedPlayers = new List<ulong>();
    private Coroutine reactionCoroutine;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    public void StartMatch()
{
    // 1. Chỉ Host/Server mới có quyền điều phối trận đấu
    if (!NetworkManager.Singleton.IsServer) return;

    // 2. Lấy danh sách ID của những người đã kết nối thực tế qua mạng
    var clients = NetworkManager.Singleton.ConnectedClientsIds;
    int connectedCount = clients.Count;

    // Khởi tạo GameState cơ bản [cite: 11]
    currentState.playerCount = connectedCount;
    currentState.playerOrder = new ulong[GameState.MAX_PLAYERS];
    currentState.handCounts = new int[GameState.MAX_PLAYERS];
    
    // Xóa dữ liệu cũ trên Host
    playerHands.Clear();

    // Chuẩn bị danh sách để đồng bộ tên qua mạng
    List<ulong> idsList = new List<ulong>();
    List<string> namesList = new List<string>();

    // Lấy thông tin người chơi từ LobbyManager (nếu có) 
    var lobbyPlayers = LobbyManager.Instance?.GetCurrentPlayers();

    for (int i = 0; i < connectedCount; i++)
    {
        ulong clientId = clients[i];
        currentState.playerOrder[i] = clientId;
        playerHands[clientId] = new List<CardInstance>();

        // Mặc định tên là Player + số thứ tự
        string pName = $"Player {i + 1}"; 

        // Nếu tìm thấy tên thật từ Lobby, hãy sử dụng nó
        if (lobbyPlayers != null && i < lobbyPlayers.Count)
        {
            if (lobbyPlayers[i].Data != null && lobbyPlayers[i].Data.ContainsKey("PlayerName"))
            {
                pName = lobbyPlayers[i].Data["PlayerName"].Value;
            }
        }

        idsList.Add(clientId);
        namesList.Add(pName);
    }

    // ĐỒNG BỘ TÊN: Gộp danh sách tên thành 1 chuỗi duy nhất cách nhau bằng dấu |
    string joinedNames = string.Join("|", namesList);
    networkGameManager.SyncAllPlayerNamesClientRpc(idsList.ToArray(), joinedNames);

    // 3. Xào bài và chia mỗi người 7 lá theo luật UNO [cite: 21, 42]
    deckManager.BuildStandardDeck();
    
    for (int i = 0; i < connectedCount; i++)
    {
        ulong clientId = currentState.playerOrder[i];
        for (int c = 0; c < 7; c++)
        {
            CardInstance drawnCard = deckManager.DrawCard();
            playerHands[clientId].Add(drawnCard);
        }
        currentState.handCounts[i] = 7;
    }

    // 4. Lật lá bài đầu tiên (Phải là lá số theo chuẩn để tránh lỗi lượt đầu) 
    CardInstance firstCard;
    do {
        firstCard = deckManager.DrawCard();
        if (firstCard.type != CardType.Number)
        {
            // Nếu bốc trúng lá chức năng, bỏ vào lại giữa xấp bài và bốc lá khác
            deckManager.drawPile.Add(firstCard); 
        }
    } while (firstCard.type != CardType.Number);
    
    currentState.topCard = firstCard;
    currentState.activeColor = firstCard.color;
    deckManager.DiscardCard(firstCard);

    // 5. Thiết lập các thông số lượt đi ban đầu [cite: 21]
    currentState.currentPlayerIndex = 0; // Host luôn đi đầu
    currentState.isClockwise = true;
    currentState.pendingPenalty = 0;
    currentState.phase = GamePhase.Playing;

    // 6. PHÁT LOA TRẠNG THÁI: Đồng bộ GameState và bài ẩn cho từng người [cite: 13, 39]
    BroadcastState();
    
    Debug.Log($"[GameManager] Match started with {connectedCount} players. First card: {firstCard.color} {firstCard.number}");
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
        if (currentState.unoVulnerableId != 0 && currentState.unoVulnerableId != clientId)
            currentState.unoVulnerableId = 0;

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

        if (hand.Count == 1) currentState.unoVulnerableId = clientId;

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
        // Ghi nhận lại xem lá bài này có gây nhảy cóc lượt (Skip) hay không
        bool hasSkippedTurn = ApplyCardEffects(card);

        // NẾU ĐÁNH BÀI WILD, TẠM DỪNG VÀ CHỜ CHỌN MÀU
        if (card.type == CardType.Wild || card.type == CardType.WildDrawFour)
        {
            currentState.phase = GamePhase.ColorSelection;
            // Dừng ở đây, KHÔNG gọi TurnManager.NextPlayer
        }
        else if (card.type == CardType.Number && card.number == 7)
        {
            currentState.phase = GamePhase.TargetSelection; 
        }
        else if (card.type == CardType.Number && card.number == 0)
        {
            currentState.phase = GamePhase.DirectionSelection; 
        }
        else if (card.type == CardType.Number && card.number == 8)
        {
            StartReactionEvent(); 
        }
        else 
        {
            // BÀI BÌNH THƯỜNG HOẶC ACTION CARD:
            // Chỉ chuyển qua người tiếp theo nếu lúc nãy chưa bị nhảy cóc lượt!
            if (!hasSkippedTurn)
            {
                TurnManager.NextPlayer(ref currentState);
            }
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

    // Xử lý khi người chơi đã chọn xong người để đổi bài (Luật lá 7)
    public void ReceiveTargetChoice(ulong clientId, ulong targetId)
    {
        if (!IsServer || currentState.phase != GamePhase.TargetSelection) return;

        // Chỉ người vừa đánh lá 7 mới được quyền yêu cầu đổi
        if (currentState.playerOrder[currentState.currentPlayerIndex] != clientId) return;

        // Chống hack: Không được tự đổi với chính mình, và mục tiêu phải tồn tại trong phòng
        if (clientId == targetId || !playerHands.ContainsKey(targetId)) return; 

        // 1. Thực hiện phép THUẬT hoán đổi 2 danh sách bài
        List<CardInstance> tempHand = playerHands[clientId];
        playerHands[clientId] = playerHands[targetId];
        playerHands[targetId] = tempHand;

        // 2. Trả game về trạng thái bình thường
        currentState.phase = GamePhase.Playing;

        // 3. Chuyển lượt đi cho người tiếp theo
        TurnManager.NextPlayer(ref currentState);
        
        // 4. Cập nhật và gửi trạng thái mới cho toàn bộ phòng
        UpdateHandCounts();
        BroadcastState();
    }

    // Xử lý khi người chơi đã chọn hướng chuyền bài (Luật lá 0)
    public void ReceivePassDirectionChoice(ulong clientId, bool passClockwise)
    {
        if (!IsServer || currentState.phase != GamePhase.DirectionSelection) return;
        if (currentState.playerOrder[currentState.currentPlayerIndex] != clientId) return;

        int count = currentState.playerCount;
        if (count > 1)
        {
            // Copy danh sách bài hiện tại ra một mảng tạm để lúc chuyền không bị đè mất dữ liệu
            List<CardInstance>[] tempHands = new List<CardInstance>[count];
            for(int i = 0; i < count; i++) 
            {
                tempHands[i] = playerHands[currentState.playerOrder[i]];
            }

            // Chuyền bài
            for (int i = 0; i < count; i++)
            {
                ulong currentId = currentState.playerOrder[i];
                // Tính toán xem mình sẽ nhận bài từ ai (người bên trái hay bên phải)
                int fromIndex = passClockwise 
                                ? (i - 1 + count) % count 
                                : (i + 1) % count;
                playerHands[currentId] = tempHands[fromIndex];
            }
        }

        currentState.phase = GamePhase.Playing;
        TurnManager.NextPlayer(ref currentState);
        
        UpdateHandCounts();
        BroadcastState();
    }

    // Xử lý Client yêu cầu rút bài
    public void TryDrawCard(ulong clientId)
    {
        if (!IsServer || currentState.phase != GamePhase.Playing) return;
        if (currentState.unoVulnerableId != 0 && currentState.unoVulnerableId != clientId)
            currentState.unoVulnerableId = 0;
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
            CardInstance drawn = deckManager.DrawCard();
            playerHands[clientId].Add(drawn);

            // Kiểm tra xem lá vừa rút có hợp lệ để đánh không
            if (CardValidator.IsLegal(drawn, currentState, playerHands[clientId].Count))
            {
                // Nếu đánh được: Giữ nguyên lượt (Không gọi TurnManager.NextPlayer)
                // Người chơi có thể click vào lá bài vừa rút trên màn hình để đánh xuống.
            }
            else
            {
                // Nếu không đánh được: Mất lượt
                TurnManager.NextPlayer(ref currentState);
            }
        }

        UpdateHandCounts();
        BroadcastState();
    }

    // Sửa void thành bool
    private bool ApplyCardEffects(CardInstance card)
    {
        if (card.type == CardType.Reverse)
        {
            if (currentState.playerCount == 2)
            {
                TurnManager.SkipNextPlayer(ref currentState); // Luật 2 người
                return true; // Báo hiệu: ĐÃ NHẢY CÓC LƯỢT
            }
            else
            {
                TurnManager.ReverseDirection(ref currentState);
                return false; // Chỉ đảo chiều xoay, KHÔNG nhảy lượt
            }
        }
        else if (card.type == CardType.Skip)
        {
            TurnManager.SkipNextPlayer(ref currentState);
            return true; // Báo hiệu: ĐÃ NHẢY CÓC LƯỢT
        }
        
        return false; // Các lá bài khác không làm nhảy cóc
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

        TriggerBotTurnIfNeeded();
    }

    private void StartReactionEvent()
    {
        currentState.phase = GamePhase.ReactionEvent;
        reactedPlayers.Clear();
        BroadcastState(); // Gửi tín hiệu để UI mọi người hiện nút React

        // Bắt đầu đếm ngược 3 giây
        if (reactionCoroutine != null) StopCoroutine(reactionCoroutine);
        reactionCoroutine = StartCoroutine(ReactionTimerRoutine());
    }

    private System.Collections.IEnumerator ReactionTimerRoutine()
    {
        yield return new WaitForSeconds(3f); // Chờ 3 giây
        ResolveReactionEvent(); // Hết giờ thì phân xử
    }

    public void ReceiveReaction(ulong clientId)
    {
        if (!IsServer || currentState.phase != GamePhase.ReactionEvent) return;

        // Nếu người này chưa bấm thì ghi tên vào danh sách
        if (!reactedPlayers.Contains(clientId))
        {
            reactedPlayers.Add(clientId);
        }

        // Nếu TẤT CẢ mọi người (cả host và client) đều đã bấm xong trước khi hết 3 giây
        if (reactedPlayers.Count >= currentState.playerCount)
        {
            if (reactionCoroutine != null) StopCoroutine(reactionCoroutine);
            ResolveReactionEvent();
        }
    }

    private void ResolveReactionEvent()
    {
        List<ulong> losers = new List<ulong>();

        // Phân xử: Ai là kẻ chậm nhất?
        if (reactedPlayers.Count < currentState.playerCount)
        {
            // Trường hợp 1: Có người KHÔNG thèm bấm -> Những người không có tên bị phạt
            for (int i = 0; i < currentState.playerCount; i++)
            {
                ulong id = currentState.playerOrder[i];
                if (!reactedPlayers.Contains(id)) losers.Add(id);
            }
        }
        else
        {
            // Trường hợp 2: Ai cũng bấm -> Kẻ bấm cuối cùng trong danh sách bị phạt
            losers.Add(reactedPlayers[reactedPlayers.Count - 1]);
        }

        // Phạt kẻ thua cuộc rút 2 lá
        foreach (ulong loserId in losers)
        {
            playerHands[loserId].Add(deckManager.DrawCard());
            playerHands[loserId].Add(deckManager.DrawCard());
        }

        // Dọn dẹp và đi tiếp
        reactedPlayers.Clear();
        currentState.phase = GamePhase.Playing;
        TurnManager.NextPlayer(ref currentState);
        
        UpdateHandCounts();
        BroadcastState();
    }

    public void ReceiveUnoCalled(ulong callerId)
    {
        if (!IsServer) return;

        ulong vulnerable = currentState.unoVulnerableId;
        if (vulnerable == 0) return; // window already closed, ignore

        if (callerId == vulnerable)
        {
            // Player called UNO on themselves in time — safe
            currentState.unoVulnerableId = 0;
        }
        else
        {
            // Another player caught them — penalize the vulnerable player
            playerHands[vulnerable].Add(deckManager.DrawCard());
            playerHands[vulnerable].Add(deckManager.DrawCard());
            currentState.unoVulnerableId = 0;
            UpdateHandCounts();
        }

        BroadcastState();
    }

    private void TriggerBotTurnIfNeeded()
    {
        if (currentState.phase != GamePhase.Playing) return;
        ulong currentId = TurnManager.GetCurrentPlayerId(ref currentState);
        if (BotManager.Instance != null && BotManager.Instance.IsBot(currentId))
            StartCoroutine(BotTurnRoutine(currentId));
    }

    private System.Collections.IEnumerator BotTurnRoutine(ulong botId)
    {
        yield return new WaitForSeconds(1.2f); // Simulate thinking
        ExecuteBotTurn(botId);
    }

    private void ExecuteBotTurn(ulong botId)
    {
        if (!IsServer) return;
        // Re-validate: still this bot's turn? (state may have changed)
        if (TurnManager.GetCurrentPlayerId(ref currentState) != botId) return;

        List<CardInstance> hand = playerHands[botId];

        // Find all legal cards
        var legalCards = new List<CardInstance>();
        foreach (var card in hand)
            if (CardValidator.IsLegal(card, currentState, hand.Count))
                legalCards.Add(card);

        if (legalCards.Count > 0)
        {
            // Play a random legal card
            CardInstance chosen = legalCards[Random.Range(0, legalCards.Count)];

            // If it's a Wild, pre-pick the color with the most cards in hand
            if (chosen.type == CardType.Wild || chosen.type == CardType.WildDrawFour)
            {
                TryPlayCard(botId, chosen);
                // After TryPlayCard, phase will be ColorSelection — resolve it immediately
                CardColor bestColor = PickBestColor(hand);
                ReceiveColorChoice(botId, bestColor);
            }
            else
            {
                TryPlayCard(botId, chosen);
            }
        }
        else
        {
            // No legal card — draw
            TryDrawCard(botId);
        }
    }

    private CardColor PickBestColor(List<CardInstance> hand)
    {
        int[] counts = new int[4]; // Red, Green, Blue, Yellow
        foreach (var c in hand)
        {
            if (c.color == CardColor.Red)    counts[0]++;
            if (c.color == CardColor.Green)  counts[1]++;
            if (c.color == CardColor.Blue)   counts[2]++;
            if (c.color == CardColor.Yellow) counts[3]++;
        }
        int best = 0;
        for (int i = 1; i < 4; i++) if (counts[i] > counts[best]) best = i;
        return (CardColor)best;
    }
}