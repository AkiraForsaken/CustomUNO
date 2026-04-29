using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using Unity.Netcode;

public class StartGameButton : NetworkBehaviour
{
  [SerializeField] private Button startButton;

  public override void OnNetworkSpawn()
  {
    // Only the host can see and press this button
    startButton.gameObject.SetActive(IsHost);
  }

  public void OnStartGameClicked()
  {
    if (!IsHost) return;

    if (NetworkManager.Singleton.ConnectedClients.Count < 2)
    {
      Debug.LogWarning("Need at least 2 players to start.");
      return;
    }
    StartGameServerRpc();
  }
  [Rpc(SendTo.Server)]
  private void StartGameServerRpc()
  {
    NetworkManager.Singleton.SceneManager.LoadScene(
      "Game", LoadSceneMode.Single
    );
  }
}