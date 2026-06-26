using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Bridges an UI Button to an UIAction ScriptableObject asset
/// </summary>
[RequireComponent(typeof(Button))]
public class UIActionButton : MonoBehaviour
{
    [Tooltip("When the button is clicked, execute this action")]
    [SerializeField] private UIAction action;

    private Button button;

    private void Awake()
    {
        button = GetComponent<Button>();
        button.onClick.AddListener(HandleClick);
    }

    private void OnDestroy()
    {
        button.onClick.RemoveListener(HandleClick);
    }

    private void HandleClick()
    {
        if (action == null)
        {
            Debug.LogWarning($"{name}: no action assigned");
            return;
        }

        action.Execute();
    }
}