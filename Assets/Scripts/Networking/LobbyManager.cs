using System.Collections.Generic;
using System.Threading.Tasks;
using Unity.Netcode;
using Unity.Services.Authentication;
using Unity.Services.Core;
using Unity.Services.Lobbies;
using Unity.Services.Lobbies.Models;
using UnityEngine;

public class LobbyManager : MonoBehaviour
{
  public static LobbyManager Instance { get; private set; }
  public const int MAX_PLAYERS = 4;
  public const int MIN_PLAYERS = 1;

  // The lobby data key for the relay join code
  private const string KEY_RELAY_JOIN_CODE = "RelayJoinCode";
  // The lobby data key for the player's display name
  private const string KEY_PLAYER_NAME = "PlayerName";

  private Lobby currentLobby;
  private float heartbeatTimer; // keeps host lobby alive
  private float lobbyPollTimer; // clients poll for updates

  public Lobby CurrentLobby => currentLobby;

  private void Awake()
  {
    if (Instance != null && Instance != this) { Destroy(gameObject); return; }
    Instance = this;
  }

  // Call this once at app start before anything else
  public async Task InitializeUnityServices()
  {
    if (UnityServices.State == ServicesInitializationState.Initialized) return;

    await UnityServices.InitializeAsync();
    await AuthenticationService.Instance.SignInAnonymouslyAsync();
    Debug.Log($"Signed in as: {AuthenticationService.Instance.PlayerId}");
  }

  // ───── HOST ─────
  public async Task<bool> CreateLobby(string playerName)
  {
    try
    {
      // 1. Create a relay and get its join code
      string relayJoinCode = await RelayManager.Instance.CreateRelay(MAX_PLAYERS);
      if (relayJoinCode == null) return false;

      // 2. Create the Unity Lobby, embedding the relay code in lobby data
      var options = new CreateLobbyOptions
      {
        IsPrivate = false,
        Data = new Dictionary<string, DataObject>
        {
          {
            KEY_RELAY_JOIN_CODE,
            new DataObject(DataObject.VisibilityOptions.Member, relayJoinCode)
          }
        },
        Player = CreatePlayerData(playerName)
      };

      currentLobby = await LobbyService.Instance.CreateLobbyAsync(
        "UNO Room", MAX_PLAYERS, options
      );

      // 3. Start as host AFTER relay is configured
      NetworkManager.Singleton.StartHost();

      Debug.Log($"Lobby created. Room code: {currentLobby.LobbyCode}");
      return true;
    }
    catch (LobbyServiceException e)
    {
      Debug.LogError($"Lobby creation failed: {e}");
      return false;
    }
  }

  // ───── CLIENT ─────
  public async Task<bool> JoinLobby(string lobbyCode, string playerName)
  {
    try
    {
      // 1. Join the Unity Lobby by room code
      var options = new JoinLobbyByCodeOptions
      {
        Player = CreatePlayerData(playerName)
      };

      currentLobby = await LobbyService.Instance.JoinLobbyByCodeAsync(
        lobbyCode.ToUpper(), options
      );

      // 2. Extract the relay join code from lobby data
      string relayJoinCode = currentLobby.Data[KEY_RELAY_JOIN_CODE].Value;

      // 3. Join the relay, then start as client
      await RelayManager.Instance.JoinRelay(relayJoinCode);
      NetworkManager.Singleton.StartClient();

      Debug.Log($"Joined lobby: {currentLobby.LobbyCode}");
      return true;
    }
    catch (LobbyServiceException e)
    {
        Debug.LogError($"Lobby join failed: {e}");
        return false;
    }
  }

  // ───── LOBBY UPKEEP ─────
  private void Update()
  {
    HandleHeartbeat();
    HandleLobbyPoll();
  }

  // Unity Lobbies expire after 30s of inactivity — host must ping to keep it alive
  private async void HandleHeartbeat()
  {
    if (currentLobby == null || !IsHost()) return;

    heartbeatTimer -= Time.deltaTime;
    if (heartbeatTimer > 0) return;

    heartbeatTimer = 20f;
    await LobbyService.Instance.SendHeartbeatPingAsync(currentLobby.Id);
  }

  // Host / Clients poll the lobby every 2s to get the updated player list
  private async void HandleLobbyPoll()
  {
    if (currentLobby == null) return; // both host and client poll now

    lobbyPollTimer -= Time.deltaTime;
    if (lobbyPollTimer > 0) return;

    lobbyPollTimer = 2f;
    currentLobby = await LobbyService.Instance.GetLobbyAsync(currentLobby.Id);
    PlayerListUI.Instance?.RefreshPlayerList(currentLobby.Players);
  }

  // ───── LEAVING ─────
  public async void LeaveLobby()
  {
    if (currentLobby == null) return;
    try
    {
      if (IsHost())
        await LobbyService.Instance.DeleteLobbyAsync(currentLobby.Id);
      else
        await LobbyService.Instance.RemovePlayerAsync(
          currentLobby.Id, AuthenticationService.Instance.PlayerId
        );

      currentLobby = null;
      NetworkManager.Singleton.Shutdown();
    }
    catch (LobbyServiceException e)
    {
      Debug.LogError($"Leave lobby failed: {e}");
    }
  }

  // ───── HELPERS ─────

  private bool IsHost() =>
    currentLobby != null &&
    currentLobby.HostId == AuthenticationService.Instance.PlayerId;

  private Player CreatePlayerData(string playerName) => new Player
  {
    Data = new Dictionary<string, PlayerDataObject>
    {
      {
        KEY_PLAYER_NAME,
        new PlayerDataObject(PlayerDataObject.VisibilityOptions.Member, playerName)
      }
    }
  };

  public List<Player> GetCurrentPlayers() => currentLobby?.Players ?? new List<Player>();

  public string GetLobbyCode() => currentLobby?.LobbyCode ?? "";

  public bool CanStartGame() =>
    IsHost() && currentLobby != null && currentLobby.Players.Count >= MIN_PLAYERS;

  public void CleanUpOnForcedDisconnect()
  {
    currentLobby = null;
    // NetworkManager is already shutting down, don't call Shutdown() again
  }
}