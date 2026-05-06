using System.Collections.Generic;
using Unity.Netcode; 
using Unity.Services.Authentication;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Unity.Services.Lobbies.Models;
using System.Linq;
using UnityEngine.SceneManagement;

public class GameUI : MonoBehaviour
{
    [Header("Opponent Panels (Top / Left / Right)")]
    [SerializeField] private OpponentUI opponentTop;
    [SerializeField] private OpponentUI opponentLeft;
    [SerializeField] private OpponentUI opponentRight;

    [Header("Center Area")]
    [SerializeField] private CardFront        topCard;
    [SerializeField] private Image            activeColorRing;   
    [SerializeField] private TextMeshProUGUI  currentColorText;  
    [SerializeField] private TextMeshProUGUI  pileCountText;
    [SerializeField] private Image            directionIndicator; 
    [SerializeField] private Button           drawPileButton;

    [Header("Turn Indicator")]
    [SerializeField] private TextMeshProUGUI turnText;
    [SerializeField] private GameObject      yourTurnBadge;      

    [Header("Color Picker")]
    [SerializeField] private GameObject colorPickerPanel;
    [SerializeField] private Button     colorRed;
    [SerializeField] private Button     colorGreen;
    [SerializeField] private Button     colorBlue;
    [SerializeField] private Button     colorYellow;

    [Header("Target Picker (Rule 7)")]
    [SerializeField] private GameObject targetPickerPanel;
    [SerializeField] private Button target1Button; 
    [SerializeField] private Button target2Button;
    [SerializeField] private Button target3Button;

    [Header("Direction Picker (Rule 0)")]
    [SerializeField] private GameObject directionPickerPanel;
    [SerializeField] private Button     clockwiseButton;        // Nút Cùng chiều
    [SerializeField] private Button     counterClockwiseButton; // Nút Ngược chiều

    [Header("Reaction Event (Rule 8)")]
    [SerializeField] private GameObject reactionPanel;
    [SerializeField] private Button     reactButton;

    [Header("Game Over")]
    [SerializeField] private GameObject      gameOverPanel;
    [SerializeField] private TextMeshProUGUI gameOverText;
    [SerializeField] private Button          backToMenuButton;

    [Header("Color Ring Sprites (solid color images)")]
    [SerializeField] private Sprite ringRed;
    [SerializeField] private Sprite ringGreen;
    [SerializeField] private Sprite ringBlue;
    [SerializeField] private Sprite ringYellow;

    // Đổi sang ulong
    private readonly List<ulong>      opponentIds   = new();
    private readonly List<OpponentUI> opponentSlots = new();

    // Cache names
    private Dictionary<ulong, string> playerNames = new();

    private void Awake()
    {
        opponentSlots.Add(opponentTop);
        opponentSlots.Add(opponentLeft);
        opponentSlots.Add(opponentRight);

        colorPickerPanel.SetActive(false);
        gameOverPanel.SetActive(false);
        if (yourTurnBadge != null) yourTurnBadge.SetActive(false);
        if (currentColorText != null) currentColorText.gameObject.SetActive(false);

        opponentTop.Clear();
        opponentLeft.Clear();
        opponentRight.Clear();

        WireColorPickerButtons();
        if (backToMenuButton != null)
            backToMenuButton.onClick.AddListener(OnBackToMenuClicked);
        if (drawPileButton != null)
            drawPileButton.onClick.AddListener(() => GameEvents.RaiseDrawCardRequested());

        if (clockwiseButton != null) 
            clockwiseButton.onClick.AddListener(() => ChooseDirection(true));
        
        if (counterClockwiseButton != null) 
            counterClockwiseButton.onClick.AddListener(() => ChooseDirection(false));
        
        if (reactButton != null)
        {
            reactButton.onClick.AddListener(OnReactButtonClicked);
        }
        // Fallback: if the networking layer didn't call InitializeGame,
        // try to populate player names from the LobbyManager so UI shows names during testing.
        TryInitializeFromLobbyIfNeeded();
    }

