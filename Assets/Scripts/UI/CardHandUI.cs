using System.Collections.Generic;
using Unity.Services.Authentication;
using UnityEngine;
using UnityEngine.UI;

// Attach to HandContainer inside LocalPlayerArea.
// Does NOT reference NetworkGameManager directly — communicates
// through GameEvents so it compiles before the network layer exists.
public class CardHandUI : MonoBehaviour
{
    // ── Inspector ────────────────────────────────────────────────
    [Header("Prefab & Container")]
    [SerializeField] private GameObject cardFrontPrefab;
    [SerializeField] private Transform  handContainer;

    // ── Private state ─────────────────────────────────────────────
    private List<CardInstance> currentHand      = new();
    private GameState          currentGameState;
    private bool               isLocalPlayerTurn;

    private readonly List<GameObject> cardObjects = new();

    // ── Lifecycle ─────────────────────────────────────────────────
    private void OnEnable()
    {
        GameEvents.OnLocalHandUpdated  += OnHandUpdated;
        GameEvents.OnGameStateUpdated  += OnGameStateUpdated;
    }

    private void OnDisable()
    {
        GameEvents.OnLocalHandUpdated  -= OnHandUpdated;
        GameEvents.OnGameStateUpdated  -= OnGameStateUpdated;
    }

    // ── Event handlers ────────────────────────────────────────────

    // Called when our private hand changes (drew or played a card)
    private void OnHandUpdated(List<CardInstance> hand)
    {
        currentHand = hand;
        RebuildCards();
    }

    // Called when shared game state changes (another player's turn, new top card, etc.)
    private void OnGameStateUpdated(GameState state)
    {
        currentGameState  = state;
        isLocalPlayerTurn = IsMyTurn(state);
        RefreshPlayability();
    }

    // ── Private helpers ───────────────────────────────────────────

    private void RebuildCards()
    {
        foreach (var go in cardObjects)
            if (go != null) Destroy(go);
        cardObjects.Clear();

        for (int i = 0; i < currentHand.Count; i++)
        {
            int          index = i;          // closure capture
            CardInstance card  = currentHand[i];

            GameObject go = Instantiate(cardFrontPrefab, handContainer);
            cardObjects.Add(go);

            var cf = go.GetComponent<CardFront>();
            if (cf != null)
            {
                cf.Setup(card);
                bool playable = isLocalPlayerTurn
                                && CardValidator.IsLegal(card, currentGameState);
                cf.SetPlayable(playable);
            }

            var btn = go.GetComponent<Button>();
            if (btn != null)
                btn.onClick.AddListener(() => OnCardClicked(index));
        }
    }

    // Lightweight pass — no instantiation, just toggle interactability
    private void RefreshPlayability()
    {
        for (int i = 0; i < cardObjects.Count; i++)
        {
            if (cardObjects[i] == null) continue;
            var cf = cardObjects[i].GetComponent<CardFront>();
            if (cf == null) continue;

            bool playable = isLocalPlayerTurn
                            && CardValidator.IsLegal(currentHand[i], currentGameState);
            cf.SetPlayable(playable);
        }
    }

    private void OnCardClicked(int index)
    {
        if (!isLocalPlayerTurn) return;
        if (index < 0 || index >= currentHand.Count) return;

        // Lock all cards immediately (optimistic UI) before round-trip
        SetAllCardsInteractable(false);

        // Fire event — NetworkGameManager listens and sends the ServerRpc
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
        if (state?.playerOrder == null || state.playerOrder.Count == 0) return false;
        if (state.currentPlayerIndex < 0 || state.currentPlayerIndex >= state.playerOrder.Count)
            return false;

        return state.playerOrder[state.currentPlayerIndex]
               == AuthenticationService.Instance.PlayerId;
    }
}