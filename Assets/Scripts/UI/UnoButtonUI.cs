using System.Collections.Generic;
using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

public class UnoButtonUI : MonoBehaviour
{
    [SerializeField] private Button          unoButton;
    [SerializeField] private TextMeshProUGUI unoLabel;

    [SerializeField] private Color activeColor   = Color.red;
    [SerializeField] private Color inactiveColor = new Color(0.4f, 0.4f, 0.4f, 1f);

    private GameState currentState;
    private int       localHandCount = 0;

    private void OnEnable()
    {
        GameEvents.OnGameStateUpdated += OnGameStateUpdated;
        GameEvents.OnLocalHandUpdated += OnLocalHandUpdated;
    }

    private void OnDisable()
    {
        GameEvents.OnGameStateUpdated -= OnGameStateUpdated;
        GameEvents.OnLocalHandUpdated -= OnLocalHandUpdated;
    }

    private void Start()
    {
        unoButton.onClick.AddListener(OnUnoPressed);
        SetInactive();
    }

    private void OnLocalHandUpdated(List<CardInstance> hand)
    {
        localHandCount = hand.Count;
        Evaluate();
    }

    private void OnGameStateUpdated(GameState state)
    {
        currentState = state;
        Evaluate();
    }

    private void Evaluate()
    {
        ulong localId = NetworkManager.Singleton.LocalClientId;

        // ── Priority 1: someone is already vulnerable (post-play window) ──
        if (currentState.unoVulnerableId != 0)
        {
            if (currentState.unoVulnerableId == localId)
                SetActive("UNO!");   // you forgot to call it
            else
                SetActive("CATCH!"); // punish the other player
            return;
        }

        // ── Priority 2: it's your turn and you have 2 cards (pre-play prompt) ──
        bool isMyTurn = currentState.playerCount > 0
            && currentState.playerOrder[currentState.currentPlayerIndex] == localId;

        if (isMyTurn && localHandCount == 2)
        {
            SetActive("UNO!");
            return;
        }

        SetInactive();
    }

    private void OnUnoPressed()
    {
        GameEvents.RaiseUnoCalled();
        SetInactive(); // optimistically disable to prevent double-press
    }

    private void SetActive(string label)
    {
        unoLabel.text              = label;
        unoButton.interactable     = true;
        unoButton.image.color      = activeColor;
        unoLabel.color             = Color.white;
    }

    private void SetInactive()
    {
        unoLabel.text              = "UNO";
        unoButton.interactable     = false;
        unoButton.image.color      = inactiveColor;
        unoLabel.color             = Color.grey;
    }
}