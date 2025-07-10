using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using TMPro;

public class WaveBannerController : MonoBehaviour
{
    public CanvasGroup canvasGroup;

    public TMP_Text bannerText;

    [Tooltip("Duración del fade-in y fade-out en segundos")]
    public float fadeDuration = 0.5f;

    [Tooltip("Tiempo que permanece al máximo alpha antes de fade-out")]
    public float displayTime = 1.5f;

    private Coroutine showRoutine;

    /// <summary>
    /// Muestra un banner en UI con el texto recibido
    /// Este banner aparece y desaparece con un fade
    /// </summary>
    public void Show(string text)
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

        // Mantener durante displayTime 
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