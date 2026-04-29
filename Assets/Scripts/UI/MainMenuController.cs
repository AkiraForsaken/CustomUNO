using UnityEngine;
using UnityEngine.SceneManagement;
using Unity.Netcode;

public class MainMenuManager : MonoBehaviour
{
    [Header("Panels")]
    [SerializeField] private GameObject mainMenuPanel;
    [SerializeField] private GameObject joinPanel;
    [SerializeField] private GameObject lobbyPanel;
    [SerializeField] private GameObject settingsPanel;

    private void Start()
    {
        // Make sure only the main menu is visible on start
        mainMenuPanel.SetActive(true);
        joinPanel.SetActive(false);
        lobbyPanel.SetActive(false);
        settingsPanel.SetActive(false);
    }

    // --- Main Menu Buttons ---
    public void OnHostClicked()
    {
        NetworkManager.Singleton.StartHost();
        mainMenuPanel.SetActive(false);
        lobbyPanel.SetActive(true);
    }

    public void OnJoinClicked()
    {
        mainMenuPanel.SetActive(false);
        joinPanel.SetActive(true);
    }

    public void OnQuitClicked()
    {
        #if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
        #else
            Application.Quit();
        #endif
    }

    // --- Join Panel Buttons ---
    public void OnConnectClicked()
    {
        // Room code logic will plug in here later via LobbyManager
        NetworkManager.Singleton.StartClient();
        joinPanel.SetActive(false);
        lobbyPanel.SetActive(true);
    }

    public void OnJoinBackClicked()
    {
        joinPanel.SetActive(false);
        mainMenuPanel.SetActive(true);
    }

    // --- Lobby Panel Buttons ---
    public void OnLeaveClicked()
    {
        NetworkManager.Singleton.Shutdown();
        lobbyPanel.SetActive(false);
        mainMenuPanel.SetActive(true);
    }

    private GameObject previousPanel; // tracks who opened settings
    // --- Settings Panel Buttons ---
    public void OnSettingsClicked()
    {
        previousPanel = mainMenuPanel; // remember we came from main menu
        mainMenuPanel.SetActive(false);
        settingsPanel.SetActive(true);
    }

    public void OnSettingsBackClicked()
    {
        settingsPanel.SetActive(false);
        if (previousPanel != null)
            previousPanel.SetActive(true); // return to wherever we came from
    }
}