using TMPro;
using UnityEngine;

public class OpponentUI : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI nameLabel;
    [SerializeField] private TextMeshProUGUI cardCountLabel;
    [SerializeField] private Transform       cardContainer; 

    [Header("Card Back Prefab")]
    [SerializeField] private GameObject cardBackPrefab;

    // Đổi sang ulong
    public ulong AssignedPlayerId { get; private set; }

    public void Assign(ulong playerId, string displayName)
    {
        AssignedPlayerId = playerId;
        nameLabel.text   = displayName;
        gameObject.SetActive(true);
    }

    public void Clear()
    {
        AssignedPlayerId = 9999; // Dummy ID
        nameLabel.text   = string.Empty;
        cardCountLabel.text = string.Empty;
        ClearCardBacks();
        gameObject.SetActive(false);
    }

    public void Refresh(int cardCount)
    {
        cardCountLabel.text = $"{cardCount} card{(cardCount == 1 ? "" : "s")}";
        RebuildCardBacks(cardCount);
    }

    private void RebuildCardBacks(int count)
    {
        ClearCardBacks();
        int display = Mathf.Min(count, 7);
        for (int i = 0; i < display; i++)
            Instantiate(cardBackPrefab, cardContainer);
    }

    private void ClearCardBacks()
    {
        foreach (Transform child in cardContainer)
            Destroy(child.gameObject);
    }
}