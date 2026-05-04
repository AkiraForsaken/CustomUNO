using System;
using UnityEngine;

// Plain serializable class — no MonoBehaviour.
// Holds the on/off state for every custom house rule.
// HouseRulesManager persists this across the lobby → game scene transition.
[Serializable]
public class HouseRulesConfig
{
    [Tooltip("When a Reverse card is played, all players pass their " +
             "entire hand to the next player in the direction of the reversal.")]
    public bool ruleZeroEnabled = false;

    [Tooltip("When a 7 is played, the player who played it must swap " +
             "their hand with any chosen opponent.")]
    public bool ruleSevenEnabled = false;

    [Tooltip("When an 8 is played, all players must react within a " +
             "time limit. The last player to react draws penalty cards.")]
    public bool ruleEightEnabled = false;
}