using System.Collections.Generic;
using Unity.Netcode;
using Unity.Services.Authentication;
using Unity.Services.Lobbies.Models;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class PlayerListUI : MonoBehaviour
{
    public static PlayerListUI Instance { get; private set; }

    [Header("UI References")]
    [SerializeField] private StartGameButton     startGameButton;
    [SerializeField] private TextMeshProUGUI     roomCodeText;
    [SerializeField] private TextMeshProUGUI     playerCountText;
    [SerializeField] private HouseRulesPanel     houseRulesPanel;

    [Header("Single List Container")]
    [SerializeField] private Transform  listContainer;   // holds players, bots, AND the add bot button
    [SerializeField] private GameObject playerRowPrefab;
    [SerializeField] private GameObject botRowPrefab;
    [SerializeField] private GameObject addBotButton;    // pre-existing child of listContainer

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    public void ShowLobby()
    {
        roomCodeText.text = $"Room Code: {LobbyManager.Instance.GetLobbyCode()}";
        RefreshList(LobbyManager.Instance.GetCurrentPlayers());
        startGameButton?.RefreshVisibility();
        houseRulesPanel?.Refresh();
    }

    // Single entry point — call this whenever the lobby state changes
    public void RefreshList(List<Player> players)
    {
        // Guard: this UI no longer exists (e.g. scene has changed)
        if (listContainer == null) return;
        // Destroy all rows except the Add Bot button, which we reuse
        foreach (Transform child in listContainer)
        {
            if (child.gameObject != addBotButton)
                Destroy(child.gameObject);
        }

        bool isHost = NetworkManager.Singleton != null && NetworkManager.Singleton.IsHost;

        // int  bots   = BotManager.Instance?.BotCount ?? 0;
        int bots  = GetLobbyBotCount();
        int  total  = players.Count + bots;

        playerCountText.text = $"Players: {total} / {LobbyManager.MAX_PLAYERS}";

        // ── Real player rows ──────────────────────────────────────
        foreach (var player in players)
        {
            GameObject row   = Instantiate(playerRowPrefab, listContainer, false);
            var        label = row.GetComponent<TextMeshProUGUI>();
            if (label == null) continue;

            string name         = player.Data["PlayerName"].Value;
            bool   playerIsHost = player.Id == LobbyManager.Instance.CurrentLobby.HostId;
            bool   isLocal      = player.Id == AuthenticationService.Instance.PlayerId;

            label.text = $"{name}" +
                         $"{(playerIsHost ? "  [HOST]" : "")}" +
                         $"{(isLocal      ? "  (you)" : "")}";
        }

        // ── Bot rows ──────────────────────────────────────────────
        if (BotManager.Instance != null)
        {
            for (int i = 0; i < bots; i++)
            {
                int   capturedIndex = i;
                ulong botId         = BotManager.BOT_IDS[i];

                GameObject row   = Instantiate(botRowPrefab, listContainer, false);
                var        label = row.GetComponent<TextMeshProUGUI>();
                if (label != null)
                    label.text = $"{BotManager.Instance.GetBotName(botId)}  [BOT]";

                var removeButton = row.GetComponentInChildren<Button>();
                if (removeButton != null)
                {
                    removeButton.gameObject.SetActive(isHost);
                    if (isHost)
                        removeButton.onClick.AddListener(() => OnRemoveBotClicked(capturedIndex));
                }
            }
        }

        // ── Add Bot button — always last in the container ─────────
        if (addBotButton != null)
        {
            addBotButton.SetActive(isHost && total < LobbyManager.MAX_PLAYERS);
            addBotButton.transform.SetAsLastSibling();
        }
    }

    private int GetLobbyBotCount()
    {
        var data = LobbyManager.Instance?.CurrentLobby?.Data;
        if (data != null && data.TryGetValue("BotCount", out var entry))
            if (int.TryParse(entry.Value, out int count)) return count;
        return 0;
    }

    // Wire this to the Add Bot button's onClick in the Inspector
    public void OnAddBotClicked()
    {
        if (BotManager.Instance == null) return;
        var players = LobbyManager.Instance.GetCurrentPlayers();
        int total   = players.Count + BotManager.Instance.BotCount;
        if (total >= LobbyManager.MAX_PLAYERS) return;

        BotManager.Instance.AddBot();
        // NetworkGameManager.Instance?.SyncBotCountClientRpc(BotManager.Instance.BotCount);
        LobbyManager.Instance.UpdateLobbyDataAsync("BotCount", BotManager.Instance.BotCount.ToString());
        RefreshList(players);
    }

    private void OnRemoveBotClicked(int botIndex)
    {
        if (BotManager.Instance == null) return;
        BotManager.Instance.RemoveBot();
        // NetworkGameManager.Instance?.SyncBotCountClientRpc(BotManager.Instance.BotCount);
        LobbyManager.Instance.UpdateLobbyDataAsync("BotCount", BotManager.Instance.BotCount.ToString());
        RefreshList(LobbyManager.Instance.GetCurrentPlayers());
    }
    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }
}