    // During development it's convenient to show player names even if NetworkGameManager
    // hasn't wired them yet. This reads the LobbyManager's current players (if any)
    // and calls InitializeGame with a simple starting-hand map so opponent panels show names.
    private void TryInitializeFromLobbyIfNeeded()
    {
        if (playerNames != null && playerNames.Count > 0) return; // already set
        if (typeof(LobbyManager) == null) return;
        if (LobbyManager.Instance == null) return;

        var players = LobbyManager.Instance.GetCurrentPlayers();
        if (players == null || players.Count == 0) return;

        // Build player order and name map. Convert lobby string IDs to ulong where possible.
        var order = new List<ulong>();
        var names = new Dictionary<ulong, string>();
        var handCounts = new Dictionary<ulong, int>();

        for (int i = 0; i < players.Count; i++)
        {
            var p = players[i];
            ulong id;
            if (!ulong.TryParse(p.Id, out id))
            {
                // Fallback: synthesize a stable numeric id for editor/testing when Player.Id isn't numeric.
                id = (ulong)(14680064 + i); // large offset to avoid colliding with real client ids
            }

            string nm = p.Data != null && p.Data.ContainsKey("PlayerName") ? p.Data["PlayerName"].Value : "Player";
            order.Add(id);
            names[id] = nm;
            handCounts[id] = 7; // placeholder until DeckManager assigns real hands
        }

        InitializeGame(order, names, handCounts);
    }

    private void OnEnable()
    {
        GameEvents.OnGameStateUpdated += OnGameStateUpdated;
        GameEvents.OnGameOver         += OnGameOver;
    }

    private void OnDisable()
    {
        GameEvents.OnGameStateUpdated -= OnGameStateUpdated;
        GameEvents.OnGameOver         -= OnGameOver;
    }

    // Đổi tham số sang ulong
    public void InitializeGame(List<ulong> playerOrder, Dictionary<ulong, string> names)
    {
        playerNames = names;
        ulong localId = NetworkManager.Singleton.LocalClientId;

        opponentIds.Clear();
        foreach (ulong id in playerOrder)
            if (id != localId) opponentIds.Add(id);

        AssignOpponentSlots();
    }

    // Overload used by the lobby-fallback which also provides initial hand counts.
    public void InitializeGame(List<ulong> playerOrder, Dictionary<ulong, string> names, Dictionary<ulong, int> handCounts)
    {
        playerNames = names;
        ulong localId = NetworkManager.Singleton != null ? NetworkManager.Singleton.LocalClientId : 0ul;

        opponentIds.Clear();
        foreach (ulong id in playerOrder)
            if (id != localId) opponentIds.Add(id);

        AssignOpponentSlots();

        if (handCounts != null)
        {
            foreach (var slot in opponentSlots)
            {
                if (!slot.gameObject.activeSelf) continue;
                if (handCounts.TryGetValue(slot.AssignedPlayerId, out int cnt))
                {
                    slot.Refresh(cnt);
                }
            }
        }
    }

    private void AssignOpponentSlots()
    {
        for (int i = 0; i < opponentSlots.Count; i++)
        {
            if (i < opponentIds.Count)
            {
                ulong id = opponentIds[i];
                string name = playerNames.TryGetValue(id, out string n) ? n : "Player";
                opponentSlots[i].Assign(id, name);
            }
            else
            {
                opponentSlots[i].Clear(); 
            }
        }
    }

    private void OnGameStateUpdated(GameState state)
    {
        if (state.playerCount == 0) return; // Check count thay vì null

        if (opponentIds.Count != state.playerCount - 1 || !playerNames.ContainsKey(state.playerOrder[0]))
        {
            AutoSyncPlayers(state);
        }

        UpdateTopCard(state);
        UpdateDirection(state);
        UpdateTurnIndicator(state);
        UpdateOpponentHandCounts(state);
        HandlePhaseChange(state);
    }

