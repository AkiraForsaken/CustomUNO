using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

// Attach to the HouseRulesPanel GameObject inside the lobby panel.
// The panel is visible to all players but only the HOST can toggle rules —
// clients see the current state but their toggles are non-interactable.
public class HouseRulesPanel : MonoBehaviour
{
    [Header("Rule 0")]
    [SerializeField] private Toggle          ruleZeroToggle;
    [SerializeField] private Button          ruleZeroInfoButton;
    [SerializeField] private TextMeshProUGUI ruleZeroLabel;

    [Header("Rule 7")]
    [SerializeField] private Toggle          ruleSevenToggle;
    [SerializeField] private Button          ruleSevenInfoButton;
    [SerializeField] private TextMeshProUGUI ruleSevenLabel;

    [Header("Rule 8")]
    [SerializeField] private Toggle          ruleEightToggle;
    [SerializeField] private Button          ruleEightInfoButton;
    [SerializeField] private TextMeshProUGUI ruleEightLabel;

    [Header("Host-only notice")]
    [SerializeField] private TextMeshProUGUI hostOnlyNote;

    // Rule descriptions shown in the info modal
    private const string DESC_ZERO  =
        "\nWhen a 0 is played, all players pass their entire hand to the next player in the current play direction.";

    private const string DESC_SEVEN =
        "\nWhen a 7 is played, the player who played it must choose an opponent to swap hands with.";

    private const string DESC_EIGHT =
        "\nWhen an 8 is played, all players must tap the \"Reaction\" button before time runs out. The last player to react draws 2 penalty cards.";

    private void Start()
    {
        // Wire info buttons (defensive: only wire if assigned)
        if (ruleZeroInfoButton != null)
            ruleZeroInfoButton.onClick.AddListener(() => UIWarningManager.Instance.ShowWarning(DESC_ZERO,  title: "Rule of 0"));
        else Debug.LogWarning("HouseRulesPanel: ruleZeroInfoButton not assigned in Inspector.", this);

        if (ruleSevenInfoButton != null)
            ruleSevenInfoButton.onClick.AddListener(() => UIWarningManager.Instance.ShowWarning(DESC_SEVEN, title: "Rule of 7"));
        else Debug.LogWarning("HouseRulesPanel: ruleSevenInfoButton not assigned in Inspector.", this);

        if (ruleEightInfoButton != null)
            ruleEightInfoButton.onClick.AddListener(() => UIWarningManager.Instance.ShowWarning(DESC_EIGHT, title: "Rule of 8"));
        else Debug.LogWarning("HouseRulesPanel: ruleEightInfoButton not assigned in Inspector.", this);

        // Wire toggles — only meaningful when called by host
        if (ruleZeroToggle != null)
            ruleZeroToggle.onValueChanged.AddListener(OnRuleZeroChanged);
        else Debug.LogWarning("HouseRulesPanel: ruleZeroToggle not assigned in Inspector.", this);

        if (ruleSevenToggle != null)
            ruleSevenToggle.onValueChanged.AddListener(OnRuleSevenChanged);
        else Debug.LogWarning("HouseRulesPanel: ruleSevenToggle not assigned in Inspector.", this);

        if (ruleEightToggle != null)
            ruleEightToggle.onValueChanged.AddListener(OnRuleEightChanged);
        else Debug.LogWarning("HouseRulesPanel: ruleEightToggle not assigned in Inspector.", this);

        RefreshInteractability();
    }

    // Call this whenever the lobby panel is shown (ShowLobby in PlayerListUI)
    // so the panel reflects the current config and correct interactability.
    public void Refresh()
    {
        if (HouseRulesManager.Instance == null) return;

        // Read from lobby data so clients see host's settings
        ApplyLobbyRulesToConfig();

        var cfg = HouseRulesManager.Instance.Config;
        ruleZeroToggle?.SetIsOnWithoutNotify(cfg.ruleZeroEnabled);
        ruleSevenToggle?.SetIsOnWithoutNotify(cfg.ruleSevenEnabled);
        ruleEightToggle?.SetIsOnWithoutNotify(cfg.ruleEightEnabled);

        RefreshInteractability();
    }

    private void ApplyLobbyRulesToConfig()
    {
        var data = LobbyManager.Instance?.CurrentLobby?.Data;
        if (data == null || !data.TryGetValue("HouseRules", out var entry)) return;

        var parts = entry.Value.Split('|');
        if (parts.Length < 3) return;

        HouseRulesManager.Instance.ApplyConfig(new HouseRulesConfig
        {
            ruleZeroEnabled  = parts[0] == "1",
            ruleSevenEnabled = parts[1] == "1",
            ruleEightEnabled = parts[2] == "1"
        });
    }

    // Host can toggle; clients see the state but cannot change it
    private void RefreshInteractability()
    {
        bool isHost = IsHost();

        ruleZeroToggle.interactable  = isHost;
        ruleSevenToggle.interactable = isHost;
        ruleEightToggle.interactable = isHost;

        hostOnlyNote.gameObject.SetActive(!isHost);
    }

    // ── Toggle callbacks (host only) ──────────────────────────────────────────
    private void PushRulesToLobby()
    {
        if (!IsHost() || HouseRulesManager.Instance == null) return;
        var cfg = HouseRulesManager.Instance.Config;
        // Encode all 3 rules as a single "0/0/0" string to avoid 3 separate lobby update calls
        string encoded = $"{(cfg.ruleZeroEnabled  ? 1 : 0)}|" +
                        $"{(cfg.ruleSevenEnabled ? 1 : 0)}|" +
                        $"{(cfg.ruleEightEnabled ? 1 : 0)}";
        LobbyManager.Instance.UpdateLobbyDataAsync("HouseRules", encoded);
    }

    private void OnRuleZeroChanged(bool value)
    {
        if (!IsHost()) return;
        if (HouseRulesManager.Instance == null)
        {
            Debug.LogWarning("Attempted to set rule zero but HouseRulesManager.Instance is null.", this);
            return;
        }
        HouseRulesManager.Instance.SetRuleZero(value);
        PushRulesToLobby();
    }

    private void OnRuleSevenChanged(bool value)
    {
        if (!IsHost()) return;
        if (HouseRulesManager.Instance == null)
        {
            Debug.LogWarning("Attempted to set rule seven but HouseRulesManager.Instance is null.", this);
            return;
        }
        HouseRulesManager.Instance.SetRuleSeven(value);
        PushRulesToLobby();
    }

    private void OnRuleEightChanged(bool value)
    {
        if (!IsHost()) return;
        if (HouseRulesManager.Instance == null)
        {
            Debug.LogWarning("Attempted to set rule eight but HouseRulesManager.Instance is null.", this);
            return;
        }
        HouseRulesManager.Instance.SetRuleEight(value);
        PushRulesToLobby();
    }

    private bool IsHost() =>
        NetworkManager.Singleton != null && NetworkManager.Singleton.IsHost;
}