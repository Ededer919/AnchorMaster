using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Manages game flow as independent pages: StartGame → LevelChoose → Game → Result.
/// Each page switch is wrapped in a pixel-grid transition.
/// Plays click SFX on button presses, BGM on game page.
/// </summary>
public class GameFlowManager : MonoBehaviour
{
    public static GameFlowManager Instance { get; private set; }

    private GameObject currentPanel;
    private Transform canvas;
    private bool resultShown;
    private bool isInitialized;

    void Awake()
    {
        if (Instance == null) { Instance = this; DontDestroyOnLoad(gameObject); }
        else { Destroy(gameObject); return; }
    }

    void Start()
    {
        // Ensure AudioManager exists
        if (FindFirstObjectByType<AudioManager>() == null)
        {
            var amGo = new GameObject("AudioManager");
            amGo.AddComponent<AudioManager>();
        }

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

        // Ensure PageTransition exists on its own GameObject
        if (FindFirstObjectByType<PageTransition>() == null)
        {
            var ptGo = new GameObject("PageTransition");
            ptGo.AddComponent<PageTransition>();
        }

        // First page: no transition (coroutines not ready on frame 0)
        isInitialized = true;
        CloseCurrent();

        var prefab0 = Resources.Load<GameObject>("UI/Panels/StartGame");
        if (prefab0 != null)
        {
            var panel0 = Instantiate(prefab0, canvas, false);
            panel0.name = "StartGamePanel";
            currentPanel = panel0;

            // Enlarge title, raise button position
            var titleRt = FindChildRecursive(panel0.transform, "nametitle") as RectTransform;
            if (titleRt != null)
            {
                var s = titleRt.localScale;
                titleRt.localScale = new Vector3(s.x * 1.3f, s.y * 1.3f, s.z);
            }
            var startRt0 = FindChildRecursive(panel0.transform, "Start") as RectTransform;
            if (startRt0 != null)
            {
                var pos = startRt0.anchoredPosition;
                startRt0.anchoredPosition = new Vector2(pos.x, pos.y + 80f);
            }

            var startBtn0 = FindButton(panel0, "Start");
            if (startBtn0 != null)
            {
                startBtn0.onClick.AddListener(() => { PlayClick(); ShowLevelChoose(); });
                UIAnimator.SetupButton(startBtn0);
            }

            // Play slide-in entrance (title from top, button from bottom)
            var anim = panel0.AddComponent<StartScreenAnim>();
            anim.Play();

            AudioManager.Instance?.PlayBGM();
            Debug.Log("[Flow] StartGame page shown (initial)");
        }
    }

    private void TransitionTo(System.Action loadAction)
    {
        if (!isInitialized || PageTransition.Instance == null)
            loadAction();
        else
            PageTransition.Instance.Transition(loadAction);
    }

    private void Update()
    {
        if (PageTransition.Instance != null)
            PageTransition.Instance.UpdateReveal();
    }

    private void PlayClick()
    {
        AudioManager.Instance?.PlayClick();
    }

    // ========== Start Game Page ==========

    void ShowStartGame()
    {
        TransitionTo(() =>
        {
            CloseCurrent();

            var prefab = Resources.Load<GameObject>("UI/Panels/StartGame");
            if (prefab == null) { Debug.LogError("[Flow] StartGame prefab not found"); return; }

            var panel = Instantiate(prefab, canvas, false);
            panel.name = "StartGamePanel";
            currentPanel = panel;

            var startBtn = FindButton(panel, "Start");
            if (startBtn != null)
            {
                startBtn.onClick.AddListener(() => { PlayClick(); ShowLevelChoose(); });
                UIAnimator.SetupButton(startBtn);
            }

            UIAnimator.SetupAllButtons(panel);
            UIAnimator.PlayEntrance(panel, 0.4f);
            Debug.Log("[Flow] StartGame page shown");
        });
    }

    // ========== Level Choose Page ==========

    void ShowLevelChoose()
    {
        TransitionTo(() =>
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
                    btn.onClick.AddListener(() => { PlayClick(); StartLevel(levelIndex); });
                    UIAnimator.SetupButton(btn);
                }
            }

