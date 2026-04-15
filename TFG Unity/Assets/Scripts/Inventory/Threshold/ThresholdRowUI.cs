using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ThresholdRowUI : MonoBehaviour
{
    [SerializeField] private Image iconImage;
    [SerializeField] private TMP_Text recipeNameText;
    [SerializeField] private TMP_InputField thresholdInputField;
    [SerializeField] private Button increaseButton;
    [SerializeField] private Button decreaseButton;

    private ItemData itemData;
    private Action<int> onThresholdChanged;

    private void Awake()
    {
        if (thresholdInputField != null)
        {
            thresholdInputField.text = "1";
            thresholdInputField.onEndEdit.RemoveListener(HandleThresholdEdited);
            thresholdInputField.onEndEdit.AddListener(HandleThresholdEdited);
        }

        if (increaseButton != null)
        {
            increaseButton.onClick.RemoveAllListeners();
            increaseButton.onClick.AddListener(() => ChangeThresholdBy(+1));
        }

        if (decreaseButton != null)
        {
            decreaseButton.onClick.RemoveAllListeners();
            decreaseButton.onClick.AddListener(() => ChangeThresholdBy(-1));
        }
    }

    private void OnDisable()
    {
        if (thresholdInputField != null)
        {
            thresholdInputField.onEndEdit.RemoveListener(HandleThresholdEdited);
        }
    }

    public void Setup(ItemData item, Sprite icon, string title, int threshold, Action<int> callback)
    {
        itemData = item;
        onThresholdChanged = callback;

        if (iconImage != null)
        {
            iconImage.sprite = icon;
            iconImage.gameObject.SetActive(icon != null);
        }

        if (recipeNameText != null)
        {
            recipeNameText.text = title;
        }

        SetThreshold(threshold, notify: false);
    }

    private void ChangeThresholdBy(int delta)
    {
        int currentThreshold = ReadThresholdField();
        int nextThreshold = Mathf.Max(0, currentThreshold + delta);
        SetThreshold(nextThreshold, notify: true);
    }

    private void HandleThresholdEdited(string value)
    {
        if (!int.TryParse(value, out int parsedValue))
        {
            parsedValue = 1;
        }

        parsedValue = Mathf.Max(0, parsedValue);
        SetThreshold(parsedValue, notify: true);
    }

    private int ReadThresholdField()
    {
        if (thresholdInputField == null)
        {
            return 1;
        }

        if (!int.TryParse(thresholdInputField.text, out int value))
        {
            value = 1;
        }

        return Mathf.Max(0, value);
    }

    private void SetThreshold(int value, bool notify)
    {
        int normalizedValue = Mathf.Max(0, value);

        if (thresholdInputField != null)
        {
            thresholdInputField.SetTextWithoutNotify(normalizedValue.ToString());
        }

        if (notify)
        {
            onThresholdChanged?.Invoke(normalizedValue);
        }
    }
}