using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class DebugMenuManager : BasePanel
{
    [Header("Score")]
    [SerializeField] private TMP_InputField scoreAmountInputField;
    [SerializeField] private Button addScoreInputButton;

    [Header("Waves")]
    [SerializeField] private Button toggleWavesButton;
    [SerializeField] private TMP_Text wavesStatusText;
    [SerializeField] private WaveManager waveManager;

    [SerializeField] private TMP_Text waveTimerText;
    [SerializeField] private TMP_InputField waveTimeInputField;
    [SerializeField] private Button setWaveTimeButton;

    [Header("Global stat")]
    [SerializeField] private List<StatKey> availableStatKeys = new();
    [SerializeField] private TMP_Dropdown statKeyDropdown;
    [SerializeField] private TMP_InputField statAmountInputField;
    [SerializeField] private Button addStatInputButton;

    [Header("Feedback")]
    [SerializeField] private TMP_Text statusText;

    private bool isListeningForDebugToggle;

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

    protected override void OnEnable()
    {
        base.OnEnable();

        AddButtonListeners();
        InitializeStatDropdown();
    }

    protected override void OnPanelReady() 
    {
        RefreshWaveState();

        if (!isListeningForDebugToggle && InputManager.Instance != null)
        {
            InputManager.Instance.OnDebugToggleRequested += TogglePanel;
            isListeningForDebugToggle = true;
        }
    }

    protected override void OnDisable()
    {
        base.OnDisable();

        if (isListeningForDebugToggle && InputManager.Instance != null)
        {
            InputManager.Instance.OnDebugToggleRequested -= TogglePanel;
            isListeningForDebugToggle = false;
        }

        RemoveButtonListeners();
        RemoveStatDropdownListener();
    }

    private void Start()
    {
        RefreshWaveState();
        SetStatus(string.Empty);
    }

    protected override void Update()
    {
        base.Update();

        if (waveTimerText != null && waveManager != null)
        {
            waveTimerText.text = $"Next wave in: {waveManager.TimeUntilNextWave:F1} seconds";
        }
    }

    #endregion

    public override void TogglePanel()
    {
        base.TogglePanel();

        RefreshWaveState();
    }

    #region Private methods

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
        if (setWaveTimeButton != null)
        {
            setWaveTimeButton.onClick.RemoveListener(SetWaveTimeFromInput);
            setWaveTimeButton.onClick.AddListener(SetWaveTimeFromInput);
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
        if (setWaveTimeButton != null)
        {
            setWaveTimeButton.onClick.RemoveListener(SetWaveTimeFromInput);
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
            SetStatus($"{amount} score added");            
        }
        else
        {
            if (ScoreManager.Instance.CanSpendScore(amount))
            {
                ScoreManager.Instance.SpendScore(amount);
                SetStatus($"{-amount} score removed");
            }
            else
            {
                SetStatus($"Cannot remove {-amount} score");
            }
        }
    }

    private void ToggleWaves()
    {
        waveManager.ToggleNewWavesEnabled();
        RefreshWaveState();
    }

    private void SetWaveTimeFromInput()
    {
        if (!TryReadFloat(waveTimeInputField, out float seconds))
        {
            SetStatus("Invalid wave time");
            return;
        }

        if (waveManager == null)
        {
            SetStatus("WaveManager not available");
            return;
        }

        seconds = Math.Max(0f, seconds);

        waveManager.SetTimeUntilNextWave(seconds);
        SetStatus($"Wave timer set to {seconds:F1} seconds");
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

    #endregion
}