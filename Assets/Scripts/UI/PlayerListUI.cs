using System.Collections.Generic;
using Unity.Services.Authentication;
using Unity.Services.Lobbies.Models;
using UnityEngine;
using TMPro;

public class PlayerListUI : MonoBehaviour
{
  public static PlayerListUI Instance { get; private set; }

  [Header("UI References")]
  [SerializeField] private StartGameButton startGameButton;
  [SerializeField] private Transform playerListContainer;  // parent object for rows
  [SerializeField] private GameObject playerRowPrefab;     // one row per player
  [SerializeField] private TextMeshProUGUI roomCodeText;
  [SerializeField] private TextMeshProUGUI playerCountText; 

  private void Awake()
  {
    if (Instance != null && Instance != this) { Destroy(gameObject); return; }
    Instance = this;
  }

  public void ShowLobby()
  {
    roomCodeText.text = $"Room Code: {LobbyManager.Instance.GetLobbyCode()}";
    RefreshPlayerList(LobbyManager.Instance.GetCurrentPlayers());
    startGameButton?.RefreshVisibility();   // ← add this
  }

  public void RefreshPlayerList(List<Player> players)
  {
    Debug.Log($"RefreshPlayerList called with {players.Count} players");

    foreach (Transform child in playerListContainer)
      Destroy(child.gameObject);

    playerCountText.text = $"Players: {players.Count} / {LobbyManager.MAX_PLAYERS}";

    foreach (var player in players)
    {
      GameObject row = Instantiate(playerRowPrefab, playerListContainer, false);

      var label = row.GetComponent<TextMeshProUGUI>();
      if (label == null) continue;

      string name = player.Data["PlayerName"].Value;
      bool isHost = player.Id == LobbyManager.Instance.CurrentLobby.HostId;
      bool isLocalPlayer = player.Id == AuthenticationService.Instance.PlayerId;

      label.text = $"{name}{(isHost ? "  [HOST]" : "")}{(isLocalPlayer ? "  (you)" : "")}";
    }
  }
}