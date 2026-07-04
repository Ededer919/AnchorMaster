using UnityEngine;
using System.Collections;

public class AnchorVisual : MonoBehaviour
{
    [Header("Animation Settings")]
    public float placeDuration = 0.3f;
    public float removeDuration = 0.2f;
    public AnimationCurve placeCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

    [Header("Visual Effects")]
    public ParticleSystem placeEffect;
    public ParticleSystem removeEffect;
    public AudioSource audioSource;
    public AudioClip placeSound;
    public AudioClip removeSound;

    private SpriteRenderer spriteRenderer;
    private Vector3 originalScale;

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        originalScale = transform.localScale;
    }

    public void PlayPlaceAnimation()
    {
        StartCoroutine(PlaceAnimationCoroutine());
    }

    public void PlayRemoveAnimation()
    {
        StartCoroutine(RemoveAnimationCoroutine());
    }

    private IEnumerator PlaceAnimationCoroutine()
    {
        transform.localScale = Vector3.zero;

        if (placeEffect != null)
        {
            placeEffect.Play();
        }

        if (audioSource != null && placeSound != null)
        {
            audioSource.PlayOneShot(placeSound);
        }

        float elapsed = 0f;
        while (elapsed < placeDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / placeDuration;
            float curveValue = placeCurve.Evaluate(t);
            transform.localScale = originalScale * curveValue;
            yield return null;
        }

        transform.localScale = originalScale;
    }

    private IEnumerator RemoveAnimationCoroutine()
    {
        if (removeEffect != null)
        {
            removeEffect.Play();
        }

        if (audioSource != null && removeSound != null)
        {
            audioSource.PlayOneShot(removeSound);
        }

        float elapsed = 0f;
        Vector3 startScale = transform.localScale;
        while (elapsed < removeDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / removeDuration;
            transform.localScale = Vector3.Lerp(startScale, Vector3.zero, t);
            yield return null;
        }

        Destroy(gameObject);
    }

    public void SetHighlight(bool highlight)
    {
        if (spriteRenderer != null)
        {
            Color color = spriteRenderer.color;
            color.a = highlight ? 0.8f : 1f;
            spriteRenderer.color = color;
        }
    }
}
