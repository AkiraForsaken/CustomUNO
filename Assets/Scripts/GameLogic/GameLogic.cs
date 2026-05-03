using UnityEngine;
using Unity.Netcode;
using UnityEngine.SceneManagement;

public class GameLogic : NetworkBehaviour
{
    public static GameLogic Instance { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    /// Called by UI to request starting the game. This will route to the server
    /// to validate that the game can be started and then perform the scene load.
    public void RequestStartGame()
    {
        if (NetworkManager.Singleton == null) return;
        // If this is the host, we can call the server RPC or perform the action directly.
        if (IsHost)
        {
            StartGameServerRpc();
        }
        else
        {
            // Allow non-hosts (if any) to request start, server will validate and ignore if unauthorized.
            StartGameServerRpc();
        }
    }

    [ServerRpc(RequireOwnership = false)]
    private void StartGameServerRpc(ServerRpcParams rpcParams = default)
    {
        // Only host/authority should start the game
        if (!LobbyManager.Instance.CanStartGame()) return;

        // Use Netcode's scene manager to load the Game scene on the server/host — it will propagate to clients.
        NetworkManager.Singleton.SceneManager.LoadScene("Game", LoadSceneMode.Single);
    }
}
