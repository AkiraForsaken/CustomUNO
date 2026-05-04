using System.Collections.Generic;
using Unity.Netcode; 
using Unity.Services.Authentication;
using UnityEngine;
using UnityEngine.UI;

// Attach to HandContainer inside LocalPlayerArea.
// Does NOT reference NetworkGameManager directly — communicates
// through GameEvents so it compiles before the network layer exists.
public class CardHandUI : MonoBehaviour
{
    [Header("Prefab & Container")]
    [SerializeField] private GameObject cardFrontPrefab;
    [SerializeField] private Transform  handContainer;

    private List<CardInstance> currentHand      = new();
    private GameState          currentGameState;
    private bool               isLocalPlayerTurn;

    private readonly List<GameObject> cardObjects = new();

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
                // Truyền thêm currentHand.Count vào đây
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
            if (cardObjects[i] == null) continue;
            var cf = cardObjects[i].GetComponent<CardFront>();
            if (cf == null) continue;

            // Truyền thêm currentHand.Count vào đây
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
        // Kiểm tra struct bằng playerCount thay vì Count hoặc null
        if (state.playerCount == 0) return false;
        if (state.currentPlayerIndex < 0 || state.currentPlayerIndex >= state.playerCount)
            return false;

        // Dùng ID mạng (ulong) thay cho Auth ID (string)
        return state.playerOrder[state.currentPlayerIndex] == NetworkManager.Singleton.LocalClientId;
    }
}