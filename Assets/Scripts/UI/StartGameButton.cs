// StartGameButton.cs — full corrected file
using UnityEngine;
using UnityEngine.UI;
using Unity.Netcode;
using UnityEngine.SceneManagement;

public class StartGameButton : MonoBehaviour
{
    [SerializeField] private Button startButton;

    public void RefreshVisibility()
    {
        bool isHost = NetworkManager.Singleton != null && NetworkManager.Singleton.IsHost;
        startButton.gameObject.SetActive(isHost);
    }

    public void OnStartGameClicked()
    {
        if (NetworkManager.Singleton == null || !NetworkManager.Singleton.IsHost) return;

        if (!LobbyManager.Instance.CanStartGame())
        {
            int current = LobbyManager.Instance.GetCurrentPlayers().Count;
            int needed = LobbyManager.MIN_PLAYERS - current;
            UIWarningManager.Instance.ShowWarning(
                $"Need {needed} more player{(needed > 1 ? "s" : "")} to start."
            );
            return;
        }

        // Host loads the scene — NGO propagates it to all clients
        NetworkManager.Singleton.SceneManager.LoadScene("Game", LoadSceneMode.Single);
    }
}