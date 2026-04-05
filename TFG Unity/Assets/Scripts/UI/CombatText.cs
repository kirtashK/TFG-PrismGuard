using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class CombatText : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private Image iconImage;
    [SerializeField] private TMP_Text amountText;
    [SerializeField] private CanvasGroup canvasGroup;
    [SerializeField] private RectTransform root;

    [Header("Motion")]
    [SerializeField] private float lifetime = 1.5f;
    [SerializeField] private float riseDistance = 0.9f;
    [SerializeField] private float settleDropDistance = 0.18f;
    [SerializeField] private float sideDriftDistance = 0.35f;
    [SerializeField] private float randomAngleRange = 28f;

    [Header("Fade")]
    [SerializeField] private float fadeStartNormalizedTime = 0.65f;

    private Coroutine activeRoutine;
    private Action<CombatText> returnCallback;
    private Transform cameraTransform;
    private Vector3 startPosition;
    private Vector3 driftDirection;

    /// <summary>
    /// Configures the instance and starts the animation
    /// </summary>
    public void Play(float amount, CombatFeedbackType feedbackType, Sprite icon,
        Color color, Vector3 spawnPosition, Transform cameraTransform)
    {
        this.cameraTransform = cameraTransform;
        startPosition = spawnPosition;

        if (root == null)
        {
            root = transform as RectTransform;
        }

        if (amountText != null)
        {
            amountText.text = Mathf.RoundToInt(amount).ToString();
            amountText.color = color;
        }

        if (iconImage != null)
        {
            iconImage.sprite = icon;
            iconImage.gameObject.SetActive(icon != null);
        }

        float randomAngle = UnityEngine.Random.Range(-randomAngleRange, randomAngleRange);
        Vector2 randomUp = Quaternion.Euler(0f, 0f, randomAngle) * Vector2.up;

        Vector2 sidewaysBias = UnityEngine.Random.insideUnitCircle.normalized;
        if (sidewaysBias.sqrMagnitude < 0.01f)
        {
            sidewaysBias = Vector2.right;
        }

        Vector2 finalDirection2D = (randomUp + sidewaysBias * 0.35f).normalized;

        driftDirection = new Vector3(finalDirection2D.x, 0f, finalDirection2D.y);

        transform.position = spawnPosition;
        gameObject.SetActive(true);

        if (activeRoutine != null)
        {
            StopCoroutine(activeRoutine);
        }

        activeRoutine = StartCoroutine(Animate());
    }

    /// <summary>
    /// Sets the callback used to return the instance to the pool after animation ends
    /// </summary>
    public void SetReturnCallback(Action<CombatText> callback)
    {
        returnCallback = callback;
    }

    private void LateUpdate()
    {
        if (cameraTransform == null)
        {
            return;
        }

        Vector3 forward = transform.position - cameraTransform.position;
        if (forward.sqrMagnitude > 0.0001f)
        {
            transform.rotation = Quaternion.LookRotation(forward, cameraTransform.up);
        }
    }

    private IEnumerator Animate()
    {
        float elapsedTime = 0f;

        while (elapsedTime < lifetime)
        {
            elapsedTime += Time.deltaTime;
            float normalizedTime = Mathf.Clamp01(elapsedTime / lifetime);

            float riseTime = Mathf.Clamp01(normalizedTime / 0.45f);
            float settleTime = Mathf.Clamp01((normalizedTime - 0.45f) / 0.30f);

            float verticalOffset;

            if (normalizedTime < 0.45f)
            {
                verticalOffset = Mathf.Lerp(0f, riseDistance, riseTime);
            }
            else if (normalizedTime < 0.75f)
            {
                verticalOffset = Mathf.Lerp(riseDistance, settleDropDistance, settleTime);
            }
            else
            {
                verticalOffset = settleDropDistance;
            }

            float horizontalProgress = Mathf.Clamp01(normalizedTime / 0.70f);
            float horizontalOffset = Mathf.Lerp(0f, sideDriftDistance, horizontalProgress);

            Vector3 offset = (Vector3.up * verticalOffset) + (driftDirection * horizontalOffset);
            transform.position = startPosition + offset;

            if (canvasGroup != null)
            {
                float alpha;
                if (normalizedTime < fadeStartNormalizedTime)
                {
                    alpha = 1f;
                }
                else
                {
                    float fadeTime = Mathf.InverseLerp(fadeStartNormalizedTime, 1f, normalizedTime);
                    alpha = Mathf.Lerp(1f, 0f, fadeTime);
                }

                canvasGroup.alpha = alpha;
            }

            yield return null;
        }

        activeRoutine = null;
        returnCallback?.Invoke(this);
    }
}