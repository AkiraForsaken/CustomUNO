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
    [SerializeField] private Button cancelButton;

    private Action onConfirm;   // optional callback when OK is pressed
    private Action onCancel;    // optional callback when Cancel is pressed

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;

        // Start hidden
        if (warningModal != null) warningModal.SetActive(false);

        // Wire up the OK/Cancel buttons once (if present)
        if (okButton != null) okButton.onClick.AddListener(OnOKClicked);
        if (cancelButton != null) cancelButton.onClick.AddListener(OnCancelClicked);
    }

    // Simple warning — just a message, no callback
    public void ShowWarning(string message, string title = "Warning")
    {
        ShowModal(title, message, null);
    }

    // Warning with a callback — fires when player clicks OK
    public void ShowWarning(string message, Action onOKPressed, string title = "Warning")
    {
        ShowModal(title, message, onOKPressed, null);
    }

    // Warning with OK and Cancel callbacks — shows Cancel button
    public void ShowWarning(string message, Action onOKPressed, Action onCancelPressed, string title = "Warning")
    {
        ShowModal(title, message, onOKPressed, onCancelPressed);
    }

    private void ShowModal(string title, string message, Action callback, Action cancelCallback = null)
    {
        if (titleText != null) titleText.text = title;
        if (messageText != null) messageText.text = message;
        onConfirm = callback;
        onCancel = cancelCallback;

        if (cancelButton != null)
            cancelButton.gameObject.SetActive(cancelCallback != null);

        if (warningModal != null) warningModal.SetActive(true);
    }

    private void OnOKClicked()
    {
        if (warningModal != null) warningModal.SetActive(false);
        onConfirm?.Invoke();   // fire callback if one was set
        onConfirm = null;
        onCancel = null;
    }

    private void OnCancelClicked()
    {
        if (warningModal != null) warningModal.SetActive(false);
        onCancel?.Invoke();
        onConfirm = null;
        onCancel = null;
    }
}