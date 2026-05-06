using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

public class UnoButtonUI : MonoBehaviour
{
    [SerializeField] private Button          unoButton;
    [SerializeField] private TextMeshProUGUI unoLabel;

    // Assign these two colours in the Inspector
    [SerializeField] private Color activeColor   = Color.red;
    [SerializeField] private Color inactiveColor = new Color(0.4f, 0.4f, 0.4f, 1f);

    private void OnEnable()  => GameEvents.OnGameStateUpdated += Refresh;
    private void OnDisable() => GameEvents.OnGameStateUpdated -= Refresh;

    private void Start()
    {
        unoButton.onClick.AddListener(OnUnoPressed);
        SetInactive();
    }

    private void Refresh(GameState state)
    {
        if (state.unoVulnerableId == 0) { SetInactive(); return; }

        ulong localId = NetworkManager.Singleton.LocalClientId;

        if (state.unoVulnerableId == localId)
        {
            // Local player forgot to call UNO
            unoLabel.text = "UNO!";
            SetActive();
        }
        else
        {
            // Someone else is vulnerable — you can catch them
            unoLabel.text = "CATCH!";
            SetActive();
        }
    }

    private void OnUnoPressed()
    {
        GameEvents.RaiseUnoCalled();
        SetInactive(); // Optimistically disable to prevent double-press
    }

    private void SetActive()
    {
        unoButton.interactable     = true;
        unoButton.image.color      = activeColor;
        unoLabel.color             = Color.white;
    }

    private void SetInactive()
    {
        unoButton.interactable     = false;
        unoButton.image.color      = inactiveColor;
        if (unoLabel != null) unoLabel.text = "UNO";
        unoLabel.color             = Color.grey;
    }
}