            UIAnimator.SetupAllButtons(panel);
            UIAnimator.PlayEntrance(panel, 0.4f);
            Debug.Log("[Flow] LevelChoose page shown");
        });
    }

    // ========== Game Page ==========

    void StartLevel(int index)
    {
        TransitionTo(() =>
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

            UIAnimator.SetupAllButtons(panel);
            Debug.Log("[Flow] Game page shown, level " + (index + 1));
        });
    }

    // ========== Result Page ==========

    /// <summary>
    /// Shows the result panel (victory or lose prefab) with a pixel transition.
    /// Called by DisasterWindController when game ends.
    /// </summary>
    public void ShowResult(bool isWin, string message)
    {
        if (resultShown) return;
        resultShown = true;

        TransitionTo(() =>
        {
            CloseCurrent();

            // ---- Semi-transparent dim overlay ----
            var overlay = new GameObject("ResultPanel");
            overlay.transform.SetParent(canvas, false);
            var overlayRt = overlay.AddComponent<RectTransform>();
            overlayRt.anchorMin = Vector2.zero;
            overlayRt.anchorMax = Vector2.one;
            overlayRt.offsetMin = Vector2.zero;
            overlayRt.offsetMax = Vector2.zero;

            var bgGo = new GameObject("DimBG");
            bgGo.transform.SetParent(overlay.transform, false);
            var bgRt = bgGo.AddComponent<RectTransform>();
            bgRt.anchorMin = Vector2.zero;
            bgRt.anchorMax = Vector2.one;
            bgRt.offsetMin = Vector2.zero;
            bgRt.offsetMax = Vector2.zero;
            var bgImg = bgGo.AddComponent<Image>();
            bgImg.color = new Color(0, 0, 0, 0.5f);
            var bgGroup = bgGo.AddComponent<CanvasGroup>();
            bgGroup.alpha = 0f;

            // ---- Load result prefab directly ----
            string prefabName = isWin ? "UI/Panels/voctory" : "UI/Panels/lose";
            var prefab = Resources.Load<GameObject>(prefabName);
            if (prefab == null)
            {
                Debug.LogError("[Flow] Result prefab not found: " + prefabName);
                return;
            }

            var panel = Instantiate(prefab, overlay.transform, false);
            // Keep prefab's original transform — don't add RectTransform if it doesn't have one
            panel.transform.localScale = Vector3.one;

            // Add CanvasGroup for fade animation if not present
            var panelGroup = panel.GetComponent<CanvasGroup>();
            if (panelGroup == null)
                panelGroup = panel.AddComponent<CanvasGroup>();
            panelGroup.alpha = 0f;
            panel.transform.localScale = new Vector3(0.85f, 0.85f, 0.85f);

            currentPanel = overlay;

            // ---- Wire buttons ----
            var retryBtn = FindButton(panel, "retry");
            if (retryBtn != null)
            {
                retryBtn.onClick.AddListener(() =>
                {
                    PlayClick();
                    int idx = LevelManager.Instance != null ? LevelManager.Instance.CurrentIndex : 0;
                    StartLevel(idx);
                });
                UIAnimator.SetupButton(retryBtn);
            }

            var backBtn = FindButton(panel, "backmain");
            if (backBtn != null)
            {
                backBtn.onClick.AddListener(() =>
                {
                    PlayClick();
                    CleanupGameAndReturn();
                });
                UIAnimator.SetupButton(backBtn);
            }

            UIAnimator.SetupAllButtons(panel);

            // ---- Entrance: dim fades in, prefab fades+scales in ----
            var anim = overlay.AddComponent<ResultPanelAnim>();
            anim.Play(panel.transform, panelGroup, bgGroup);

            Debug.Log("[Flow] Result page shown: " + (isWin ? "Win" : "Lose") + " — " + message);
        });
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

    Transform FindChildRecursive(Transform root, string name)
    {
        for (int i = 0; i < root.childCount; i++)
        {
            var child = root.GetChild(i);
            if (child.name == name) return child;
            var result = FindChildRecursive(child, name);
            if (result != null) return result;
        }
        return null;
    }
}
