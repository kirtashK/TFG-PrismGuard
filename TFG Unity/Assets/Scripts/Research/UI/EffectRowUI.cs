using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class EffectRowUI : MonoBehaviour
{
    public Image iconImage;
    public TMP_Text description;

    public void Setup(ResearchEffect effect)
    {
        if (effect == null)
        {
            description.text = "";
            iconImage.gameObject.SetActive(false);
            return;
        }

        if (effect.icon != null)
        {
            iconImage.sprite = effect.icon;
            iconImage.gameObject.SetActive(true);
        }
        else
        {
            iconImage.gameObject.SetActive(false);
        }

        description.text = effect.GetDescription();
    }
}