    private void UpdateTopCard(GameState state)
    {
        // 1. Gọi Setup để nó tự đắp phôi bài và Icon lên (thay cho đoạn switch rườm rà cũ)
        if (topCard != null) 
        {
            topCard.Setup(state.topCard);
        }

        // 2. GIỮ NGUYÊN đoạn code kiểm tra lá Wild để hiện chữ thông báo màu trên bàn chơi
        bool wildActive = state.topCard.type == CardType.Wild
                       || state.topCard.type == CardType.WildDrawFour;
        if (currentColorText != null)
        {
            currentColorText.gameObject.SetActive(wildActive);
            if (wildActive) currentColorText.text = state.activeColor.ToString();
        }
    }

    private void UpdateDirection(GameState state)
    {
        if (directionIndicator != null)
            directionIndicator.rectTransform.localScale = state.isClockwise
                ? Vector3.one
                : new Vector3(-1f, 1f, 1f);
    }

    private void UpdateTurnIndicator(GameState state)
    {
        ulong localId = NetworkManager.Singleton.LocalClientId;
        bool isMyTurn = state.playerCount > 0 && state.playerOrder[state.currentPlayerIndex] == localId;

        if (yourTurnBadge != null) yourTurnBadge.SetActive(isMyTurn);

        if (turnText != null)
        {
            if (isMyTurn)
            {
                turnText.text = "Your turn";
            }
            else
            {
                ulong currentId = state.playerOrder[state.currentPlayerIndex];
                string currentName = playerNames.TryGetValue(currentId, out string n) ? n : "...";
                turnText.text = $"{currentName}'s turn";
            }
        }
    }

    private void UpdateOpponentHandCounts(GameState state)
    {
        if (state.handCounts == null) return;

        foreach (var slot in opponentSlots)
        {
            if (!slot.gameObject.activeSelf) continue;
            
            // Tìm trong mảng của GameState
            for (int i = 0; i < state.playerCount; i++)
            {
                if (state.playerOrder[i] == slot.AssignedPlayerId)
                {
                    slot.Refresh(state.handCounts[i]);
                    break;
                }
            }
        }
    }

    private void HandlePhaseChange(GameState state)
    {
        // 1. Lấy ID của máy hiện tại và ID của người đang đến lượt
        ulong localId = NetworkManager.Singleton.LocalClientId;
        bool isMyTurn = state.playerOrder[state.currentPlayerIndex] == localId;

        // 2. Chỉ hiện bảng chọn màu cho chính người đánh lá Wild
        colorPickerPanel.SetActive(state.phase == GamePhase.ColorSelection && isMyTurn);
        
        // 3. Rule 7: Chỉ hiện bảng chọn người cho chính người đánh lá 7 [cite: 77, 80]
        if (targetPickerPanel != null)
        {
            bool showTargetPicker = (state.phase == GamePhase.TargetSelection && isMyTurn);
            targetPickerPanel.SetActive(showTargetPicker);
            
            if (showTargetPicker)
            {
                SetupTargetButtons();
            }
        }

        // 4. Rule 0: Chỉ hiện bảng chọn hướng cho chính người đánh lá 0 [cite: 68, 74]
        if (directionPickerPanel != null)
        {
            directionPickerPanel.SetActive(state.phase == GamePhase.DirectionSelection && isMyTurn);
        }

        // 5. Rule 8 (Reaction): HIỆN CHO TẤT CẢ MỌI NGƯỜI
        // Vì đặc tả yêu cầu tất cả người chơi phải bấm nút phản xạ 
        if (reactionPanel != null) 
        {
            reactionPanel.SetActive(state.phase == GamePhase.ReactionEvent);
        }
    }

    private void OnReactButtonClicked()
    {
        // Bấm xong thì ẩn nút đi luôn để tránh spam click
        reactionPanel.SetActive(false); 
        GameEvents.RaiseReactionClicked();
    }

    private void WireColorPickerButtons()
    {
        colorRed.onClick.AddListener(   () => ChooseColor(CardColor.Red));
        colorGreen.onClick.AddListener( () => ChooseColor(CardColor.Green));
        colorBlue.onClick.AddListener(  () => ChooseColor(CardColor.Blue));
        colorYellow.onClick.AddListener(() => ChooseColor(CardColor.Yellow));
    }

    private void ChooseColor(CardColor color)
    {
        colorPickerPanel.SetActive(false);
        GameEvents.RaiseColorChosen(color);
    }

