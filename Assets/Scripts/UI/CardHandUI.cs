using System.Collections.Generic;
using Unity.Netcode; 
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class CardHandUI : MonoBehaviour
{
    [Header("Prefab & Container")]
    [SerializeField] private GameObject      cardFrontPrefab;
    [SerializeField] private Transform       handContainer;

    // ── FIX D: add this field, then drag the "You: X card" TMP label ──
    // into it in the Inspector on the LocalPlayerArea object.
    [Header("UI Labels")]
    [SerializeField] private TextMeshProUGUI cardCountLabel;

    private List<CardInstance> currentHand     = new();
    private GameState          currentGameState;
    private bool               isLocalPlayerTurn;

    private readonly List<GameObject> cardObjects = new();

    private void OnEnable()
    {
        GameEvents.OnLocalHandUpdated += OnHandUpdated;
        GameEvents.OnGameStateUpdated += OnGameStateUpdated;
    }

    private void OnDisable()
    {
        GameEvents.OnLocalHandUpdated -= OnHandUpdated;
        GameEvents.OnGameStateUpdated -= OnGameStateUpdated;
    }

    private void OnHandUpdated(List<CardInstance> hand)
    {
        currentHand = hand;
        RebuildCards();
    }

    private void OnGameStateUpdated(GameState state)
    {
        currentGameState  = state;
        isLocalPlayerTurn = IsMyTurn(state);
        RefreshPlayability();
    }

    private void RebuildCards()
    {
        foreach (var go in cardObjects)
            if (go != null) Destroy(go);
        cardObjects.Clear();

        // ── FIX D: update the count label whenever the hand changes ──
        if (cardCountLabel != null)
            cardCountLabel.text = $"You:  {currentHand.Count} card{(currentHand.Count == 1 ? "" : "s")}";

        for (int i = 0; i < currentHand.Count; i++)
        {
            int          index = i;
            CardInstance card  = currentHand[i];

            GameObject go = Instantiate(cardFrontPrefab, handContainer);
            cardObjects.Add(go);

            var cf = go.GetComponent<CardFront>();
            if (cf != null)
            {
                cf.Setup(card);
                bool playable = isLocalPlayerTurn
                                && CardValidator.IsLegal(card, currentGameState, currentHand.Count);
                cf.SetPlayable(playable);
            }

            var btn = go.GetComponent<Button>();
            if (btn != null)
                btn.onClick.AddListener(() => OnCardClicked(index));
        }
    }

    private void RefreshPlayability()
    {
        for (int i = 0; i < cardObjects.Count; i++)
        {
            if (i >= currentHand.Count) continue;
            if (cardObjects[i] == null) continue;
            var cf = cardObjects[i].GetComponent<CardFront>();
            if (cf == null) continue;

            bool playable = isLocalPlayerTurn
                            && CardValidator.IsLegal(currentHand[i], currentGameState, currentHand.Count);
            cf.SetPlayable(playable);
        }
    }

    private void OnCardClicked(int index)
    {
        if (!isLocalPlayerTurn) return;
        if (index < 0 || index >= currentHand.Count) return;

        SetAllCardsInteractable(false);
        GameEvents.RaisePlayCardRequested(currentHand[index]);
    }

    private void SetAllCardsInteractable(bool interactable)
    {
        foreach (var go in cardObjects)
        {
            if (go == null) continue;
            go.GetComponent<CardFront>()?.SetPlayable(interactable);
        }
    }

    private bool IsMyTurn(GameState state)
    {
        if (state.playerCount == 0) return false;
        if (state.currentPlayerIndex < 0 || state.currentPlayerIndex >= state.playerCount)
            return false;

        return state.playerOrder[state.currentPlayerIndex]
               == NetworkManager.Singleton.LocalClientId;
    }
}