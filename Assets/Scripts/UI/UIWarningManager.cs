using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UIWarningManager : MonoBehaviour
{
    public static UIWarningManager Instance { get; private set; }

    [Header("Modal References")]
    [SerializeField] private GameObject warningModal;    // the full-screen overlay
    [SerializeField] private TextMeshProUGUI titleText;
    [SerializeField] private TextMeshProUGUI messageText;
    [SerializeField] private Button okButton;

    private Action onConfirm;   // optional callback when OK is pressed

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;

        // Start hidden
        warningModal.SetActive(false);

        // Wire up the OK button once
        okButton.onClick.AddListener(OnOKClicked);
    }

    // Simple warning — just a message, no callback
    public void ShowWarning(string message, string title = "Warning")
    {
        ShowModal(title, message, null);
    }

    // Warning with a callback — fires when player clicks OK
    public void ShowWarning(string message, Action onOKPressed, string title = "Warning")
    {
        ShowModal(title, message, onOKPressed);
    }

    private void ShowModal(string title, string message, Action callback)
    {
        titleText.text = title;
        messageText.text = message;
        onConfirm = callback;
        warningModal.SetActive(true);
    }

    private void OnOKClicked()
    {
        warningModal.SetActive(false);
        onConfirm?.Invoke();   // fire callback if one was set
        onConfirm = null;
    }
}