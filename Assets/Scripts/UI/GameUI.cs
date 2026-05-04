using System.Collections.Generic;
using Unity.Services.Authentication;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Unity.Services.Lobbies.Models;
using System.Linq;

// Attach to the GameUI root object in the Game scene.
// This is the single coordinator: it subscribes to GameEvents,
// reads shared GameState, and pushes data down to every child UI.
//
// Member 2 (NetworkGameManager) communicates upward exclusively
// through GameEvents — no direct reference needed.
public class GameUI : MonoBehaviour
{
    // ── Opponent slots (Top, Left, Right) ────────────────────────
    [Header("Opponent Panels (Top / Left / Right)")]
    [SerializeField] private OpponentUI opponentTop;
    [SerializeField] private OpponentUI opponentLeft;
    [SerializeField] private OpponentUI opponentRight;

    // ── Center area ───────────────────────────────────────────────
    [Header("Center Area")]
    [SerializeField] private Image            topCardImage;
    [SerializeField] private Image            activeColorRing;   // child of DiscardPile (unused when using TMP text)
    [SerializeField] private TextMeshProUGUI  currentColorText;  // Text below DiscardPile showing current color
    [SerializeField] private TextMeshProUGUI  pileCountText;
    [SerializeField] private Image            directionIndicator; // ↻ icon
    [SerializeField] private Button           drawPileButton;

    // ── Turn indicator ────────────────────────────────────────────
    [Header("Turn Indicator")]
    [SerializeField] private TextMeshProUGUI turnText;
    [SerializeField] private GameObject      yourTurnBadge;      // "Your Turn!" highlight

    // ── Overlay panels ─────────────────────────────────────────────
    [Header("Color Picker")]
    [SerializeField] private GameObject colorPickerPanel;
    [SerializeField] private Button     colorRed;
    [SerializeField] private Button     colorGreen;
    [SerializeField] private Button     colorBlue;
    [SerializeField] private Button     colorYellow;

    [Header("Game Over")]
    [SerializeField] private GameObject      gameOverPanel;
    [SerializeField] private TextMeshProUGUI gameOverText;
    [SerializeField] private Button          backToMenuButton;

    // ── Card sprites for the discard pile display ─────────────────
    [Header("Color Ring Sprites (solid color images)")]
    [SerializeField] private Sprite ringRed;
    [SerializeField] private Sprite ringGreen;
    [SerializeField] private Sprite ringBlue;
    [SerializeField] private Sprite ringYellow;

    // ── Private state ─────────────────────────────────────────────
    // Ordered list of opponent IDs as assigned at game start
    // Index 0 = Top, 1 = Left, 2 = Right
    private readonly List<string>     opponentIds   = new();
    private readonly List<OpponentUI> opponentSlots = new();

    // Cache of playerId → displayName (populated from LobbyManager)
    private Dictionary<string, string> playerNames = new();

    // Cache of playerId → hand count (updated from GameState)
    private Dictionary<string, int> handCounts = new();

