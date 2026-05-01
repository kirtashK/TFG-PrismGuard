using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
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

    private InputSystem_Actions.UIActions uiActions;
    private bool uiModePushed;
    private bool inputReady;

    #region Unity methods

    private void Awake()
    {
        if (panelRoot != null)
        {
            panelRoot.SetActive(false);
        }

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
        while (HideElementManager.Instance == null
            || InputManager.Instance == null)
        {
            yield return null;
        }

        HideElementManager.Instance.Register(this);
        RefreshWaveState();

        uiActions = InputManager.Instance.UI;
        InputManager.Instance.OnDebugToggleRequested += TogglePanel;
        inputReady = true;
    }

    private void OnDisable()
    {
        if (HideElementManager.Instance != null)
        {
            HideElementManager.Instance.Unregister(this);
        }

        if (InputManager.Instance != null)
        {
            InputManager.Instance.OnDebugToggleRequested -= TogglePanel;
        }

        inputReady = false;

        RemoveButtonListeners();
        RemoveStatDropdownListener();
    }

    private void Start()
    {
        RefreshWaveState();
        SetStatus(string.Empty);
    }

    private void Update()
    {
        if (panelRoot == null || !panelRoot.activeSelf)
        {
            return;
        }

        if (!inputReady)
        {
            return;
        }

        if (uiActions.Cancel.WasPressedThisFrame())
        {
            HidePanel();
            return;
        }

        if (uiActions.Click.WasPressedThisFrame())
        {
            RectTransform rect = panelRoot.GetComponent<RectTransform>();
            Vector2 pointerPosition = uiActions.Point.ReadValue<Vector2>();

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

    #endregion

    public void TogglePanel()
    {
        if (panelRoot == null)
        {
            Debug.LogError($"{name}: missing {nameof(panelRoot)}");
            return;
        }

        if (panelRoot.activeSelf)
        {
            HidePanel();
            return;
        }

        HideElementManager.Instance.ShowOnly(this);
        panelRoot.SetActive(!panelRoot.activeSelf);

        if (!uiModePushed)
        {
            InputManager.Instance.PushMode(InputManager.InputMode.UI);
            uiModePushed = true;
        }

        RefreshWaveState();
    }

    public void HidePanel()
    {
        if (panelRoot != null)
        {
            panelRoot.SetActive(false);
        }

        if (uiModePushed)
        {
            InputManager.Instance.PopMode();
            uiModePushed = false;
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

        if (ScoreManager.Instance == null)
        {
            SetStatus("ScoreManager not available");
            return;
        }

        if (amount >= 0)
        {
            ScoreManager.Instance.AddScore(amount);
            SetStatus($"+{amount} score");            
        }
        else
        {
            ScoreManager.Instance.SpendScore(amount);
            SetStatus($"{amount} score");
        }
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

        if (amount >= 0f)
        {
            SetStatus($"+{amount:0.##} {statKey.Name}");
        }
        else
        {
            SetStatus($"{amount:0.##} {statKey.Name}");
        }
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