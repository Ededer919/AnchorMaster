using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

/// <summary>
/// Adds hover scale-up, click scale-punch, and panel entrance animations to UI.
/// </summary>
[RequireComponent(typeof(Button))]
public class UIAnimator : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler
{
    private Vector3 baseScale;
    private bool isAnimating;

    private const float HoverScale = 1.08f;
    private const float ClickScale = 0.92f;
    private const float LerpSpeed = 15f;

    private Vector3 targetScale;

    void Awake()
    {
        baseScale = transform.localScale;
        targetScale = baseScale;
    }

    void Update()
    {
        if (transform.localScale != targetScale)
            transform.localScale = Vector3.Lerp(transform.localScale, targetScale, Time.deltaTime * LerpSpeed);
    }

    public void OnPointerEnter(PointerEventData e)
    {
        if (IsInteractable())
            targetScale = baseScale * HoverScale;
    }

    public void OnPointerExit(PointerEventData e)
    {
        if (IsInteractable())
            targetScale = baseScale;
    }

    public void OnPointerDown(PointerEventData e)
    {
        if (IsInteractable())
            targetScale = baseScale * ClickScale;
    }

    public void OnPointerUp(PointerEventData e)
    {
        if (IsInteractable())
            targetScale = baseScale * HoverScale;
    }

    private bool IsInteractable()
    {
        var btn = GetComponent<Button>();
        return btn == null || btn.interactable;
    }

    public static void SetupButton(Button btn)
    {
        if (btn == null) return;
        if (btn.GetComponent<UIAnimator>() == null)
            btn.gameObject.AddComponent<UIAnimator>();
    }

    public static void SetupAllButtons(GameObject root)
    {
        if (root == null) return;
        foreach (var btn in root.GetComponentsInChildren<Button>(true))
            SetupButton(btn);
    }

    // ========== Panel Entrance Animation ==========

    /// <summary>
    /// Plays a staggered entrance: panel scales up from 0 + fades in,
    /// then child buttons pop in one by one.
    /// </summary>
    public static void PlayEntrance(GameObject panel, float duration = 0.4f)
    {
        if (panel == null) return;

        // Add CanvasGroup for fade
        var group = panel.GetComponent<CanvasGroup>();
        if (group == null) group = panel.AddComponent<CanvasGroup>();

        var anim = panel.GetComponent<PanelEntranceAnim>();
        if (anim == null) anim = panel.AddComponent<PanelEntranceAnim>();
        anim.Play(duration);
    }

    /// <summary>
    /// Component that drives the entrance animation via Update.
    /// </summary>
    public class PanelEntranceAnim : MonoBehaviour
    {
        private CanvasGroup group;
        private Vector3 targetScale = Vector3.one;
        private float timer;
        private float duration;
        private bool playing;
        private Button[] childButtons;
        private int buttonsShown;

        public void Play(float dur)
        {
            group = GetComponent<CanvasGroup>();
            if (group == null) group = gameObject.AddComponent<CanvasGroup>();
            duration = dur;
            timer = 0f;
            playing = true;
            buttonsShown = 0;
            transform.localScale = Vector3.zero;
            group.alpha = 0f;

            // Collect child buttons for staggered pop-in
            childButtons = GetComponentsInChildren<Button>(true);
            foreach (var btn in childButtons)
            {
                if (btn != null) btn.transform.localScale = Vector3.zero;
            }
        }

        void Update()
        {
            if (!playing) return;

            timer += Time.deltaTime;
            float t = Mathf.Clamp01(timer / duration);

            // Panel scale-up + fade-in
            float eased = 1f - Mathf.Pow(1f - t, 3f); // ease-out cubic
            transform.localScale = Vector3.Lerp(Vector3.zero, targetScale, eased);
            if (group != null) group.alpha = eased;

            // Staggered button pop-in (after 50% of entrance)
            if (t > 0.5f && childButtons != null)
            {
                float btnT = (t - 0.5f) / 0.5f;
                int targetShown = Mathf.CeilToInt(btnT * childButtons.Length);
                while (buttonsShown < targetShown && buttonsShown < childButtons.Length)
                {
                    var btn = childButtons[buttonsShown];
                    if (btn != null)
                    {
                        btn.transform.localScale = Vector3.one;
                    }
                    buttonsShown++;
                }
            }

            if (t >= 1f)
            {
                playing = false;
                transform.localScale = targetScale;
                if (group != null) group.alpha = 1f;
            }
        }
    }
}
