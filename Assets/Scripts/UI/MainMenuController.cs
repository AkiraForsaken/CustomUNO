using UnityEngine;
using UnityEngine.SceneManagement;
using Unity.Netcode;
using TMPro;
using System.Threading.Tasks;

public class MainMenuController : MonoBehaviour
{
    [Header("Panels")]
    [SerializeField] private GameObject mainMenuPanel;
    [SerializeField] private GameObject joinPanel;
    [SerializeField] private GameObject lobbyPanel;
    [SerializeField] private GameObject settingsPanel;

    [Header("Join Panel")]
    [SerializeField] private TMP_InputField roomCodeInput;
    [SerializeField] private TMP_InputField joinNameInput;

    [Header("Host Panel")]
    [SerializeField] private TMP_InputField hostNameInput;

    // Use UIWarningManager for warnings (centralized)

    [Header("Buttons to lock during async operations")]
    [SerializeField] private UnityEngine.UI.Button hostButton;
    [SerializeField] private UnityEngine.UI.Button connectButton;
    private bool isBusy = false;
    private void Awake()
    {
        if (hostButton == null || connectButton == null)
        {
            Debug.LogWarning("MainMenuController: hostButton or connectButton not assigned in inspector.", this);
        }
    }
    private void SetBusy(bool busy)
    {
        isBusy = busy;
        if (hostButton != null) hostButton.interactable = !busy;
        if (connectButton != null) connectButton.interactable = !busy;
    }
    private async void Start()
    {
        ShowPanel(mainMenuPanel);
        await LobbyManager.Instance.InitializeUnityServices();
    }

    // Validates everything needed before joining
    private bool ValidateJoinInputs(string code)
    {
        if (string.IsNullOrEmpty(code))
        {
            UIWarningManager.Instance.ShowWarning("Please enter a room code.");
            return false;
        }
        if (code.Length != 6) // Unity Lobby codes are 6 characters
        {
            UIWarningManager.Instance.ShowWarning("Room code must be 6 characters.");
            return false;
        }
        return true;
    }

    private string GetOrGenerateName(string rawInput)
    {
        string trimmed = rawInput.Trim();
        if (!string.IsNullOrEmpty(trimmed)) return trimmed;

        string guestId = UnityEngine.Random.Range(1000, 9999).ToString();
        return $"Guest_{guestId}";
    }

    private void OnEnable()
    {
        // Subscribe when this object becomes active
        if (NetworkManager.Singleton != null)
            NetworkManager.Singleton.OnClientDisconnectCallback += OnClientDisconnected;
    }
    private void OnDisable()
    {
        // Always unsubscribe to avoid memory leaks
        if (NetworkManager.Singleton != null)
            NetworkManager.Singleton.OnClientDisconnectCallback -= OnClientDisconnected;
    }
    private void OnClientDisconnected(ulong clientId)
    {
        // On a client, NetworkManager.Singleton.LocalClientId is our own ID
        // This fires for us when the host disconnects and boots everyone
        bool isOurOwnDisconnect = !NetworkManager.Singleton.IsHost;

        if (isOurOwnDisconnect)
        {
            LobbyManager.Instance.CleanUpOnForcedDisconnect();
            ShowPanel(mainMenuPanel);
            UIWarningManager.Instance.ShowWarning("The host has left the room.");
        }
    }

    // --- Main Menu Buttons ---
    public async void OnHostClicked()
    {
        // NetworkManager.Singleton.StartHost();
        // mainMenuPanel.SetActive(false);
        // lobbyPanel.SetActive(true);
        if (isBusy) return;
        SetBusy(true);

        string playerName = hostNameInput.text.Trim();
        if (string.IsNullOrEmpty(playerName)) playerName = "Player";

        bool success = await LobbyManager.Instance.CreateLobby(playerName);
        SetBusy(false);
        if (!success) return;

        ShowPanel(lobbyPanel);
        PlayerListUI.Instance.ShowLobby();
    }
    public void OnJoinClicked()
    {
        // mainMenuPanel.SetActive(false);
        // joinPanel.SetActive(true);
        ShowPanel(joinPanel);
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
    public async void OnConnectClicked()
    {
        // Room code logic will plug in here later via LobbyManager
        // NetworkManager.Singleton.StartClient();
        // joinPanel.SetActive(false);
        // lobbyPanel.SetActive(true);

        if (isBusy) return;

        string code = roomCodeInput.text.Trim().ToUpper();
        string name = GetOrGenerateName(joinNameInput.text);

        if (!ValidateJoinInputs(code)) return;

        SetBusy(true);
        bool success = await LobbyManager.Instance.JoinLobby(code, name);
        SetBusy(false);

        if (!success) {
            UIWarningManager.Instance.ShowWarning("Could not find a room with that code. Please check and try again.");
            return;
        }

        // if (string.IsNullOrEmpty(code)) return;
        // if (string.IsNullOrEmpty(name)) name = "Player";

        ShowPanel(lobbyPanel);
        PlayerListUI.Instance.ShowLobby();
    }

    public void OnJoinBackClicked()
    {
        // joinPanel.SetActive(false);
        // mainMenuPanel.SetActive(true);
        ShowPanel(mainMenuPanel);
    }
    // --- Lobby Panel Buttons ---
    // In MainMenuController.cs
    public void OnLeaveClicked()
    {
        UIWarningManager.Instance.ShowWarning(
            "Are you sure you want to leave the lobby?",
            onOKPressed: () =>
            {
                LobbyManager.Instance.LeaveLobby();
                ShowPanel(mainMenuPanel);
            },
            title: "Leave Lobby"
        );
    }
    private GameObject previousPanel; // tracks who opened settings
    // --- Settings Panel Buttons ---
    public void OnSettingsClicked()
    {
        previousPanel = mainMenuPanel;
        mainMenuPanel.SetActive(false);
        settingsPanel.SetActive(true);
    }
    public void OnSettingsBackClicked()
    {
        // settingsPanel.SetActive(false);
        // if (previousPanel != null)
        //     previousPanel.SetActive(true); // return to wherever we came from
        settingsPanel.SetActive(false);
        if (previousPanel != null) previousPanel.SetActive(true);
    }

    private void ShowPanel(GameObject target)
    {
        mainMenuPanel.SetActive(false);
        joinPanel.SetActive(false);
        lobbyPanel.SetActive(false);
        settingsPanel.SetActive(false);
        target.SetActive(true);
    }
}