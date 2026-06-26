using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class CategoryButton : MonoBehaviour
{
    public StructureCategory category;

    [Tooltip("UI Button component")]
    public Button button;

    public TMP_Text label;

    public void Setup(StructureCategory cat, System.Action<StructureCategory> onClick)
    {
        category = cat;
        if (label != null)
        {
            label.text = cat.ToString();
        }
        if (button == null)
        {
            button = GetComponent<Button>();
        }
        if (button != null)
        {
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(() => onClick?.Invoke(category));
        }
    }
}
