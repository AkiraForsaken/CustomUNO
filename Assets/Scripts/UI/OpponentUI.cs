using TMPro;
using UnityEngine;

// Attach one of these to each of the three opponent panels:
// OpponentArea_Top, OpponentArea_Left, OpponentArea_Right.
// GameUI assigns a lobby player ID to each slot at game start,
// then calls Refresh() whenever GameState updates.
public class OpponentUI : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI nameLabel;
    [SerializeField] private TextMeshProUGUI cardCountLabel;
    [SerializeField] private Transform       cardContainer; // holds card back prefabs

    [Header("Card Back Prefab")]
    [SerializeField] private GameObject cardBackPrefab;

    // The Unity Auth player ID assigned to this slot. Empty = slot unused.
    public string AssignedPlayerId { get; private set; }

    // Assign a player to this slot and show the panel
    public void Assign(string playerId, string displayName)
    {
        AssignedPlayerId = playerId;
        nameLabel.text   = displayName;
        gameObject.SetActive(true);
    }

    // Hide and clear — called when fewer than 4 players are in the game
    public void Clear()
    {
        AssignedPlayerId = string.Empty;
        nameLabel.text   = string.Empty;
        cardCountLabel.text = string.Empty;
        ClearCardBacks();
        gameObject.SetActive(false);
    }

    // Refresh card count display from the shared GameState
    // handCounts: Dictionary<playerId, handSize> broadcast in GameState
    public void Refresh(int cardCount)
    {
        cardCountLabel.text = $"{cardCount} card{(cardCount == 1 ? "" : "s")}";
        RebuildCardBacks(cardCount);
    }

    // Rebuild the visual card-back row to match actual hand size
    private void RebuildCardBacks(int count)
    {
        ClearCardBacks();

        // Cap the visual display so very large hands don't overflow the panel
        int display = Mathf.Min(count, 10);
        for (int i = 0; i < display; i++)
            Instantiate(cardBackPrefab, cardContainer);
    }

    private void ClearCardBacks()
    {
        foreach (Transform child in cardContainer)
            Destroy(child.gameObject);
    }
}