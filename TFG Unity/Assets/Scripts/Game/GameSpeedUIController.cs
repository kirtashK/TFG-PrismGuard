using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class GameSpeedUIController : MonoBehaviour
{
    [Serializable]
    public class SpeedButtonEntry
    {
        [Tooltip("Speed value represented by this button")]
        public float speedValue;

        [Tooltip("Button that sets the game speed when clicked")]
        public Button button;

        public Graphic icon;

        public Outline outline;

        [Tooltip("Color used when this button is not selected")]
        public Color normalColor = new(1f, 1f, 1f, 0.45f);

        [Tooltip("Color used when this button is selected")]
        public Color selectedColor = Color.white;

        [Tooltip("Scale used when this button is not selected")]
        public float normalScale = 1f;

        [Tooltip("Scale used when this button is selected")]
        public float selectedScale = 1.1f;
    }

    [Header("Buttons")]
    [SerializeField] private SpeedButtonEntry pauseButton;
    [SerializeField] private SpeedButtonEntry normalSpeedButton;
    [SerializeField] private SpeedButtonEntry fastSpeedButton;
    [SerializeField] private SpeedButtonEntry veryFastSpeedButton;

    private SpeedButtonEntry[] entries;

    private void Awake()
    {
        entries = new[]
        {
            pauseButton,
            normalSpeedButton,
            fastSpeedButton,
            veryFastSpeedButton,
        };

        WireButton(pauseButton);
        WireButton(normalSpeedButton);
        WireButton(fastSpeedButton);
        WireButton(veryFastSpeedButton);
    }

    private void OnEnable()
    {
        SetAllUnselected();
        StartCoroutine(RegisterWhenReady());
    }

    private IEnumerator RegisterWhenReady()
    {
        while (GameSpeedManager.Instance == null)
        {
            yield return null;
        }

        GameSpeedManager.Instance.OnGameSpeedChanged += HandleGameSpeedChanged;
        HandleGameSpeedChanged(GameSpeedManager.Instance.CurrentSpeed, GameSpeedManager.Instance.IsPaused);
    }

    private void OnDisable()
    {
        if (GameSpeedManager.Instance != null)
        {
            GameSpeedManager.Instance.OnGameSpeedChanged -= HandleGameSpeedChanged;
        }
    }

    private void WireButton(SpeedButtonEntry entry)
    {
        if (entry == null || entry.button == null)
        {
            return;
        }

        entry.button.onClick.AddListener(() =>
        {
            if (GameSpeedManager.Instance != null)
            {
                GameSpeedManager.Instance.SetGameSpeed(entry.speedValue);
            }
        });
    }

    /// <summary>
    /// Updates the selected visuals to match the current game speed state
    /// </summary>
    private void HandleGameSpeedChanged(float currentSpeed, bool isPaused)
    {
        for (int i = 0; i < entries.Length; i++)
        {
            SpeedButtonEntry entry = entries[i];
            if (entry == null)
            {
                continue;
            }

            bool isSelected;

            if (entry.speedValue <= 0f)
            {
                isSelected = isPaused;
            }
            else
            {
                isSelected = !isPaused && Mathf.Approximately(entry.speedValue, currentSpeed);
            }

            ApplyVisualState(entry, isSelected);
        }
    }

    private void ApplyVisualState(SpeedButtonEntry entry, bool selected)
    {
        if (entry.icon != null)
        {
            entry.icon.color = selected ? entry.selectedColor : entry.normalColor;
        }

        if (entry.outline != null)
        {
            entry.outline.enabled = selected;
        }

        if (entry.button != null)
        {
            Transform buttonTransform = entry.button.transform;
            float targetScale = selected ? entry.selectedScale : entry.normalScale;
            buttonTransform.localScale = new Vector3(targetScale, targetScale, targetScale);
        }
    }

    private void SetAllUnselected()
    {
        for (int i = 0; i < entries.Length; i++)
        {
            SpeedButtonEntry entry = entries[i];
            if (entry != null)
            {
                ApplyVisualState(entry, false);
            }
        }
    }
}