using UnityEngine;

// Attach to a DontDestroyOnLoad GameObject (same one as LobbyManager / RelayManager).
// Survives the MainMenu → Game scene transition so NetworkGameManager can read
// the host's chosen rules on game start.
//
// Only the HOST can change rules (via the lobby settings panel).
// NetworkGameManager broadcasts the final config to all clients at game start
// via InitializeGameClientRpc — GameEvents carries it to the UI.
public class HouseRulesManager : MonoBehaviour
{
    public static HouseRulesManager Instance { get; private set; }

    // The active config — host writes this, everyone reads it
    public HouseRulesConfig Config { get; private set; } = new();

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    // Called by HouseRulesPanel when the host toggles a rule
    public void SetRuleZero(bool enabled)  => Config.ruleZeroEnabled  = enabled;
    public void SetRuleSeven(bool enabled) => Config.ruleSevenEnabled = enabled;
    public void SetRuleEight(bool enabled) => Config.ruleEightEnabled = enabled;

    // Called by NetworkGameManager to apply the config received from the host
    public void ApplyConfig(HouseRulesConfig config) => Config = config;
}