using System.Collections.Generic;
using Unity.Services.Authentication;
using Unity.Services.Lobbies.Models;
using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class PlayerListUI : MonoBehaviour
{
  public static PlayerListUI Instance { get; private set; }

  [Header("UI References")]
  [SerializeField] private StartGameButton startGameButton;
  [SerializeField] private Transform playerListContainer;  // parent object for rows
  [SerializeField] private GameObject playerRowPrefab;     // one row per player
  [SerializeField] private TextMeshProUGUI roomCodeText;
  [SerializeField] private TextMeshProUGUI playerCountText;
  [SerializeField] private HouseRulesPanel houseRulesPanel;

  [Header("Bot Controls")]
  [SerializeField] private GameObject addBotButton;      // A Button GameObject
  [SerializeField] private Transform  botListContainer;  // Parent for bot name rows
  [SerializeField] private GameObject botRowPrefab;

  private void Awake()
  {
    if (Instance != null && Instance != this) { Destroy(gameObject); return; }
    Instance = this;
  }

  public void ShowLobby()
  {
    roomCodeText.text = $"Room Code: {LobbyManager.Instance.GetLobbyCode()}";
    var players = LobbyManager.Instance.GetCurrentPlayers();
    RefreshPlayerList(players);
    RefreshBotList(players.Count);
    startGameButton?.RefreshVisibility();
    houseRulesPanel?.Refresh();
  }

  public void RefreshPlayerList(List<Player> players)
  {
    ClearChildrenExceptButtons(playerListContainer);

    int totalCount = players.Count + (BotManager.Instance?.BotCount ?? 0);
    playerCountText.text = $"Players: {totalCount} / {LobbyManager.MAX_PLAYERS}";

    foreach (var player in players)
    {
      GameObject row  = Instantiate(playerRowPrefab, playerListContainer, false);
      var label = row.GetComponent<TextMeshProUGUI>();
      if (label == null) continue;

      string name      = player.Data["PlayerName"].Value;
      bool   isHost    = player.Id == LobbyManager.Instance.CurrentLobby.HostId;
      bool   isLocal   = player.Id == AuthenticationService.Instance.PlayerId;
      label.text       = $"{name}{(isHost ? "  [HOST]" : "")}{(isLocal ? "  (you)" : "")}";
    }
  }

  private void ClearChildrenExceptButtons(Transform container)
  {
    if (container == null) return;
    var toDestroy = new List<GameObject>();
    foreach (Transform child in container)
    {
      if (child == null) continue;
      if (child.GetComponent<Button>() != null) continue; // don't destroy UI Buttons
      toDestroy.Add(child.gameObject);
    }

    foreach (var go in toDestroy)
      Destroy(go);
  }

  private void RefreshBotList(int humanPlayerCount)
  {
    if (BotManager.Instance == null) return;
    bool isHost = Unity.Netcode.NetworkManager.Singleton != null 
               && Unity.Netcode.NetworkManager.Singleton.IsHost;

    // Clear old bot rows (but don't destroy UI Buttons)
    ClearChildrenExceptButtons(botListContainer);

    // Render a row for each active bot
    int bots = BotManager.Instance.BotCount;
    for (int i = 0; i < bots; i++)
    {
        ulong botId = BotManager.BOT_IDS[i];
        GameObject row = Instantiate(botRowPrefab, botListContainer, false);
        var label = row.GetComponent<TextMeshProUGUI>();
        if (label != null)
            label.text = $"{BotManager.Instance.GetBotName(botId)}  [BOT]";
    }

    // Show/hide the "Add Bot" button — host only, and only when room isn't full
    if (addBotButton != null)
    {
        int total = humanPlayerCount + bots;
        addBotButton.SetActive(isHost && total < LobbyManager.MAX_PLAYERS);
    }
  }

  public void OnAddBotClicked()
  {
    if (BotManager.Instance == null) return;
    int humanCount = LobbyManager.Instance.GetCurrentPlayers().Count;
    if (!BotManager.Instance.CanAddBot(humanCount + BotManager.Instance.BotCount)) return;
    BotManager.Instance.AddBot();
    RefreshBotList(humanCount);

    // Update the total count label
    int total = humanCount + BotManager.Instance.BotCount;
    playerCountText.text = $"Players: {total} / {LobbyManager.MAX_PLAYERS}";
  }
}