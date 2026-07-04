using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Manages game flow as independent pages: StartGame → LevelChoose → Game → Result.
/// Each page is instantiated/destroyed — no show/hide toggling.
/// </summary>
public class GameFlowManager : MonoBehaviour
{
    public static GameFlowManager Instance { get; private set; }

    private GameObject currentPanel;
    private Transform canvas;
    private bool resultShown;

    void Awake()
    {
        if (Instance == null) { Instance = this; DontDestroyOnLoad(gameObject); }
        else { Destroy(gameObject); return; }
    }

    void Start()
    {
        var mainCanvas = GameObject.Find("MainCanvas");
        if (mainCanvas != null)
        {
            canvas = mainCanvas.transform;
        }
        else
        {
            var go = new GameObject("MainCanvas");
            var c = go.AddComponent<Canvas>();
            c.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = go.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            go.AddComponent<GraphicRaycaster>();
            canvas = go.transform;
        }

        if (FindFirstObjectByType<UnityEngine.EventSystems.EventSystem>() == null)
        {
            var es = new GameObject("EventSystem");
            es.AddComponent<UnityEngine.EventSystems.EventSystem>();
            es.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
        }

        ShowStartGame();
    }

    // ========== Start Game Page ==========

    void ShowStartGame()
    {
        CloseCurrent();

        var prefab = Resources.Load<GameObject>("UI/Panels/StartGame");
        if (prefab == null) { Debug.LogError("[Flow] StartGame prefab not found"); return; }

        var panel = Instantiate(prefab, canvas, false);
        panel.name = "StartGamePanel";
        currentPanel = panel;

        var startBtn = FindButton(panel, "Start");
        if (startBtn != null)
            startBtn.onClick.AddListener(ShowLevelChoose);

        Debug.Log("[Flow] StartGame page shown");
    }

    // ========== Level Choose Page ==========

    void ShowLevelChoose()
    {
        CloseCurrent();

        var prefab = Resources.Load<GameObject>("UI/Panels/LevelChoose");
        if (prefab == null) { Debug.LogError("[Flow] LevelChoose prefab not found"); return; }

        var panel = Instantiate(prefab, canvas, false);
        panel.name = "LevelChoosePanel";
        currentPanel = panel;

        for (int i = 1; i <= 5; i++)
        {
            var btn = FindButton(panel, "lv" + i);
            if (btn != null)
            {
                int levelIndex = i - 1;
                btn.onClick.AddListener(() => StartLevel(levelIndex));
            }
        }

        Debug.Log("[Flow] LevelChoose page shown");
    }

    // ========== Game Page ==========

    void StartLevel(int index)
    {
        CloseCurrent();

        if (LevelManager.Instance != null)
            LevelManager.Instance.LoadLevel(index);

        var prefab = Resources.Load<GameObject>("UI/Panels/Game");
        if (prefab == null) { Debug.LogError("[Flow] Game prefab not found"); return; }

        var panel = Instantiate(prefab, canvas, false);
        panel.name = "GamePanel";
        currentPanel = panel;

        resultShown = false;

        var launcher = FindFirstObjectByType<GameLauncher>();
        if (launcher != null)
            launcher.ReloadScene();

        Debug.Log("[Flow] Game page shown, level " + (index + 1));
    }

    // ========== Result Page ==========

    /// <summary>
    /// Creates a standalone result page. Called by DisasterWindController when game ends.
    /// GameWorld is kept visible behind the semi-transparent result panel.
    /// </summary>
    public void ShowResult(bool isWin, string message)
    {
        if (resultShown) return;
        resultShown = true;

        CloseCurrent();

        var panel = CreateResultPanel(isWin, message);
        panel.name = "ResultPanel";
        currentPanel = panel;

        Debug.Log("[Flow] Result page shown: " + (isWin ? "Win" : "Lose"));
    }