    // ── Lifecycle ─────────────────────────────────────────────────
    private void Awake()
    {
        opponentSlots.Add(opponentTop);
        opponentSlots.Add(opponentLeft);
        opponentSlots.Add(opponentRight);

        // Hide all overlays on start
        colorPickerPanel.SetActive(false);
        gameOverPanel.SetActive(false);
        if (yourTurnBadge != null)
            yourTurnBadge.SetActive(false);
        if (currentColorText != null)
            currentColorText.gameObject.SetActive(false);

        // All opponent panels start hidden; AssignOpponentSlots shows them
        opponentTop.Clear();
        opponentLeft.Clear();
        opponentRight.Clear();

        WireColorPickerButtons();
        if (backToMenuButton != null)
            backToMenuButton.onClick.AddListener(OnBackToMenuClicked);
        if (drawPileButton != null)
            drawPileButton.onClick.AddListener(() => GameEvents.RaiseDrawCardRequested());

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

        // Build player order and name map. Use the order provided by the Lobby.
        var order = new List<string>();
        var names = new Dictionary<string, string>();
        var handCounts = new Dictionary<string, int>();

        foreach (var p in players)
        {
            order.Add(p.Id);
            string nm = p.Data != null && p.Data.ContainsKey("PlayerName") ? p.Data["PlayerName"].Value : "Player";
            names[p.Id] = nm;
            handCounts[p.Id] = 7; // placeholder until DeckManager assigns real hands
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

    // ── Called by NetworkGameManager once after the scene loads ───
    // playerOrder: Auth player IDs in turn order (index 0 = host)
    // names:       playerId → display name from Lobby data
    // handCounts:  playerId → starting hand size (7 for everyone)
    public void InitializeGame(List<string> playerOrder,
                               Dictionary<string, string> names,
                               Dictionary<string, int> startingHandCounts)
    {
        playerNames = names;
        handCounts  = startingHandCounts;

        string localId = AuthenticationService.Instance.PlayerId;

        // Build the ordered list of opponents (everyone except the local player),
        // preserving turn order so Top is always "next" in the rotation.
        opponentIds.Clear();
        foreach (string id in playerOrder)
            if (id != localId) opponentIds.Add(id);

        AssignOpponentSlots();
    }

    // ── Opponent slot assignment ──────────────────────────────────
    // Maps opponentIds[0..n] to the Top/Left/Right panels.
    // With 2 players total: 1 opponent → Top only.
    // With 3 players: 2 opponents → Top + Left.
    // With 4 players: 3 opponents → Top + Left + Right.
    private void AssignOpponentSlots()
    {
        for (int i = 0; i < opponentSlots.Count; i++)
        {
            if (i < opponentIds.Count)
            {
                string id   = opponentIds[i];
                string name = playerNames.TryGetValue(id, out string n) ? n : "Player";
                opponentSlots[i].Assign(id, name);
                opponentSlots[i].Refresh(handCounts.TryGetValue(id, out int c) ? c : 7);
            }
            else
            {
                opponentSlots[i].Clear(); // hides the panel entirely
            }
        }
    }

    // ── GameState update (fired after every card played / drawn) ──
    private void OnGameStateUpdated(GameState state)
    {
        if (state == null) return;

        UpdateTopCard(state);
        UpdateDirection(state);
        UpdateTurnIndicator(state);
        UpdateOpponentHandCounts(state);
        HandlePhaseChange(state);
    }

    private void UpdateTopCard(GameState state)
    {
        if (state.topCard == null) return;

        // Load the face sprite from Resources using the same naming convention as CardFront
        string symbolName = state.topCard.type switch
        {
            CardType.Number       => $"_{state.topCard.number}",
            CardType.Skip         => "_skip",
            CardType.Reverse      => "_reverse",
            CardType.DrawTwo      => "_drawtwo",
            CardType.Wild         => "_wild",
            CardType.WildDrawFour => "_wild_draw",
            _                     => "_0"
        };
        var symbol = Resources.Load<Sprite>($"CardSymbols/{symbolName}");
        if (symbol != null && topCardImage != null) topCardImage.sprite = symbol;

        // Show the current color text when a Wild has been played
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
        // Flip the direction indicator sprite horizontally for counter-clockwise
        if (directionIndicator != null)
            directionIndicator.rectTransform.localScale = state.isClockwise
                ? Vector3.one
                : new Vector3(-1f, 1f, 1f);
    }

    private void UpdateTurnIndicator(GameState state)
    {
        string localId = AuthenticationService.Instance.PlayerId;
        bool   isMyTurn = state.playerOrder.Count > 0
                        && state.playerOrder[state.currentPlayerIndex] == localId;

        if (yourTurnBadge != null)
            yourTurnBadge.SetActive(isMyTurn);

        if (turnText != null)
        {
            if (isMyTurn)
            {
                turnText.text = "Your turn";
            }
            else
            {
                string currentId   = state.playerOrder[state.currentPlayerIndex];
                string currentName = playerNames.TryGetValue(currentId, out string n) ? n : "...";
                turnText.text = $"{currentName}'s turn";
            }
        }
    }

    private void UpdateOpponentHandCounts(GameState state)
    {
        // GameState needs to carry hand counts (not full hands) in a public field.
        // Member 2 adds:  public Dictionary<string,int> handCounts = new();  to GameState.
        // Until then this method is a no-op so nothing breaks.
        if (state.handCounts == null) return;

        foreach (var slot in opponentSlots)
        {
            if (string.IsNullOrEmpty(slot.AssignedPlayerId)) continue;
            if (state.handCounts.TryGetValue(slot.AssignedPlayerId, out int count))
                slot.Refresh(count);
        }
    }

    private void HandlePhaseChange(GameState state)
    {
        // Show the color picker only during ColorSelection phase
        colorPickerPanel.SetActive(state.phase == GamePhase.ColorSelection);
    }

    // ── Color picker ──────────────────────────────────────────────
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

    // ── Game over ─────────────────────────────────────────────────
    private void OnGameOver(string winnerId)
    {
        string name   = playerNames.TryGetValue(winnerId, out string n) ? n : "Someone";
        bool   isMe   = winnerId == AuthenticationService.Instance.PlayerId;

        gameOverText.text = isMe ? "You win!" : $"{name} wins!";
        gameOverPanel.SetActive(true);
    }

    private void OnBackToMenuClicked()
    {
        LobbyManager.Instance.LeaveLobby();
        // NGO shutdown triggers the MainMenu scene reload via
        // the OnClientDisconnected handler in MainMenuController
        Unity.Netcode.NetworkManager.Singleton.Shutdown();
    }

    // ── Helpers ───────────────────────────────────────────────────
    private Sprite GetRingSprite(CardColor color) => color switch
    {
        CardColor.Red    => ringRed,
        CardColor.Green  => ringGreen,
        CardColor.Blue   => ringBlue,
        CardColor.Yellow => ringYellow,
        _                => ringRed
    };

    // Convenience — lets NetworkGameManager update the draw pile count label
    public void SetDrawPileCount(int count)
    {
        if (pileCountText != null)
            pileCountText.text = count.ToString();
    }
}