using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Manages game flow UI panels: StartGame → LevelChoose → Game.
/// Loads panel prefabs from Resources/UI/Panels/ under the scene's MainCanvas.
/// </summary>
public class GameFlowManager : MonoBehaviour
{
    public static GameFlowManager Instance { get; private set; }

    private GameObject currentPanel;
    private Transform canvas;

    void Awake()
    {
        if (Instance == null) { Instance = this; DontDestroyOnLoad(gameObject); }
        else { Destroy(gameObject); return; }
    }

    void Start()
    {
        // Find existing MainCanvas in scene
        var mainCanvas = GameObject.Find("MainCanvas");
        if (mainCanvas != null)
        {
            canvas = mainCanvas.transform;
        }
        else
        {
            // Fallback: create one
            var go = new GameObject("MainCanvas");
            var c = go.AddComponent<Canvas>();
            c.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = go.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            go.AddComponent<GraphicRaycaster>();
            canvas = go.transform;
        }

        // Ensure EventSystem
        if (FindFirstObjectByType<UnityEngine.EventSystems.EventSystem>() == null)
        {
            var es = new GameObject("EventSystem");
            es.AddComponent<UnityEngine.EventSystems.EventSystem>();
            es.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
        }

        ShowStartGame();
    }

    // ========== Start Game Panel ==========

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

        Debug.Log("[Flow] StartGame panel shown");
    }

    // ========== Level Choose Panel ==========

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

        Debug.Log("[Flow] LevelChoose panel shown");
    }

    // ========== Game Panel ==========

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

        var launcher = FindFirstObjectByType<GameLauncher>();
        if (launcher != null)
            launcher.ReloadScene();

        Debug.Log("[Flow] Game panel shown, level " + (index + 1));
    }

    public void ReturnToLevelChoose()
    {
        ShowLevelChoose();
    }

    public void StartNextLevel()
    {
        if (LevelManager.Instance != null)
            StartLevel(LevelManager.Instance.CurrentIndex);
    }

    // ========== Helpers ==========

    void CloseCurrent()
    {
        if (currentPanel != null)
            DestroyImmediate(currentPanel);
        currentPanel = null;
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