    private GameObject CreateResultPanel(bool isWin, string message)
    {
        var go = new GameObject("ResultPanel");
        go.transform.SetParent(canvas, false);

        var rt = go.AddComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;

        // Semi-transparent background
        var bgGo = new GameObject("Background");
        bgGo.transform.SetParent(go.transform, false);
        var bgRt = bgGo.AddComponent<RectTransform>();
        bgRt.anchorMin = Vector2.zero;
        bgRt.anchorMax = Vector2.one;
        bgRt.offsetMin = Vector2.zero;
        bgRt.offsetMax = Vector2.zero;
        var bgImg = bgGo.AddComponent<Image>();
        bgImg.color = new Color(0, 0, 0, 0.6f);

        // Result card
        var cardGo = new GameObject("ResultCard");
        cardGo.transform.SetParent(go.transform, false);
        var cardRt = cardGo.AddComponent<RectTransform>();
        cardRt.sizeDelta = new Vector2(500, 320);
        cardRt.anchoredPosition = Vector2.zero;
        var cardImg = cardGo.AddComponent<Image>();
        cardImg.color = new Color(0.15f, 0.15f, 0.2f, 0.95f);

        // Title text
        var titleGo = new GameObject("ResultTitle");
        titleGo.transform.SetParent(cardGo.transform, false);
        var titleRt = titleGo.AddComponent<RectTransform>();
        titleRt.anchoredPosition = new Vector2(0, 80);
        titleRt.sizeDelta = new Vector2(460, 60);
        var titleText = titleGo.AddComponent<Text>();
        titleText.text = isWin ? "胜利!" : "失败!";
        titleText.color = isWin ? new Color(0.3f, 1f, 0.4f) : new Color(1f, 0.3f, 0.3f);
        titleText.alignment = TextAnchor.MiddleCenter;
        titleText.fontSize = 48;
        titleText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

        // Subtitle text
        var subGo = new GameObject("ResultSubtitle");
        subGo.transform.SetParent(cardGo.transform, false);
        var subRt = subGo.AddComponent<RectTransform>();
        subRt.anchoredPosition = new Vector2(0, 20);
        subRt.sizeDelta = new Vector2(460, 40);
        var subText = subGo.AddComponent<Text>();
        subText.text = message;
        subText.color = Color.white;
        subText.alignment = TextAnchor.MiddleCenter;
        subText.fontSize = 22;
        subText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

        // Button row
        var rowGo = new GameObject("ButtonRow");
        rowGo.transform.SetParent(cardGo.transform, false);
        var rowRt = rowGo.AddComponent<RectTransform>();
        rowRt.anchoredPosition = new Vector2(0, -70);
        rowRt.sizeDelta = new Vector2(460, 50);

        // Back to level choose button
        var backBtn = CreateButton(rowGo.transform, "BackButton", "返回选关", new Vector2(-120, 0));
        backBtn.onClick.AddListener(() =>
        {
            CleanupGameAndReturn();
        });

        // Next level button (only if win and not last level)
        bool showNext = isWin && LevelManager.Instance != null && !LevelManager.Instance.IsLastLevel();
        if (showNext)
        {
            var nextBtn = CreateButton(rowGo.transform, "NextButton", "下一关", new Vector2(120, 0));
            nextBtn.onClick.AddListener(() =>
            {
                CloseCurrent();
                if (LevelManager.Instance != null)
                    LevelManager.Instance.NextLevel();
                StartLevel(LevelManager.Instance.CurrentIndex);
            });
        }

        return go;
    }

    private Button CreateButton(Transform parent, string name, string label, Vector2 position)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        var rt = go.AddComponent<RectTransform>();
        rt.sizeDelta = new Vector2(200, 50);
        rt.anchoredPosition = position;
        var img = go.AddComponent<Image>();
        img.color = new Color(0.2f, 0.4f, 0.7f, 1f);
        var btn = go.AddComponent<Button>();
        var colors = btn.colors;
        colors.highlightedColor = new Color(0.3f, 0.5f, 0.8f, 1f);
        colors.pressedColor = new Color(0.15f, 0.3f, 0.5f, 1f);
        btn.colors = colors;

        var labelGo = new GameObject("Label");
        labelGo.transform.SetParent(go.transform, false);
        var labelRt = labelGo.AddComponent<RectTransform>();
        labelRt.anchorMin = Vector2.zero;
        labelRt.anchorMax = Vector2.one;
        labelRt.offsetMin = Vector2.zero;
        labelRt.offsetMax = Vector2.zero;
        var labelText = labelGo.AddComponent<Text>();
        labelText.text = label;
        labelText.color = Color.white;
        labelText.alignment = TextAnchor.MiddleCenter;
        labelText.fontSize = 24;
        labelText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        labelText.raycastTarget = false;

        return btn;
    }

    // ========== Navigation Helpers ==========

    /// <summary>
    /// Cleans up game scene then returns to level choose page.
    /// </summary>
    public void ReturnToLevelChoose()
    {
        var launcher = FindFirstObjectByType<GameLauncher>();
        if (launcher != null)
            launcher.CleanupScene();

        ShowLevelChoose();
    }

    private void CleanupGameAndReturn()
    {
        CloseCurrent();
        ReturnToLevelChoose();
    }

    public void StartNextLevel()
    {
        if (LevelManager.Instance != null)
            StartLevel(LevelManager.Instance.CurrentIndex);
    }

    // ========== Internal ==========

    void CloseCurrent()
    {
        if (currentPanel != null)
            DestroyImmediate(currentPanel);
        currentPanel = null;
        resultShown = false;
    }

    Button FindButton(GameObject root, string name)
    {
        var t = root.transform.Find(name);
        if (t != null) return t.GetComponent<Button>();

        var btns = root.GetComponentsInChildren<Button>(true);
        foreach (var b in btns)
        {
            if (b.gameObject.name == name) return b;
        }
        return null;
    }
}
