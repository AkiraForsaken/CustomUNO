using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class NetworkGameManager : NetworkBehaviour
{
    public static NetworkGameManager Instance { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        if (IsClient)
        {
            GameEvents.OnPlayCardRequested += HandlePlayCardRequested;
            GameEvents.OnDrawCardRequested += HandleDrawCardRequested;
            GameEvents.OnColorChosen += HandleColorChosen;
            GameEvents.OnTargetChosen += HandleTargetChosen;
            GameEvents.OnPassDirectionChosen += HandlePassDirectionChosen;
            GameEvents.OnReactionClicked += HandleReactionClicked;
            GameEvents.OnUnoCalled += HandleUnoCalled;
        }

        // Host sẽ chạy Coroutine để khởi tạo UI và Game
        if (IsServer)
        {
            StartCoroutine(SafeStartGameRoutine());
        }
    }

    private System.Collections.IEnumerator SafeStartGameRoutine()
    {
        // 1. Đợi 2 giây để chắc chắn các máy (kể cả Host) đã Load xong Scene Game 100%
        yield return new WaitForSeconds(2f);

        // 2. KHỞI TẠO UI (Khắc phục lỗi số 2)
        // Lấy danh sách tên từ LobbyManager để truyền cho GameUI
        var connectedIds = NetworkManager.Singleton.ConnectedClientsIds;
        List<ulong> playerOrder = new List<ulong>(connectedIds);
        Dictionary<ulong, string> playerNames = new Dictionary<ulong, string>();
        
        var lobbyPlayers = LobbyManager.Instance.GetCurrentPlayers();
        for (int i = 0; i < lobbyPlayers.Count; i++)
        {
            // Tạm thời gán tên theo thứ tự kết nối (Cần tinh chỉnh map ID sau)
            if (i < playerOrder.Count)
            {
                playerNames[playerOrder[i]] = lobbyPlayers[i].Data["PlayerName"].Value;
            }
        }

        // Gọi UI khởi tạo trên máy Host (Client sẽ dùng ClientRpc để tự khởi tạo sau)
        FindObjectOfType<GameUI>()?.InitializeGame(playerOrder, playerNames);

        // 3. KHỞI TẠO BÀI (Khắc phục lỗi số 3)
        Debug.Log("--- BẮT ĐẦU START MATCH ---");
        GameManager.Instance.StartMatch();
    }
    public override void OnNetworkDespawn()
    {
        // Vẫn giữ nguyên việc gỡ sự kiện UI của Client
        if (IsClient)
        {
            GameEvents.OnPlayCardRequested -= HandlePlayCardRequested;
            GameEvents.OnDrawCardRequested -= HandleDrawCardRequested;
            GameEvents.OnColorChosen -= HandleColorChosen;
            GameEvents.OnTargetChosen -= HandleTargetChosen;
            GameEvents.OnPassDirectionChosen -= HandlePassDirectionChosen;
            GameEvents.OnReactionClicked -= HandleReactionClicked;
            GameEvents.OnUnoCalled -= HandleUnoCalled;
        }
        
        base.OnNetworkDespawn();
    }

    // ─────────────────────────────────────────────────────────────
    // [CLIENT GỌI SERVER] - Bắt sự kiện từ UI và đẩy lên Host
    // ─────────────────────────────────────────────────────────────

    private void HandlePlayCardRequested(CardInstance card)
    {
        RequestPlayCardServerRpc(card, NetworkManager.Singleton.LocalClientId);
    }

    private void HandleDrawCardRequested()
    {
        RequestDrawCardServerRpc(NetworkManager.Singleton.LocalClientId);
    }

    private void HandleColorChosen(CardColor color)
    {
        RequestColorChoiceServerRpc(color, NetworkManager.Singleton.LocalClientId);
    }

    // [ServerRpc]: Mã này CHỈ chạy trên Host/Server.
    // Client gọi hàm này, nhưng thực tế nó được thực thi tại máy của Host.
    [ServerRpc(RequireOwnership = false)]
    public void RequestPlayCardServerRpc(CardInstance card, ulong clientId)
    {
        // Chuyển yêu cầu cho GameManager xử lý logic
        GameManager.Instance.TryPlayCard(clientId, card);
    }

    [ServerRpc(RequireOwnership = false)]
    public void RequestDrawCardServerRpc(ulong clientId)
    {
        GameManager.Instance.TryDrawCard(clientId);
    }

    [ServerRpc(RequireOwnership = false)]
    public void RequestColorChoiceServerRpc(CardColor color, ulong clientId)
    {
        GameManager.Instance.ReceiveColorChoice(clientId, color);
    }

    // ─────────────────────────────────────────────────────────────
    // [SERVER GỌI CLIENT] - Host yêu cầu Client cập nhật UI
    // ─────────────────────────────────────────────────────────────

    /// <summary>
    /// Đồng bộ trạng thái game chung (Lá bài trên cùng, lượt đánh...) cho TẤT CẢ mọi người.
    /// </summary>
    [ClientRpc]
    public void SyncGameStateClientRpc(GameState state)
    {
        // Kích hoạt event cho các script UI (như GameUI.cs) tự động cập nhật hiển thị
        GameEvents.RaiseGameStateUpdated(state);
    }

    /// <summary>
    /// Đồng bộ bài ẩn. Kỹ thuật ClientRpcParams giúp Host chỉ gửi danh sách bài này
    /// đích danh cho đúng 1 Client, những người chơi khác hoàn toàn mù thông tin.
    /// </summary>
    [ClientRpc]
    public void SyncPrivateHandClientRpc(CardInstance[] handArray, ClientRpcParams clientRpcParams = default)
    {
        // UI dùng List, nhưng Netcode dùng Array (vì đã khai báo INetworkSerializable)
        // Nên ta chuyển Array ngược lại thành List khi nhận được dữ liệu.
        List<CardInstance> handList = new List<CardInstance>(handArray);
        GameEvents.RaiseLocalHandUpdated(handList);
    }

    /// <summary>
    /// Hàm helper để GameManager gọi. Nó tự động đóng gói List thành Array
    /// và thiết lập mục tiêu (TargetClientId) để gửi RPC ẩn.
    /// </summary>
    public void SyncPrivateHand(ulong targetClientId, List<CardInstance> hand)
    {
        ClientRpcParams rpcParams = new ClientRpcParams
        {
            Send = new ClientRpcSendParams
            {
                TargetClientIds = new ulong[] { targetClientId }
            }
        };

        // Chuyển List thành Array trước khi truyền qua mạng
        SyncPrivateHandClientRpc(hand.ToArray(), rpcParams);
    }

    /// <summary>
    /// Thông báo kết thúc game.
    /// </summary>
    [ClientRpc]
    public void NotifyGameOverClientRpc(string winnerId)
    {
        GameEvents.RaiseGameOver(winnerId);
    }

    private void HandleTargetChosen(ulong targetId)
    {
        // Gửi ID của người bị chọn và ID của chính mình lên Server
        RequestTargetServerRpc(targetId, NetworkManager.Singleton.LocalClientId);
    }

    [ServerRpc(RequireOwnership = false)]
    public void RequestTargetServerRpc(ulong targetId, ulong clientId)
    {
        // Chuyển cho GameManager xử lý việc tráo đổi bài
        GameManager.Instance.ReceiveTargetChoice(clientId, targetId);
    }

    private void HandlePassDirectionChosen(bool isClockwise)
    {
        RequestPassDirectionServerRpc(isClockwise, NetworkManager.Singleton.LocalClientId);
    }

    [ServerRpc(RequireOwnership = false)]
    public void RequestPassDirectionServerRpc(bool isClockwise, ulong clientId)
    {
        GameManager.Instance.ReceivePassDirectionChoice(clientId, isClockwise);
    }

    private void HandleReactionClicked()
    {
        RequestReactionServerRpc(NetworkManager.Singleton.LocalClientId);
    }

    [ServerRpc(RequireOwnership = false)]
    public void RequestReactionServerRpc(ulong clientId)
    {
        GameManager.Instance.ReceiveReaction(clientId);
    }

    [ClientRpc]
    public void SyncAllPlayerNamesClientRpc(ulong[] ids, string joinedNames)
    {
        // Cắt chuỗi gộp ra lại thành danh sách tên bằng dấu |
        string[] names = joinedNames.Split('|');

        Dictionary<ulong, string> nameMap = new Dictionary<ulong, string>();
        for (int i = 0; i < ids.Length; i++)
        {
            // Tránh lỗi nếu mảng tên bị thiếu
            nameMap[ids[i]] = (i < names.Length) ? names[i] : "Player"; 
        }

        // Cập nhật vào UI
        var ui = Object.FindAnyObjectByType<GameUI>();
        if (ui != null)
        {
            ui.UpdatePlayerNames(nameMap);
        }
    }

    /// Host → all clients: someone added or removed a bot in the lobby
    [ClientRpc]
    public void SyncBotCountClientRpc(int botCount)
    {
        if (IsServer) return; // host already has the correct count
        BotManager.Instance?.SetBotCount(botCount);
        PlayerListUI.Instance?.RefreshList(LobbyManager.Instance.GetCurrentPlayers());
    }

    /// Host → all clients: a house rule toggle changed
    [ClientRpc]
    public void SyncHouseRulesClientRpc(bool ruleZero, bool ruleSeven, bool ruleEight)
    {
        if (IsServer) return; // host already has the correct config
        var config = new HouseRulesConfig
        {
            ruleZeroEnabled  = ruleZero,
            ruleSevenEnabled = ruleSeven,
            ruleEightEnabled = ruleEight
        };
        HouseRulesManager.Instance?.ApplyConfig(config);
        Object.FindAnyObjectByType<HouseRulesPanel>()?.Refresh();
    }

    private void HandleUnoCalled()
    {
        RequestUnoServerRpc(NetworkManager.Singleton.LocalClientId);
    }

    [ServerRpc(RequireOwnership = false)]
    public void RequestUnoServerRpc(ulong callerId)
    {
        GameManager.Instance.ReceiveUnoCalled(callerId);
    }
}