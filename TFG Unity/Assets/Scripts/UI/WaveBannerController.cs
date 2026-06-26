using UnityEngine;
using System.Collections;
using TMPro;

public class WaveBannerController : MonoBehaviour
{
    public CanvasGroup canvasGroup;

    public TMP_Text bannerText;

    [Tooltip("Duration of the fade (seconds)")]
    public float fadeDuration = 0.5f;

    [Tooltip("Time it stays at maximun alpha before fading (seconds)")]
    public float displayTime = 1.5f;

    private Coroutine showRoutine;

    /// <summary>
    /// Shows a banner in the UI with the received text.
    /// The banner uses a fade
    /// </summary>
    public void ShowText(string text)
    {
        if (showRoutine != null)
        {
            StopCoroutine(showRoutine);
            showRoutine = null;
        }

        canvasGroup.alpha = 0f;
        showRoutine = StartCoroutine(ShowRoutine(text));
    }

    private IEnumerator ShowRoutine(string text)
    {
        bannerText.text = text;

        // Fade in
        float time = 0f;
        while (time < fadeDuration)
        {
            time += Time.deltaTime;
            canvasGroup.alpha = Mathf.Lerp(0f, 1f, time / fadeDuration);
            yield return null;
        }

        canvasGroup.alpha = 1f;

        yield return new WaitForSeconds(displayTime);

        // Fade out
        time = 0f;
        while (time < fadeDuration)
        {
            time += Time.deltaTime;
            canvasGroup.alpha = Mathf.Lerp(1f, 0f, time / fadeDuration);
            yield return null;
        }

        canvasGroup.alpha = 0f;
        showRoutine = null;
    }
}