    private void OnGameOver(string winnerIdStr)
    {
        if (ulong.TryParse(winnerIdStr, out ulong winnerId))
        {
            string name = playerNames.TryGetValue(winnerId, out string n) ? n : "Player";
            bool isMe = winnerId == NetworkManager.Singleton.LocalClientId;

            gameOverText.text = isMe ? "You win!" : $"{name} wins!";
            gameOverPanel.SetActive(true);
        }
    }

    private void OnBackToMenuClicked()
    {
        NetworkManager.Singleton.Shutdown();
        SceneManager.LoadScene("MainMenu");
    }

    private Sprite GetRingSprite(CardColor color) => color switch
    {
        CardColor.Red    => ringRed,
        CardColor.Green  => ringGreen,
        CardColor.Blue   => ringBlue,
        CardColor.Yellow => ringYellow,
        _                => ringRed
    };

    public void SetDrawPileCount(int count)
    {
        if (pileCountText != null) pileCountText.text = count.ToString();
    }

    private void AutoSyncPlayers(GameState state)
    {
        ulong localId = NetworkManager.Singleton.LocalClientId;
        playerNames.Clear();
        opponentIds.Clear();

        // Lấy danh sách tên từ Lobby (nếu có)
        var lobbyPlayers = LobbyManager.Instance?.GetCurrentPlayers();

        // Dùng danh sách ID mạng CHUẨN XÁC từ GameState
        for (int i = 0; i < state.playerCount; i++)
        {
            ulong netId = state.playerOrder[i];
            string name = $"Player {i + 1}";

            // Lắp tên vào đúng người
            if (lobbyPlayers != null && i < lobbyPlayers.Count)
            {
                var lp = lobbyPlayers[i];
                if (lp.Data != null && lp.Data.ContainsKey("PlayerName"))
                    name = lp.Data["PlayerName"].Value;
            }

            playerNames[netId] = name;
            
            // Nếu không phải bản thân mình thì thêm vào danh sách đối thủ
            if (netId != localId) 
            {
                opponentIds.Add(netId);
            }
        }
        AssignOpponentSlots();
    }

    private void SetupTargetButtons()
    {
        // Tắt hết các nút trước
        target1Button.gameObject.SetActive(false);
        target2Button.gameObject.SetActive(false);
        target3Button.gameObject.SetActive(false);

        Button[] btnArray = { target1Button, target2Button, target3Button };

        // Duyệt qua danh sách đối thủ (opponentIds đã có sẵn trong GameUI)
        for (int i = 0; i < opponentIds.Count; i++)
        {
            if (i >= btnArray.Length) break;

            ulong targetId = opponentIds[i];
            string tName = playerNames.TryGetValue(targetId, out string n) ? n : "Player";
            
            // Đổi chữ trên nút thành tên đối thủ
            var textUI = btnArray[i].GetComponentInChildren<TextMeshProUGUI>();
            if (textUI != null) textUI.text = tName;

            // Xóa sự kiện cũ và gắn sự kiện mới để khi bấm thì gửi đúng ID của người đó
            btnArray[i].onClick.RemoveAllListeners();
            btnArray[i].onClick.AddListener(() => ChooseTarget(targetId));
            
            btnArray[i].gameObject.SetActive(true);
        }
    }

    private void ChooseTarget(ulong targetId)
    {
        targetPickerPanel.SetActive(false);
        GameEvents.RaiseTargetChosen(targetId);
    }

    private void ChooseDirection(bool isClockwise)
    {
        directionPickerPanel.SetActive(false);
        GameEvents.RaisePassDirectionChosen(isClockwise);
    }

    // Thêm hàm này vào GameUI.cs
    public void UpdatePlayerNames(Dictionary<ulong, string> names)
    {
        this.playerNames = names;
        
        // Cập nhật lại các ô hiển thị đối thủ (Top, Left, Right)
        AssignOpponentSlots();
        
        // Buộc UI cập nhật lại văn bản lượt đi (để mất chữ ...'s turn)
        if (GameManager.Instance != null)
        {
            UpdateTurnIndicator(GameManager.Instance.currentState);
        }
    }
}