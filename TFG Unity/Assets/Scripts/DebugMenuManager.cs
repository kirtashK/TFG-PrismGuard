using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class DebugMenuManager : MonoBehaviour, IHideElement
{
    [SerializeField] private GameObject panelRoot;

    [Header("Score")]
    [SerializeField] private TMP_InputField scoreAmountInputField;
    [SerializeField] private Button addScoreInputButton;

    [Header("Waves")]
    [SerializeField] private Button toggleWavesButton;
    [SerializeField] private TMP_Text wavesStatusText;
    [SerializeField] private WaveManager waveManager;

    [Header("Global stat")]
    [SerializeField] private List<StatKey> availableStatKeys = new();
    [SerializeField] private TMP_Dropdown statKeyDropdown;
    [SerializeField] private TMP_InputField statAmountInputField;
    [SerializeField] private Button addStatInputButton;

    [Header("Feedback")]
    [SerializeField] private TMP_Text statusText;

    private InputAction toggleMenuAction;
    private InputAction pointerAction;
    private InputAction clickAction;
    private InputAction cancelAction;

    private void Awake()
    {
        if (panelRoot != null)
        {
            panelRoot.SetActive(false);
        }

        toggleMenuAction = new InputAction("ToggleDebugMenu", InputActionType.Button, "<Keyboard>/k");
        pointerAction = new InputAction("Pointer", InputActionType.Value, "<Pointer>/position");
        clickAction = new InputAction("LeftClick", InputActionType.Button, "<Mouse>/leftButton");
        cancelAction = new InputAction("CancelDebugMenu", InputActionType.Button);
        cancelAction.AddBinding("<Keyboard>/escape");
        cancelAction.AddBinding("<Mouse>/rightButton");

        toggleMenuAction.Enable();
        pointerAction.Enable();
        clickAction.Enable();
        cancelAction.Enable();

        if (waveManager == null)
        {
            Debug.LogWarning($"{name}: missing {nameof(waveManager)}");
        }
        if (wavesStatusText == null)
        {
            Debug.LogWarning($"{name}: missing {nameof(wavesStatusText)}");
        }
    }

    private void OnEnable()
    {
        StartCoroutine(RegisterWhenReady());
        AddButtonListeners();
        InitializeStatDropdown();
    }

    private IEnumerator RegisterWhenReady()
    {
        while (HideElementManager.Instance == null)
        {
            yield return null;
        }

        HideElementManager.Instance.Register(this);
        RefreshWaveState();
    }

    private void OnDisable()
    {
        if (HideElementManager.Instance != null)
        {
            HideElementManager.Instance.Unregister(this);
        }

        RemoveButtonListeners();
        RemoveStatDropdownListener();
    }

    private void OnDestroy()
    {
        toggleMenuAction?.Dispose();
        pointerAction?.Dispose();
        clickAction?.Dispose();
        cancelAction?.Dispose();
    }

    private void Start()
    {
        RefreshWaveState();
        SetStatus(string.Empty);
    }

    private void Update()
    {
        if (toggleMenuAction != null && toggleMenuAction.triggered)
        {
            TogglePanel();
            return;
        }
        if (panelRoot == null || !panelRoot.activeSelf)
        {
            return;
        }
        if (cancelAction != null && cancelAction.triggered)
        {
            HidePanel();
            return;
        }

        if (clickAction != null && clickAction.triggered)
        {
            RectTransform rect = panelRoot.GetComponent<RectTransform>();
            Vector2 pointerPosition = pointerAction.ReadValue<Vector2>();

            Canvas canvas = panelRoot.GetComponentInParent<Canvas>();
            Camera uiCamera = null;
            if (canvas != null && canvas.renderMode == RenderMode.ScreenSpaceCamera)
            {
                uiCamera = canvas.worldCamera;
            }

            bool clickedInside = RectTransformUtility.RectangleContainsScreenPoint(rect, pointerPosition, uiCamera);
            if (!clickedInside)
            {
                HidePanel();
            }
        }
    }

    public void TogglePanel()
    {
        if (panelRoot == null)
        {
            Debug.LogError($"{name}: missing {nameof(panelRoot)}");
            return;
        }

        if (!panelRoot.activeSelf)
        {
            HideElementManager.Instance.ShowOnly(this);
        }

        panelRoot.SetActive(!panelRoot.activeSelf);
        RefreshWaveState();
    }

    public void HidePanel()
    {
        if (panelRoot != null)
        {
            panelRoot.SetActive(false);
        }
    }

    private void AddButtonListeners()
    {
        if (addScoreInputButton != null)
        {
            addScoreInputButton.onClick.RemoveListener(AddScoreFromInput);
            addScoreInputButton.onClick.AddListener(AddScoreFromInput);
        }
        if (toggleWavesButton != null)
        {
            toggleWavesButton.onClick.RemoveListener(ToggleWaves);
            toggleWavesButton.onClick.AddListener(ToggleWaves);
        }
        if (addStatInputButton != null)
        {
            addStatInputButton.onClick.RemoveListener(AddGlobalStatFromInput);
            addStatInputButton.onClick.AddListener(AddGlobalStatFromInput);
        }
    }

    private void RemoveButtonListeners()
    {
        if (addScoreInputButton != null)
        {
            addScoreInputButton.onClick.RemoveListener(AddScoreFromInput);
        }
        if (toggleWavesButton != null)
        {
            toggleWavesButton.onClick.RemoveListener(ToggleWaves);
        }
        if (addStatInputButton != null)
        {
            addStatInputButton.onClick.RemoveListener(AddGlobalStatFromInput);
        }
    }

    private void InitializeStatDropdown()
    {
        if (statKeyDropdown == null)
        {
            return;
        }

        RemoveStatDropdownListener();

        List<string> options = new();

        if (availableStatKeys == null || availableStatKeys.Count == 0)
        {
            options.Add("(No StatKeys assigned)");
            statKeyDropdown.interactable = false;
            statKeyDropdown.ClearOptions();
            statKeyDropdown.AddOptions(options);
            statKeyDropdown.SetValueWithoutNotify(0);
            statKeyDropdown.RefreshShownValue();
            return;
        }

        statKeyDropdown.interactable = true;
        statKeyDropdown.ClearOptions();

        for (int i = 0; i < availableStatKeys.Count; i++)
        {
            StatKey statKey = availableStatKeys[i];
            string label = statKey != null
                ? string.IsNullOrWhiteSpace(statKey.Name) ? statKey.name : statKey.Name
                : "(Missing StatKey)";

            options.Add(label);
        }

        statKeyDropdown.AddOptions(options);
        statKeyDropdown.SetValueWithoutNotify(0);
        statKeyDropdown.RefreshShownValue();
        statKeyDropdown.onValueChanged.AddListener(OnStatDropdownChanged);
    }

    private void RemoveStatDropdownListener()
    {
        if (statKeyDropdown != null)
        {
            statKeyDropdown.onValueChanged.RemoveListener(OnStatDropdownChanged);
        }
    }

    private void OnStatDropdownChanged(int index)
    {
        if (statKeyDropdown == null)
        {
            return;
        }

        if (index < 0 || index >= availableStatKeys.Count)
        {
            SetStatus("Invalid stat selection");
            return;
        }

        StatKey selectedStatKey = availableStatKeys[index];
        if (selectedStatKey == null)
        {
            SetStatus("Selected StatKey is missing");
            return;
        }

        SetStatus($"Selected stat: {selectedStatKey.Name}");
    }

    private StatKey GetSelectedStatKey()
    {
        if (statKeyDropdown == null || availableStatKeys == null || availableStatKeys.Count == 0)
        {
            return null;
        }

        int selectedIndex = statKeyDropdown.value;
        if (selectedIndex < 0 || selectedIndex >= availableStatKeys.Count)
        {
            return null;
        }

        return availableStatKeys[selectedIndex];
    }

    private void AddScoreFromInput()
    {
        if (!TryReadInt(scoreAmountInputField, out int amount))
        {
            SetStatus("Invalid score amount");
            return;
        }

        amount = Mathf.Abs(amount);
        if (amount == 0)
        {
            SetStatus("Score amount must be greater than 0");
            return;
        }

        if (ScoreManager.Instance == null)
        {
            SetStatus("ScoreManager not available");
            return;
        }

        ScoreManager.Instance.AddScore(amount);
        SetStatus($"+{amount} score");
    }

    private void ToggleWaves()
    {
        waveManager.ToggleNewWavesEnabled();
        RefreshWaveState();
    }

    private void AddGlobalStatFromInput()
    {
        StatKey selectedStatKey = GetSelectedStatKey();
        if (selectedStatKey == null)
        {
            SetStatus("Assign and select a StatKey first");
            return;
        }
        if (!TryReadFloat(statAmountInputField, out float amount))
        {
            SetStatus("Invalid stat amount");
            return;
        }

        amount = Mathf.Abs(amount);
        if (Mathf.Approximately(amount, 0f))
        {
            SetStatus("Stat amount must be greater than 0");
            return;
        }

        ApplyGlobalStatModifier(selectedStatKey, amount);
    }

    private void ApplyGlobalStatModifier(StatKey statKey, float amount)
    {
        if (StatModifierManager.Instance == null)
        {
            SetStatus("StatModifierManager not available");
            return;
        }

        if (statKey == null)
        {
            SetStatus("Missing StatKey");
            return;
        }

        string sourceId = Guid.NewGuid().ToString("N");

        StatModifierManager.Instance.AddModifier(
            sourceId,
            string.Empty,
            statKey,
            StatModifierManager.ModifierKind.Additive,
            amount
        );

        SetStatus($"+{amount:0.##} {statKey.Name}");
    }

    private void RefreshWaveState()
    {
        if (wavesStatusText == null)
        {
            return;
        }

        wavesStatusText.text = waveManager.AreNewWavesEnabled ? "Status: enabled" : "Status: disabled";
    }

    private static bool TryReadInt(TMP_InputField inputField, out int value)
    {
        value = 0;
        if (inputField == null)
        {
            return false;
        }

        return int.TryParse(inputField.text, out value);
    }

    private static bool TryReadFloat(TMP_InputField inputField, out float value)
    {
        value = 0f;
        if (inputField == null)
        {
            return false;
        }

        return float.TryParse(inputField.text, out value);
    }

    private void SetStatus(string message)
    {
        if (statusText != null)
        {
            statusText.text = message;
        }
    }
}