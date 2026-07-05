using UnityEngine;

/// <summary>
/// Result panel entrance: dim background fades in, prefab fades + scales up.
/// </summary>
public class ResultPanelAnim : MonoBehaviour
{
    private Transform panel;
    private CanvasGroup panelGroup;
    private CanvasGroup bgGroup;
    private float timer;
    private const float Duration = 0.4f;
    private bool playing;

    public void Play(Transform panelTransform, CanvasGroup panelGrp, CanvasGroup bgGrp)
    {
        panel = panelTransform;
        panelGroup = panelGrp;
        bgGroup = bgGrp;
        timer = 0f;
        playing = true;
    }

    void Update()
    {
        if (!playing) return;

        timer += Time.deltaTime;
        float t = Mathf.Clamp01(timer / Duration);
        float eased = 1f - Mathf.Pow(1f - t, 3f);

        if (bgGroup != null)
            bgGroup.alpha = eased * 0.5f;

        if (panelGroup != null)
            panelGroup.alpha = eased;

        if (panel != null)
            panel.localScale = Vector3.Lerp(new Vector3(0.85f, 0.85f, 0.85f), Vector3.one, eased);

        if (t >= 1f)
        {
            playing = false;
            if (panel != null) panel.localScale = Vector3.one;
            if (panelGroup != null) panelGroup.alpha = 1f;
            if (bgGroup != null) bgGroup.alpha = 0.5f;
        }
    }
}
