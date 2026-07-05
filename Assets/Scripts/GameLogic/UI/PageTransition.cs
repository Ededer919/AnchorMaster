using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Pixel-grid transition overlay. Created by GameFlowManager.
/// Animation is driven by GameFlowManager.Update() since standalone
/// MonoBehaviour Update is unreliable in this editor setup.
/// </summary>
public class PageTransition : MonoBehaviour
{
    public static PageTransition Instance { get; private set; }

    public const int GridCols = 32;
    public const int GridRows = 18;
    private const float RevealDuration = 0.45f;
    private const float CellColor = 0.04f;

    private Canvas overlayCanvas;
    private Image[,] cells;
    private float[,] waveDist;

    private float revealStart;
    private bool isRevealing;

    public bool IsRevealing => isRevealing;
    public bool IsCovering => overlayCanvas != null && overlayCanvas.gameObject.activeSelf && !isRevealing;

    void Awake()
    {
        Instance = this;
        CreateOverlay();
    }

    private void CreateOverlay()
    {
        overlayCanvas = new GameObject("TransitionOverlay").AddComponent<Canvas>();
        overlayCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        overlayCanvas.sortingOrder = 10000;
        var scaler = overlayCanvas.gameObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        overlayCanvas.gameObject.AddComponent<GraphicRaycaster>();
        overlayCanvas.gameObject.SetActive(false);

        cells = new Image[GridCols, GridRows];
        waveDist = new float[GridCols, GridRows];

        float cellW = 1920f / GridCols;
        float cellH = 1080f / GridRows;

        for (int x = 0; x < GridCols; x++)
        {
            for (int y = 0; y < GridRows; y++)
            {
                var go = new GameObject("px_" + x + "_" + y);
                go.transform.SetParent(overlayCanvas.transform, false);
                var rt = go.AddComponent<RectTransform>();
                rt.sizeDelta = new Vector2(cellW + 1, cellH + 1);
                rt.anchorMin = new Vector2(0.5f, 0.5f);
                rt.anchorMax = new Vector2(0.5f, 0.5f);
                rt.anchoredPosition = new Vector2(
                    -960f + cellW * 0.5f + x * cellW,
                    -540f + cellH * 0.5f + y * cellH);

                var img = go.AddComponent<Image>();
                img.color = new Color(CellColor, CellColor, CellColor + 0.03f, 0f);
                img.raycastTarget = false;
                cells[x, y] = img;

                float dx = (x - GridCols * 0.5f) / (GridCols * 0.5f);
                float dy = (y - GridRows * 0.5f) / (GridRows * 0.5f);
                waveDist[x, y] = Mathf.Sqrt(dx * dx + dy * dy);
            }
        }
    }

    /// <summary>
    /// Covers the screen, switches page, then reveals with pixel wave animation.
    /// The reveal animation is driven by GameFlowManager.Update() via UpdateReveal().
    /// </summary>
    public void Transition(System.Action midAction)
    {
        overlayCanvas.gameObject.SetActive(true);
        ForceAllAlpha(1f);

        midAction?.Invoke();

        isRevealing = true;
        revealStart = Time.realtimeSinceStartup;
    }

    /// <summary>
    /// Instant transition with no animation. Fallback if Update is unavailable.
    /// </summary>
    public void TransitionInstant(System.Action midAction)
    {
        overlayCanvas.gameObject.SetActive(true);
        ForceAllAlpha(1f);
        midAction?.Invoke();
        ForceAllAlpha(0f);
        overlayCanvas.gameObject.SetActive(false);
    }

    /// <summary>
    /// Call this from an external Update() to drive the reveal animation.
    /// </summary>
    public void UpdateReveal()
    {
        if (!isRevealing) return;

        float elapsed = Time.realtimeSinceStartup - revealStart;
        if (elapsed >= RevealDuration)
        {
            ForceAllAlpha(0f);
            overlayCanvas.gameObject.SetActive(false);
            isRevealing = false;
            return;
        }

        float progress = elapsed / RevealDuration;
        float maxDist = Mathf.Sqrt(2f);
        for (int x = 0; x < GridCols; x++)
        {
            for (int y = 0; y < GridRows; y++)
            {
                float delay = waveDist[x, y] / maxDist * 0.6f;
                float cellProgress = Mathf.Clamp01((progress - delay) / (1f - 0.6f));
                float alpha = 1f - cellProgress;
                var c = cells[x, y].color;
                c.a = alpha;
                cells[x, y].color = c;
            }
        }
    }

    private void ForceAllAlpha(float a)
    {
        for (int x = 0; x < GridCols; x++)
        {
            for (int y = 0; y < GridRows; y++)
            {
                var c = cells[x, y].color;
                c.a = a;
                cells[x, y].color = c;
            }
        }
    }
}
