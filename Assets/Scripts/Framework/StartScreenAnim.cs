using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Start screen entrance animation: title+bolt slide from top,
/// start button slides from bottom. Slower, smooth movement.
/// </summary>
public class StartScreenAnim : MonoBehaviour
{
    private RectTransform titleRt;
    private RectTransform boltRt;
    private RectTransform startRt;

    private Vector2 titleTarget;
    private Vector2 boltTarget;
    private Vector2 startTarget;

    private float timer;
    private const float TitleDuration = 1.2f;
    private const float BtnStartDelay = 0.6f;
    private const float BtnDuration = 1.0f;
    private const float SlideDistance = 1200f;

    private bool playing;

    public void Play()
    {
        var root = transform;

        // Find children by name
        titleRt = FindChild(root, "nametitle");
        boltRt = FindChild(root, "maodian");
        startRt = FindChild(root, "Start");

        if (titleRt != null)
        {
            titleTarget = titleRt.anchoredPosition;
            titleRt.anchoredPosition = titleTarget + new Vector2(0, SlideDistance);
        }
        if (boltRt != null)
        {
            boltTarget = boltRt.anchoredPosition;
            boltRt.anchoredPosition = boltTarget + new Vector2(0, SlideDistance);
        }
        if (startRt != null)
        {
            startTarget = startRt.anchoredPosition;
            startRt.anchoredPosition = startTarget - new Vector2(0, SlideDistance);
        }

        timer = 0f;
        playing = true;
    }

    private static RectTransform FindChild(Transform root, string name)
    {
        for (int i = 0; i < root.childCount; i++)
        {
            var child = root.GetChild(i);
            if (child.name == name)
                return child as RectTransform;
        }
        // Search deeper
        for (int i = 0; i < root.childCount; i++)
        {
            var result = FindChild(root.GetChild(i), name);
            if (result != null) return result;
        }
        return null;
    }

    void Update()
    {
        if (!playing) return;

        timer += Time.deltaTime;

        // Title + bolt slide down from top
        if (titleRt != null)
        {
            float t = Mathf.Clamp01(timer / TitleDuration);
            float eased = 1f - Mathf.Pow(1f - t, 3f);
            titleRt.anchoredPosition = Vector2.Lerp(
                titleTarget + new Vector2(0, SlideDistance), titleTarget, eased);
        }
        if (boltRt != null)
        {
            float t = Mathf.Clamp01(timer / TitleDuration);
            float eased = 1f - Mathf.Pow(1f - t, 3f);
            boltRt.anchoredPosition = Vector2.Lerp(
                boltTarget + new Vector2(0, SlideDistance), boltTarget, eased);
        }

        // Start button slides up from bottom (delayed)
        if (startRt != null && timer > BtnStartDelay)
        {
            float t = Mathf.Clamp01((timer - BtnStartDelay) / BtnDuration);
            float eased = 1f - Mathf.Pow(1f - t, 3f);
            startRt.anchoredPosition = Vector2.Lerp(
                startTarget - new Vector2(0, SlideDistance), startTarget, eased);
        }

        if (timer > BtnStartDelay + BtnDuration)
            playing = false;
    }
}
